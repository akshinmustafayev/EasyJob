using System.Diagnostics;
using System.Text;
using EasyJob.Models;

namespace EasyJob.Services;

/// <summary>Builds the process start info for a configured button.</summary>
public static class ScriptCommand
{
    public static string ResolveScriptPath(ConfigButton b)
    {
        var script = NormalizeSeparators(b.Script);
        if (string.Equals(b.ScriptPathType, "relative", StringComparison.OrdinalIgnoreCase) && !Path.IsPathRooted(script))
            return Path.GetFullPath(Path.Combine(ConfigService.BaseDirectory, script));
        return script;
    }

    /// <summary>Configs written on Windows use backslashes; accept them on every OS.</summary>
    public static string NormalizeSeparators(string path) =>
        Path.DirectorySeparatorChar == '/' ? path.Replace('\\', '/') : path.Replace('/', '\\');

    public static ProcessStartInfo Build(AppConfig config, ConfigButton button, IReadOnlyList<string> answers, out string displayCommand)
    {
        var scriptPath = ResolveScriptPath(button);
        var scriptArgs = Tokenize(string.Join(" ", answers.Where(a => !string.IsNullOrWhiteSpace(a))));

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // closed right after start so scripts never block waiting for input
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = WorkingDirectoryFor(scriptPath)
        };
        psi.Environment["FORCE_COLOR"] = "1";
        psi.Environment["PYTHONUNBUFFERED"] = "1";

        if (button.ScriptType.Equals("powershell", StringComparison.OrdinalIgnoreCase))
        {
            psi.FileName = ShellResolver.ResolvePowerShell(config)
                ?? throw new InvalidOperationException(
                    "PowerShell was not found. Install PowerShell (pwsh) or set 'default_powershell_path' in Settings → Configuration.");
            foreach (var a in Tokenize(config.powershell_arguments)) psi.ArgumentList.Add(a);
            if (!config.powershell_arguments.Contains("-NonInteractive", StringComparison.OrdinalIgnoreCase)) psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(scriptPath);
            foreach (var a in scriptArgs) psi.ArgumentList.Add(a);
        }
        else if (button.ScriptType.Equals("python", StringComparison.OrdinalIgnoreCase))
        {
            psi.FileName = ShellResolver.ResolvePython(config)
                ?? throw new InvalidOperationException(
                    "Python was not found. Install Python 3 or set 'default_python_path' in Settings → Configuration.");
            psi.ArgumentList.Add("-u"); // unbuffered, so output appears live
            psi.ArgumentList.Add(scriptPath);
            foreach (var a in scriptArgs) psi.ArgumentList.Add(a);
        }
        else if (ShellResolver.IsWindows)
        {
            psi.FileName = ShellResolver.ResolveShell(config)!;
            // cmd.exe has its own quoting rules; hand it one pre-quoted string.
            psi.Arguments = "/c \"\"" + scriptPath + "\"" + (scriptArgs.Count > 0 ? " " + string.Join(" ", scriptArgs.Select(QuoteForCmd)) : "") + "\"";
        }
        else
        {
            psi.FileName = ShellResolver.ResolveShell(config)
                ?? throw new InvalidOperationException("No POSIX shell (/bin/sh) was found.");
            psi.ArgumentList.Add(scriptPath);
            foreach (var a in scriptArgs) psi.ArgumentList.Add(a);
        }

        displayCommand = psi.FileName + " " + (psi.ArgumentList.Count > 0
            ? string.Join(" ", psi.ArgumentList.Select(QuoteForDisplay))
            : psi.Arguments);
        return psi;
    }

    static string WorkingDirectoryFor(string scriptPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
        }
        catch { }
        return ConfigService.BaseDirectory;
    }

    static string QuoteForCmd(string s) => s.Contains(' ') && !s.StartsWith('"') ? "\"" + s + "\"" : s;
    static string QuoteForDisplay(string s) => s.Contains(' ') ? "\"" + s + "\"" : s;

    /// <summary>Splits a command line into tokens, honouring single and double quotes.</summary>
    public static List<string> Tokenize(string? line)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(line)) return result;

        var sb = new StringBuilder();
        char quote = '\0';
        bool has = false;
        foreach (var ch in line)
        {
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0'; else sb.Append(ch);
            }
            else if (ch is '"' or '\'')
            {
                quote = ch; has = true;
            }
            else if (char.IsWhiteSpace(ch))
            {
                if (sb.Length > 0 || has) { result.Add(sb.ToString()); sb.Clear(); has = false; }
            }
            else sb.Append(ch);
        }
        if (sb.Length > 0 || has) result.Add(sb.ToString());
        return result;
    }
}
