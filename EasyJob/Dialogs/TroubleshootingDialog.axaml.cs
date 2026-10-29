using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Media;
using EasyJob.Models;
using EasyJob.Services;

namespace EasyJob.Dialogs;

public partial class TroubleshootingDialog : Window
{
    // A null value marks a section header (kept so the copied report has the same structure).
    readonly List<(string Label, TextBlock? Value)> _facts = new();

    public TroubleshootingDialog() : this(new AppConfig()) { }

    public TroubleshootingDialog(AppConfig config)
    {
        InitializeComponent();
        var main = ViewModels.MainViewModel.Current;
        var proc = Process.GetCurrentProcess();

        Section("APPLICATION");
        Add("EasyJob version", UpdateService.Format(UpdateService.CurrentVersion));
        Add("Update status", main?.Update is { } up ? $"version {up.VersionText} is available (Help → {main.UpdateText})"
            : config.check_updates_on_startup ? "no newer release found at startup" : "update check on startup is off");
        Add("Program", Environment.ProcessPath ?? "unknown");
        Add("Install folder", AppContext.BaseDirectory);
        Add("Process", $"pid {Environment.ProcessId}, started {proc.StartTime:yyyy-MM-dd HH:mm:ss}, using {proc.WorkingSet64 / 1048576} MB");
        Add("Theme", $"{config.theme} (showing {Application.Current?.ActualThemeVariant})");

        Section("SYSTEM");
        Add("Operating system", RuntimeInformation.OSDescription);
        Add("OS version", Environment.OSVersion.VersionString);
        Add("Architecture", $"{RuntimeInformation.OSArchitecture} (app runs as {RuntimeInformation.ProcessArchitecture})");
        Add("Computer", $"{Environment.MachineName} · user {Environment.UserName}");
        Add("Processor cores", Environment.ProcessorCount.ToString());
        var mem = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        Add("Memory", mem > 0 ? $"{mem / 1073741824.0:0.#} GB" : "unknown");
        Add("System uptime", FormatSpan(TimeSpan.FromMilliseconds(Environment.TickCount64)));
        Add("Language", $"{CultureInfo.CurrentCulture.Name} (UI {CultureInfo.CurrentUICulture.Name})");
        Add("Time zone", $"{TimeZoneInfo.Local.DisplayName} · local time {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Add("Text encoding", $"console output read as UTF-8; system default {Encoding.Default.EncodingName}");

        Section("RUNTIME");
        Add(".NET runtime", RuntimeInformation.FrameworkDescription);
        Add("Runtime identifier", RuntimeInformation.RuntimeIdentifier);
        Add("UI framework", "Avalonia " + typeof(Application).Assembly.GetName().Version?.ToString(3));
        Add("Temporary files", Path.GetTempPath());

        Section("CONFIGURATION");
        Add("Config file", ConfigService.ConfigPath + FileState(ConfigService.ConfigPath));
        Add("Mode", File.Exists(Path.Combine(AppContext.BaseDirectory, "config.json"))
            ? "portable (config.json next to the program)" : "per-user (application data folder)");
        Add("Scripts resolve against", ConfigService.BaseDirectory);
        int buttons = config.tabs.Sum(t => t.Buttons.Count);
        Add("Tabs and buttons", $"{config.tabs.Count} tabs, {buttons} script buttons");
        var byType = config.tabs.SelectMany(t => t.Buttons).GroupBy(b => b.ScriptType.ToLowerInvariant()).OrderByDescending(g => g.Count())
            .Select(g => $"{g.Count()} {g.Key}");
        Add("Buttons by type", buttons == 0 ? "none" : string.Join(", ", byType));
        var missing = config.tabs.SelectMany(t => t.Buttons.Select(b => (Tab: t.Header, Button: b)))
            .Where(x => !File.Exists(ScriptCommand.ResolveScriptPath(x.Button))).ToList();
        Add("Script files", missing.Count == 0 ? (buttons == 0 ? "no scripts yet" : $"all {buttons} found")
            : $"{missing.Count} missing:\n" + string.Join("\n", missing.Take(8).Select(x => $"{x.Tab} › {x.Button.Text}: {ScriptCommand.ResolveScriptPath(x.Button)}"))
              + (missing.Count > 8 ? $"\n…and {missing.Count - 8} more" : ""), missing.Count > 0);
        Add("Console", $"text {config.console_foreground} on {config.console_background}; PowerShell's own colors {(config.powershell_native_colors ? "on" : "off")}");
        Add("Button size", config.button_density);
        Add("Active restrictions", RestrictionSummary(config.restrictions));

        Section("INTERPRETERS");
        var ps = ShellResolver.ResolvePowerShell(config);
        var psRow = Add("PowerShell", ps ?? "not found — install PowerShell 7 (pwsh) or set its path in Tools → Configuration", ps == null);
        Add("PowerShell arguments", string.IsNullOrWhiteSpace(config.powershell_arguments) ? "none" : config.powershell_arguments);
        var py = ShellResolver.ResolvePython(config);
        var pyRow = Add("Python", py ?? "not found — install Python 3 or set its path in Tools → Configuration", py == null);
        var sh = ShellResolver.ResolveShell(config);
        Add(ShellResolver.IsWindows ? "Command prompt" : "Shell", sh ?? "not found", sh == null);
        var dialogs = OperatingSystem.IsWindows() ? "Windows Forms" : OperatingSystem.IsMacOS()
            ? (ShellResolver.FindOnPath("osascript") is { } o ? "AppleScript (" + o + ")" : null)
            : (ShellResolver.FindOnPath("zenity") is { } z ? "zenity (" + z + ")" : null);
        Add("Script dialogs", dialogs ?? "not available — install zenity (Linux)", dialogs == null);

        Section("ENVIRONMENT");
        Add("Shell variable", Environment.GetEnvironmentVariable("SHELL") ?? Environment.GetEnvironmentVariable("ComSpec") ?? "not set");
        Add("Home folder", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Add("Working directory", Environment.CurrentDirectory);
        var path = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        Add($"PATH ({path.Length} folders)", string.Join("\n", path));

        Section("THIS SESSION");
        int runs = main?.Tabs.Sum(t => t.Runs.Count) ?? 0;
        int running = main?.Tabs.Sum(t => t.Runs.Count(r => r.IsRunning)) ?? 0;
        Add("Runs", $"{runs} in output panels, {running} running now");
        var events = main?.Events.TakeLast(12).Select(e => $"{e.TimeText}  {e.Text}").ToList() ?? new();
        Add("Recent events", events.Count == 0 ? "none" : string.Join("\n", events));

        if (ps != null) _ = AppendVersionAsync(psRow, ps, ShellResolver.PowerShellVersion);
        if (py != null) _ = AppendVersionAsync(pyRow, py, ShellResolver.PythonVersion);
    }

    static string FileState(string path)
    {
        try
        {
            if (!File.Exists(path)) return " (not created yet)";
            var info = new FileInfo(path);
            return $" ({info.Length / 1024.0:0.#} KB, changed {info.LastWriteTime:yyyy-MM-dd HH:mm}{(info.IsReadOnly ? ", read-only" : "")})";
        }
        catch { return ""; }
    }

    static string FormatSpan(TimeSpan t) => t.TotalDays >= 1 ? $"{(int)t.TotalDays} d {t.Hours} h" : $"{t.Hours} h {t.Minutes} min";

    static string RestrictionSummary(ConfigRestrictions r)
    {
        var on = typeof(ConfigRestrictions).GetProperties()
            .Where(p => p.PropertyType == typeof(bool) && (bool)p.GetValue(r)!)
            .Select(p => p.Name).ToList();
        return on.Count == 0 ? "none" : $"{on.Count}: " + string.Join(", ", on);
    }

    void Section(string text)
    {
        var t = new TextBlock { Text = text };
        t.Classes.Add("section");
        Rows.Children.Add(t);
        _facts.Add((text, null));
    }

    TextBlock Add(string label, string value, bool bad = false)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("190,*"), Margin = new Avalonia.Thickness(0, 4) };
        var l = new TextBlock { Text = label };
        l.Classes.Add("muted");
        var v = new SelectableTextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
        if (bad) v.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("DangerBrush"));
        Grid.SetColumn(v, 1);
        row.Children.Add(l);
        row.Children.Add(v);
        Rows.Children.Add(row);
        _facts.Add((label, v));
        return v;
    }

    static async Task AppendVersionAsync(TextBlock target, string path, Func<string, string?> getVersion)
    {
        var version = await Task.Run(() => getVersion(path));
        target.Text = version == null ? path : $"{path}\nversion {version}";
    }

    async void OnCopy(object? s, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("EasyJob troubleshooting report");
        foreach (var (label, value) in _facts)
        {
            if (value == null) { sb.AppendLine().AppendLine($"== {label} =="); continue; }
            var text = value.Text ?? "";
            if (text.Contains('\n')) sb.AppendLine($"{label}:").AppendLine("  " + text.Replace("\n", "\n  "));
            else sb.AppendLine($"{label}: {text}");
        }
        if (Clipboard != null) await Clipboard.SetTextAsync(sb.ToString());
    }

    void OnOpenFolder(object? s, RoutedEventArgs e) => Services.Launcher.Open(ConfigService.BaseDirectory);

    void OnClose(object? s, RoutedEventArgs e) => Close(true);
}
