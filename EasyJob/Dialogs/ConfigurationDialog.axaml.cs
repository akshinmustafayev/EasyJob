using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EasyJob.Controls;
using EasyJob.Models;
using EasyJob.Services;

namespace EasyJob.Dialogs;

public partial class ConfigurationDialog : Window
{
    readonly AppConfig _config;
    readonly List<(PropertyInfo Property, CheckBox Box)> _restrictions = new();

    public ConfigurationDialog() : this(new AppConfig()) { }

    public ConfigurationDialog(AppConfig config)
    {
        InitializeComponent();
        _config = config;

        PowerShellPath.Text = config.default_powershell_path;
        PowerShellPath.Watermark = ShellResolver.ResolvePowerShell(new AppConfig()) ?? "pwsh";
        PowerShellDetected.Text = ShellResolver.ResolvePowerShell(config) is { } ps ? "In use: " + ps : "PowerShell was not found on this machine.";
        PythonPath.Text = config.default_python_path;
        PythonPath.Watermark = ShellResolver.ResolvePython(new AppConfig()) ?? "python3";
        PythonDetected.Text = ShellResolver.ResolvePython(config) is { } py ? "In use: " + py : "Python was not found on this machine.";
        CmdPath.Text = config.default_cmd_path;
        ShellPath.Text = config.default_shell_path;
        PowerShellArgs.Text = config.powershell_arguments;
        ConsoleBackgroundBox.Text = config.console_background;
        ConsoleForegroundBox.Text = config.console_foreground;
        NativeColors.IsChecked = config.powershell_native_colors;
        ClearEvents.IsChecked = config.clear_events_when_reload;

        // cmd.exe only exists on Windows; the POSIX shell setting only matters elsewhere.
        CmdRow.IsVisible = CmdPath.IsVisible = ShellResolver.IsWindows;
        ShellLabel.IsVisible = ShellPath.IsVisible = !ShellResolver.IsWindows;

        ConsoleBackgroundBox.TextChanged += (_, _) => UpdatePreview();
        ConsoleForegroundBox.TextChanged += (_, _) => UpdatePreview();
        UpdatePreview();
        BuildRestrictions();
    }

    void UpdatePreview()
    {
        Preview.Background = Brush(ConsoleBackgroundBox.Text, Colors.Black);
        PreviewText.Foreground = Brush(ConsoleForegroundBox.Text, Colors.White);
    }

    static IBrush Brush(string? text, Color fallback) =>
        new SolidColorBrush(!string.IsNullOrWhiteSpace(text) && Color.TryParse(text, out var c) ? c : fallback);

    void BuildRestrictions()
    {
        AddGroup("BLOCK ACTIONS", Props().Where(p => p.Name.StartsWith("block_")));
        AddGroup("HIDE MENU ITEMS", Props().Where(p => p.Name.StartsWith("hide_")));
    }

    void AddGroup(string heading, IEnumerable<PropertyInfo> props)
    {
        var title = new TextBlock { Text = heading, Margin = new Avalonia.Thickness(2, 16, 0, 8) };
        title.Classes.Add("section");
        RestrictionsPanel.Children.Add(title);

        var list = new StackPanel { Spacing = 2 };
        foreach (var p in props) list.Children.Add(CreateRestriction(p));
        var card = new Border { Padding = new Avalonia.Thickness(14, 8), Child = list };
        card.Classes.Add("card");
        RestrictionsPanel.Children.Add(card);
    }

    static IEnumerable<PropertyInfo> Props() =>
        typeof(ConfigRestrictions).GetProperties().Where(p => p.PropertyType == typeof(bool));

    Control CreateRestriction(PropertyInfo p)
    {
        var box = new CheckBox { Content = Describe(p.Name), IsChecked = (bool)p.GetValue(_config.restrictions)! };
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        row.Children.Add(box);
        row.Children.Add(new HelpButton { Topic = HelpTopic(p.Name) });
        _restrictions.Add((p, box));
        return row;
    }

    /// <summary>block_tabs_remove → "Block tabs remove"; hide_menu_item_settings_workflow → "Hide Settings → Workflow".</summary>
    static string Describe(string name)
    {
        var words = name.Replace("hide_menu_item_", "hide_").Split('_');
        if (words[0] == "hide" && words.Length > 1)
        {
            var path = words.Skip(1).Select(Cap).ToList();
            return "Hide " + string.Join(" → ", path.Count > 2 ? new[] { path[0], string.Join(" ", path.Skip(1)) } : path);
        }
        var s = string.Join(" ", words);
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    static string Cap(string w) => char.ToUpperInvariant(w[0]) + w[1..];

    static string HelpTopic(string name)
    {
        var n = name.Replace("hide_menu_item_file_clear_events_list", "hide_clear_events_list")
                    .Replace("hide_menu_item_", "hide_")
                    .Replace("file_", "File_");
        var sb = new StringBuilder("HelpConfiguration");
        foreach (var part in n.Split('_'))
        {
            if (part == "help" && n.StartsWith("hide_help_")) continue; // "HideTroubleshooting", not "HideHelpTroubleshooting"
            sb.Append(Cap(part));
        }
        return sb.ToString();
    }

    void OnSave(object? s, RoutedEventArgs e)
    {
        _config.default_powershell_path = PowerShellPath.Text?.Trim() ?? "";
        _config.default_python_path = PythonPath.Text?.Trim() ?? "";
        _config.default_cmd_path = CmdPath.Text?.Trim() ?? "";
        _config.default_shell_path = ShellPath.Text?.Trim() ?? "";
        _config.powershell_arguments = PowerShellArgs.Text?.Trim() ?? "";
        _config.console_background = ConsoleBackgroundBox.Text?.Trim() ?? "";
        _config.console_foreground = ConsoleForegroundBox.Text?.Trim() ?? "";
        _config.powershell_native_colors = NativeColors.IsChecked == true;
        _config.clear_events_when_reload = ClearEvents.IsChecked == true;
        foreach (var (p, box) in _restrictions) p.SetValue(_config.restrictions, box.IsChecked == true);
        Close(true);
    }

    void OnCancel(object? s, RoutedEventArgs e) => Close(false);
}
