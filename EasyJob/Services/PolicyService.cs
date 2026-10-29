using System.Reflection;
using System.Text.Json;
using EasyJob.Models;

namespace EasyJob.Services;

/// <summary>
/// Administrator policy. A <c>policy.json</c> that only an administrator can write is merged over the user's own
/// restrictions every time the config loads, so users cannot switch a restriction off by editing config.json.
/// Looked for (all that exist are merged; a restriction set by any of them stays on):
///   Windows  %ProgramData%\EasyJob\policy.json
///   macOS    /Library/Application Support/EasyJob/policy.json
///   Linux    /etc/easyjob/policy.json
///   all      policy.json next to the program, and the file named by the EASYJOB_POLICY variable.
/// The file has the same shape as config.json; only its "restrictions" block is read.
/// </summary>
public static class PolicyService
{
    public sealed record Source(string Path, string? Error);

    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    static readonly PropertyInfo[] Props = typeof(ConfigRestrictions).GetProperties();

    static ConfigRestrictions? _policy;
    public static IReadOnlyList<Source> Sources { get; private set; } = Array.Empty<Source>();

    /// <summary>True when at least one policy file exists.</summary>
    public static bool IsManaged => _policy != null;

    public static IEnumerable<string> Candidates()
    {
        if (OperatingSystem.IsWindows())
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EasyJob", "policy.json");
        else if (OperatingSystem.IsMacOS())
            yield return "/Library/Application Support/EasyJob/policy.json";
        else
            yield return "/etc/easyjob/policy.json";

        yield return Path.Combine(AppContext.BaseDirectory, "policy.json");
        if (Environment.GetEnvironmentVariable("EASYJOB_POLICY") is { Length: > 0 } custom) yield return custom;
    }

    /// <summary>Reads the policy files. Called on every config load, so an administrator's change applies on reload.</summary>
    public static void Load()
    {
        var merged = new ConfigRestrictions();
        var sources = new List<Source>();
        bool any = false;

        foreach (var path in Candidates().Distinct())
        {
            if (!File.Exists(path)) continue;
            any = true;
            try
            {
                var file = JsonSerializer.Deserialize<PolicyFile>(File.ReadAllText(path), Options);
                Merge(merged, file?.restrictions ?? new ConfigRestrictions());
                sources.Add(new Source(path, null));
            }
            catch (Exception ex)
            {
                // A policy that cannot be read must not become "no policy": fail closed.
                Merge(merged, new ConfigRestrictions
                {
                    config_readonly = true,
                    block_script_editor = true,
                    block_updates = true,
                    hide_menu_item_settings_configuration = true
                });
                sources.Add(new Source(path, ex.Message));
            }
        }

        _policy = any ? merged : null;
        Sources = sources;
    }

    /// <summary>Puts the policy over the user's restrictions. The user's own values are kept so saving does not bake the policy into config.json.</summary>
    public static void Apply(AppConfig config)
    {
        Load();
        config.UserRestrictions = Clone(config.restrictions);
        Recompute(config);
    }

    /// <summary>Rebuilds the effective restrictions from the user's own plus the policy (after the user changed theirs).</summary>
    public static void Recompute(AppConfig config)
    {
        config.UserRestrictions ??= Clone(config.restrictions);
        var r = config.restrictions = Clone(config.UserRestrictions);
        if (_policy != null) Merge(r, _policy);

        if (r.config_readonly)
        {
            r.block_tabs_add = r.block_tabs_remove = r.block_tabs_rename = true;
            r.block_buttons_add = r.block_buttons_remove = r.block_buttons_edit = true;
            r.block_buttons_reorder = r.block_buttons_paste = r.block_buttons_copy = true;
            r.hide_menu_item_settings_configuration = true;
        }
        // With the editor window blocked, its own options (read-only mode, no Run, ...) have nothing to apply to.
        if (r.block_script_editor)
            r.script_editor_readonly = r.block_script_editor_run = r.block_script_editor_open_files = r.block_powershell_commands = false;
        if (r.block_updates) config.check_updates_on_startup = false;
    }

    /// <summary>Restrictions that only make sense while the script editor window is allowed.</summary>
    public static readonly string[] EditorOptions =
        { "script_editor_readonly", "block_script_editor_run", "block_script_editor_open_files", "block_powershell_commands" };

    /// <summary>True when the policy (not just the user's config) switches this restriction on.</summary>
    public static bool IsForced(string property)
    {
        if (_policy == null) return false;
        var p = Props.FirstOrDefault(x => x.Name == property);
        return p?.PropertyType == typeof(bool) && (bool)p.GetValue(_policy)!;
    }

    static ConfigRestrictions Clone(ConfigRestrictions r) =>
        JsonSerializer.Deserialize<ConfigRestrictions>(JsonSerializer.Serialize(r)) ?? new ConfigRestrictions();

    /// <summary>Booleans: on if either is on. Numbers: the smaller positive limit. Lists: the policy's list wins when it has entries.</summary>
    static void Merge(ConfigRestrictions target, ConfigRestrictions policy)
    {
        foreach (var p in Props)
        {
            if (p.PropertyType == typeof(bool))
            {
                if ((bool)p.GetValue(policy)!) p.SetValue(target, true);
            }
            else if (p.PropertyType == typeof(int))
            {
                int a = (int)p.GetValue(target)!, b = (int)p.GetValue(policy)!;
                if (b > 0) p.SetValue(target, a > 0 ? Math.Min(a, b) : b);
            }
            else if (p.PropertyType == typeof(List<string>))
            {
                if (p.GetValue(policy) is List<string> { Count: > 0 } list) p.SetValue(target, new List<string>(list));
            }
        }
    }

    sealed class PolicyFile
    {
        public ConfigRestrictions? restrictions { get; set; }
    }

    // ---------------------------------------------------------------- run checks

    /// <summary>Why this button may not run, or null when it may.</summary>
    public static string? CheckRun(AppConfig config, ConfigButton button, string resolvedPath)
    {
        var r = config.restrictions;

        if (r.allowed_script_types is { Count: > 0 } types
            && !types.Any(t => string.Equals(t?.Trim(), button.ScriptType, StringComparison.OrdinalIgnoreCase)))
            return $"Running {button.ScriptType} scripts is not allowed by your administrator.";

        if (!IsPathAllowed(config, resolvedPath))
            return "This script is outside the folders your administrator allows.";

        return null;
    }

    /// <summary>With no allowed folders every path is fine; otherwise the file must sit inside one of them (symbolic links are followed).</summary>
    public static bool IsPathAllowed(AppConfig config, string path)
    {
        if (config.restrictions.allowed_script_folders is not { Count: > 0 } folders) return true;

        string full;
        try
        {
            full = Path.GetFullPath(path);
            if (File.ResolveLinkTarget(full, returnFinalTarget: true) is { } target) full = target.FullName;
        }
        catch { return false; }

        var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        foreach (var raw in folders)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var folder = raw.Trim()
                .Replace("{config}", ConfigService.BaseDirectory)
                .Replace("{home}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            try
            {
                folder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (full.StartsWith(folder, comparison)) return true;
            }
            catch { }
        }
        return false;
    }

    // ---------------------------------------------------------------- concurrency

    static int _running;

    /// <summary>Reserves a run slot. False when max_concurrent_runs is reached.</summary>
    public static bool TryEnterRun(AppConfig config)
    {
        int limit = config.restrictions.max_concurrent_runs;
        while (true)
        {
            int current = Volatile.Read(ref _running);
            if (limit > 0 && current >= limit) return false;
            if (Interlocked.CompareExchange(ref _running, current + 1, current) == current) return true;
        }
    }

    public static void LeaveRun() => Interlocked.Decrement(ref _running);
}
