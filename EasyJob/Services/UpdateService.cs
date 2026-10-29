using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyJob.Services;

/// <summary>A newer release found on GitHub.</summary>
public sealed record UpdateInfo(Version Version, string Tag, string Notes, string PageUrl, string? AssetName, string? AssetUrl, long AssetSize, string? Sha256)
{
    public string VersionText => UpdateService.Format(Version);
}

/// <summary>A downloaded and unpacked update, ready to be copied over the installation.</summary>
public sealed record StagedUpdate(string StageRoot, string WorkDir, string TargetDir, string? BundlePath);

/// <summary>
/// Checks the GitHub releases of EasyJob and installs a newer one. Every release carries a zip; the app downloads it,
/// unpacks it to a temporary folder, and a tiny script copies it over the installation after the app has quit
/// (a running program cannot overwrite itself), keeping the user's config.json, then starts the new version.
/// </summary>
public static class UpdateService
{
    const string LatestUrl = "https://api.github.com/repos/akshinmustafayev/EasyJob/releases/latest";
    public const string ReleasesPage = "https://github.com/akshinmustafayev/EasyJob/releases";

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EasyJob-Updater");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static Version CurrentVersion =>
        Normalize(Assembly.GetEntryAssembly()?.GetName().Version ?? Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0));

    static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    /// <summary>"2.1.0" (or "2.1.0.3" when the fourth part is used).</summary>
    public static string Format(Version v) => v.Revision > 0 ? v.ToString(4) : v.ToString(3);

    public static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0);
        var m = Regex.Match(tag ?? "", @"\d+(\.\d+){0,3}");
        if (!m.Success || !Version.TryParse(m.Value.Contains('.') ? m.Value : m.Value + ".0", out var parsed)) return false;
        version = Normalize(parsed);
        return true;
    }

    /// <summary>Returns the newest release if it is newer than this app, otherwise null.</summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        using var response = await Http.GetAsync(LatestUrl, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null; // no releases yet
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!TryParseTag(tag, out var version) || version <= CurrentVersion) return null;

        var assets = new List<(string Name, string Url, long Size, string? Sha)>();
        if (root.TryGetProperty("assets", out var list))
            foreach (var a in list.EnumerateArray())
            {
                var name = a.GetProperty("name").GetString() ?? "";
                if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                string? sha = null;
                if (a.TryGetProperty("digest", out var d) && d.GetString() is { } digest && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    sha = digest[7..];
                assets.Add((name, a.GetProperty("browser_download_url").GetString() ?? "", a.TryGetProperty("size", out var s) ? s.GetInt64() : 0, sha));
            }

        var asset = PickAsset(assets.Select(a => a.Name).ToList()) is { } picked ? assets.First(a => a.Name == picked) : default;
        return new UpdateInfo(version, tag,
            root.TryGetProperty("body", out var body) ? (body.GetString() ?? "").Trim() : "",
            root.TryGetProperty("html_url", out var url) ? url.GetString() ?? ReleasesPage : ReleasesPage,
            asset.Name, asset.Url, asset.Size, asset.Sha);
    }

    /// <summary>One zip is used as is. Several are matched on the OS and CPU in their file name (win/osx/linux, x64/arm64).</summary>
    public static string? PickAsset(IReadOnlyList<string> zips)
    {
        if (zips.Count == 0) return null;
        if (zips.Count == 1) return zips[0];

        string[] os = OperatingSystem.IsWindows() ? new[] { "win" }
            : OperatingSystem.IsMacOS() ? new[] { "osx", "macos", "mac", "darwin" }
            : new[] { "linux" };
        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

        var forOs = zips.Where(z => os.Any(o => z.Contains(o, StringComparison.OrdinalIgnoreCase))).ToList();
        if (forOs.Count == 0) return null;
        return forOs.FirstOrDefault(z => z.Contains(arch, StringComparison.OrdinalIgnoreCase))
            ?? (arch == "arm64" ? forOs.FirstOrDefault(z => z.Contains("x64", StringComparison.OrdinalIgnoreCase)) : null)
            ?? forOs[0];
    }

    // ---------------------------------------------------------------- install

    /// <summary>Why this copy of the app cannot update itself, or null when it can.</summary>
    public static string? CannotSelfUpdateReason()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return "The location of the running program is unknown.";
        if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            return "EasyJob is running through the dotnet tool (a development run), not as an installed app.";

        var target = TargetDirectory(exe, out _);
        try
        {
            var probe = Path.Combine(target, ".easyjob-write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "");
            File.Delete(probe);
        }
        catch
        {
            return $"EasyJob cannot write to its folder ({target}). Move it somewhere you own, or update it manually.";
        }
        return null;
    }

    /// <summary>The folder to update: the app folder, or the .app bundle on macOS.</summary>
    static string TargetDirectory(string exe, out string? bundle)
    {
        bundle = null;
        var marker = ".app" + Path.DirectorySeparatorChar + "Contents";
        int i = exe.IndexOf(marker, StringComparison.Ordinal);
        if (OperatingSystem.IsMacOS() && i >= 0)
        {
            bundle = exe[..(i + 4)];
            return bundle;
        }
        return Path.GetDirectoryName(exe)!;
    }

    public static async Task<StagedUpdate> DownloadAsync(UpdateInfo update, IProgress<double>? progress, CancellationToken ct = default)
    {
        if (update.AssetUrl == null) throw new InvalidOperationException("This release has no download for your system.");
        if (!Uri.TryCreate(update.AssetUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The release download is not an HTTPS link.");

        var exe = Environment.ProcessPath!;
        var target = TargetDirectory(exe, out var bundle);
        var work = Path.Combine(Path.GetTempPath(), "EasyJobUpdate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var zip = Path.Combine(work, "update.zip");
            using (var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? update.AssetSize;
                await using var input = await response.Content.ReadAsStreamAsync(ct);
                await using var output = File.Create(zip);
                var buffer = new byte[81920];
                long done = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                    done += read;
                    if (total > 0) progress?.Report((double)done / total);
                }
            }

            if (update.Sha256 != null)
            {
                await using var check = File.OpenRead(zip);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(check, ct));
                if (!hash.Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The downloaded file does not match the checksum published on GitHub.");
            }

            var stage = Path.Combine(work, "stage");
            ZipFile.ExtractToDirectory(zip, stage);
            File.Delete(zip);

            // Zips often wrap everything in one folder; step into it.
            var root = stage;
            while (Directory.GetFiles(root).Length == 0 && Directory.GetDirectories(root) is { Length: 1 } only
                   && !(bundle != null && only[0].EndsWith(".app", StringComparison.OrdinalIgnoreCase)))
                root = only[0];
            if (bundle != null && Directory.GetDirectories(root) is { Length: 1 } app && app[0].EndsWith(".app", StringComparison.OrdinalIgnoreCase) && Directory.GetFiles(root).Length == 0)
                root = app[0];

            // The user's settings stay as they are.
            var config = Path.Combine(root, "config.json");
            if (File.Exists(config)) File.Delete(config);

            var exeName = Path.GetFileName(exe);
            if (bundle == null && !File.Exists(Path.Combine(root, exeName)))
                throw new InvalidDataException($"The downloaded package does not contain {exeName}. It may be for another system.");
            if (bundle != null && !Directory.Exists(Path.Combine(root, "Contents")))
                throw new InvalidDataException("The downloaded package is not a macOS app bundle.");

            return new StagedUpdate(root, work, target, bundle);
        }
        catch
        {
            TryDelete(work);
            throw;
        }
    }

    /// <summary>Starts the helper that installs the staged files once this process has exited. The caller must then quit the app.</summary>
    public static void StartInstaller(StagedUpdate staged)
    {
        var exe = Environment.ProcessPath!;
        int pid = Environment.ProcessId;
        ProcessStartInfo psi;

        if (OperatingSystem.IsWindows())
        {
            string Q(string s) => s.Replace("%", "%%");
            var script = Path.Combine(staged.WorkDir, "update.cmd");
            File.WriteAllText(script, string.Join("\r\n", new[]
            {
                "@echo off",
                ":wait",
                $"tasklist /FI \"PID eq {pid}\" 2>nul | find \"{pid}\" >nul",
                "if not errorlevel 1 (timeout /t 1 /nobreak >nul & goto wait)",
                $"robocopy \"{Q(staged.StageRoot)}\" \"{Q(staged.TargetDir)}\" /E /R:15 /W:1 /NFL /NDL /NJH /NJS >nul",
                $"start \"\" \"{Q(exe)}\"",
                $"(goto) 2>nul & rd /s /q \"{Q(staged.WorkDir)}\""
            }) + "\r\n", Encoding.ASCII);
            psi = new ProcessStartInfo("cmd.exe", $"/c \"{script}\"") { UseShellExecute = false, CreateNoWindow = true };
        }
        else
        {
            static string Q(string s) => "'" + s.Replace("'", "'\\''") + "'";
            var script = Path.Combine(staged.WorkDir, "update.sh");
            var relaunch = staged.BundlePath != null ? $"open -n {Q(staged.BundlePath)}" : $"{Q(exe)} >/dev/null 2>&1 &";
            File.WriteAllText(script, string.Join("\n", new[]
            {
                "#!/bin/sh",
                $"while kill -0 {pid} 2>/dev/null; do sleep 0.5; done",
                $"cp -Rf {Q(staged.StageRoot)}/. {Q(staged.TargetDir)}/",
                staged.BundlePath == null ? $"chmod +x {Q(exe)}" : "true",
                relaunch,
                $"rm -rf {Q(staged.WorkDir)}"
            }) + "\n");
            psi = new ProcessStartInfo("/bin/sh") { UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add(script);
        }

        Process.Start(psi);
    }

    public static void Discard(StagedUpdate staged) => TryDelete(staged.WorkDir);

    static void TryDelete(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }
}
