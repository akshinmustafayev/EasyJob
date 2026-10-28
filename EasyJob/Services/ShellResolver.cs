using System.Runtime.InteropServices;
using EasyJob.Models;

namespace EasyJob.Services;

/// <summary>Finds the interpreters used to run scripts on the current operating system.</summary>
public static class ShellResolver
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>Resolves the PowerShell executable: configured path if it exists, else pwsh/powershell on PATH.</summary>
    public static string? ResolvePowerShell(AppConfig config)
    {
        if (UsableConfiguredPath(config.default_powershell_path) is { } configured) return configured;

        var candidates = IsWindows
            ? new[] { "pwsh.exe", "powershell.exe" }
            : new[] { "pwsh", "pwsh-preview" };
        foreach (var c in candidates)
            if (FindOnPath(c) is { } found) return found;

        if (IsWindows)
        {
            var legacy = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
            if (File.Exists(legacy)) return legacy;
        }
        return null;
    }

    /// <summary>Resolves the Python interpreter: configured path, else python3/python (py launcher on Windows).</summary>
    public static string? ResolvePython(AppConfig config)
    {
        if (UsableConfiguredPath(config.default_python_path) is { } configured) return configured;

        var candidates = IsWindows
            ? new[] { "python.exe", "py.exe", "python3.exe" }
            : new[] { "python3", "python" };
        foreach (var c in candidates)
            if (FindOnPath(c) is { } found)
            {
                // The Microsoft Store stub in WindowsApps opens the Store instead of running Python.
                if (IsWindows && found.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase)) continue;
                return found;
            }
        return null;
    }

    /// <summary>Resolves cmd.exe on Windows, /bin/sh (or bash) elsewhere.</summary>
    public static string? ResolveShell(AppConfig config)
    {
        if (IsWindows)
        {
            if (UsableConfiguredPath(config.default_cmd_path) is { } configured) return configured;
            return FindOnPath("cmd.exe") ?? Path.Combine(Environment.SystemDirectory, "cmd.exe");
        }

        if (UsableConfiguredPath(config.default_shell_path) is { } shell) return shell;
        foreach (var c in new[] { "/bin/bash", "/bin/sh", "/usr/bin/sh" })
            if (File.Exists(c)) return c;
        return FindOnPath("sh");
    }

    public static string? PowerShellVersion(string path) => RunForOutput(path, "-NoProfile", "-Command", "$PSVersionTable.PSVersion.ToString()");

    public static string? PythonVersion(string path) => RunForOutput(path, "--version");

    static string? RunForOutput(string path, params string[] args)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(path)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return null;
            var output = (p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()).Trim(); // old Pythons print the version to stderr
            if (!p.WaitForExit(5000)) { p.Kill(true); return null; }
            return output;
        }
        catch { return null; }
    }

    // A path copied from a Windows config (C:\...) is useless on macOS/Linux: ignore it instead of failing.
    static string? UsableConfiguredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (File.Exists(path)) return path;
        return FindOnPath(path);
    }

    public static string? FindOnPath(string name)
    {
        if (Path.IsPathRooted(name)) return File.Exists(name) ? name : null;
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return null;
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var full = Path.Combine(dir.Trim('"'), name);
                if (File.Exists(full)) return full;
            }
            catch { }
        }
        return null;
    }

    public static string OsDescription => RuntimeInformation.OSDescription;
}
