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
    readonly List<(string Label, TextBlock Value)> _facts = new();

    public TroubleshootingDialog() : this(new AppConfig()) { }

    public TroubleshootingDialog(AppConfig config)
    {
        InitializeComponent();

        Section("SYSTEM");
        Add("Operating system", RuntimeInformation.OSDescription);
        Add("Architecture", RuntimeInformation.OSArchitecture.ToString());
        Add(".NET runtime", RuntimeInformation.FrameworkDescription);

        Section("CONFIGURATION");
        Add("Config file", ConfigService.ConfigPath);
        Add("Scripts resolve against", ConfigService.BaseDirectory);

        Section("INTERPRETERS");
        var ps = ShellResolver.ResolvePowerShell(config);
        var psRow = Add("PowerShell", ps ?? "not found — install PowerShell 7 (pwsh) or set its path in Settings → Configuration", ps == null);
        Add("PowerShell arguments", string.IsNullOrWhiteSpace(config.powershell_arguments) ? "none" : config.powershell_arguments);
        var py = ShellResolver.ResolvePython(config);
        var pyRow = Add("Python", py ?? "not found — install Python 3 or set its path in Settings → Configuration", py == null);
        var sh = ShellResolver.ResolveShell(config);
        Add(ShellResolver.IsWindows ? "Command prompt" : "Shell", sh ?? "not found", sh == null);

        if (ps != null) _ = AppendVersionAsync(psRow, ps, ShellResolver.PowerShellVersion);
        if (py != null) _ = AppendVersionAsync(pyRow, py, ShellResolver.PythonVersion);
    }

    void Section(string text)
    {
        var t = new TextBlock { Text = text };
        t.Classes.Add("section");
        Rows.Children.Add(t);
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
        foreach (var (label, value) in _facts) sb.AppendLine($"{label}: {value.Text?.Replace("\n", " — ")}");
        if (Clipboard != null) await Clipboard.SetTextAsync(sb.ToString());
    }

    void OnClose(object? s, RoutedEventArgs e) => Close(true);
}
