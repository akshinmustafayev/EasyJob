using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        CheckUpdates.IsChecked = config.check_updates_on_startup;
        ClearEvents.IsChecked = config.clear_events_when_reload;

        // cmd.exe only exists on Windows; the POSIX shell setting only matters elsewhere.
        CmdRow.IsVisible = CmdPath.IsVisible = ShellResolver.IsWindows;
        ShellLabel.IsVisible = ShellPath.IsVisible = !ShellResolver.IsWindows;

        LinkPicker(ConsoleBackgroundBox, ConsoleBackgroundPicker, Colors.Black);
        LinkPicker(ConsoleForegroundBox, ConsoleForegroundPicker, Colors.White);
        UpdatePreview();
        BuildRestrictions();
    }

    /// <summary>
    /// Keeps a hex/name text box and a color swatch button showing the same color, whichever one is edited.
    /// The button opens the spectrum / palette / sliders picker in a flyout (no dropdown arrow).
    /// </summary>
    void LinkPicker(TextBox box, Button swatchButton, Color fallback)
    {
        var swatch = new Border { CornerRadius = new Avalonia.CornerRadius(4), BorderThickness = new Avalonia.Thickness(1) };
        swatch.Bind(Border.BorderBrushProperty, this.GetResourceObservable("BorderBrushSoft"));
        swatchButton.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        swatchButton.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        swatchButton.Content = swatch;

        var view = new ColorView { IsAlphaVisible = false, IsAlphaEnabled = false, IsColorModelVisible = false, Width = 320 };
        swatchButton.Flyout = new Flyout { Content = view, Placement = PlacementMode.BottomEdgeAlignedRight };

        bool syncing = false;
        void FromText()
        {
            if (syncing) return;
            syncing = true;
            var color = !string.IsNullOrWhiteSpace(box.Text) && Color.TryParse(box.Text.Trim(), out var c) ? c : fallback;
            view.Color = color;
            swatch.Background = new SolidColorBrush(color);
            syncing = false;
            UpdatePreview();
        }
        box.TextChanged += (_, _) => FromText();
        view.ColorChanged += (_, e) =>
        {
            if (syncing) return;
            syncing = true;
            var c = e.NewColor;
            box.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            swatch.Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            syncing = false;
            UpdatePreview();
        };
        FromText();
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
        if (PolicyService.IsManaged)
        {
            var note = new TextBlock { Text = "Settings marked with a lock are set by your administrator and cannot be changed here.", TextWrapping = TextWrapping.Wrap };
            note.Classes.Add("hint");
            RestrictionsPanel.Children.Add(note);
        }
        AddGroup("ADMINISTRATION", Props().Where(p => !p.Name.StartsWith("block_") && !p.Name.StartsWith("hide_")));
        AddGroup("BLOCK ACTIONS", Props().Where(p => p.Name.StartsWith("block_")));
        AddGroup("HIDE MENU ITEMS", Props().Where(p => p.Name.StartsWith("hide_")));
        LinkEditorOptions();
    }

    // What the editor options were set to before "Block the script editor" switched them off, so unblocking restores them.
    readonly Dictionary<string, bool> _editorOptionValues = new();

    /// <summary>"Block the script editor" makes the editor's own options meaningless (read-only mode cannot apply to a window that never opens), so they are switched off and locked while it is on.</summary>
    void LinkEditorOptions()
    {
        var block = _restrictions.FirstOrDefault(r => r.Property.Name == "block_script_editor");
        if (block.Box == null) return;
        var own = _config.UserRestrictions ?? _config.restrictions;
        foreach (var name in PolicyService.EditorOptions)
            _editorOptionValues[name] = (bool)typeof(ConfigRestrictions).GetProperty(name)!.GetValue(own)!;

        void Apply()
        {
            bool blocked = block.Box.IsChecked == true;
            foreach (var (p, box) in _restrictions)
            {
                if (!PolicyService.EditorOptions.Contains(p.Name)) continue;
                if (blocked)
                {
                    if (box.IsEnabled) _editorOptionValues[p.Name] = box.IsChecked == true;
                    box.IsChecked = false;
                    box.IsEnabled = false;
                    ToolTip.SetTip(box, "Not used while the script editor is blocked");
                }
                else
                {
                    bool forced = PolicyService.IsForced(p.Name);
                    box.IsEnabled = !forced;
                    box.IsChecked = forced || _editorOptionValues[p.Name];
                    ToolTip.SetTip(box, forced ? "Set by your administrator" : null);
                }
            }
        }
        block.Box.IsCheckedChanged += (_, _) => Apply();
        if (block.Box.IsChecked == true) Apply();
    }

    static readonly Dictionary<string, string> Labels = new()
    {
        ["block_tabs_remove"] = "Block removing tabs",
        ["block_buttons_remove"] = "Block removing script buttons",
        ["block_tabs_add"] = "Block adding tabs",
        ["block_buttons_add"] = "Block adding script buttons",
        ["block_buttons_reorder"] = "Block reordering script buttons",
        ["block_buttons_edit"] = "Block editing script buttons",
        ["block_tabs_rename"] = "Block renaming tabs",
        ["block_buttons_paste"] = "Block pasting script buttons",
        ["block_buttons_copy"] = "Block copying script buttons",
        ["hide_menu_item_file_reload_config"] = "Hide File → Reload config",
        ["hide_menu_item_file_open_app_folder"] = "Hide File → Open config folder",
        ["hide_menu_item_file_clear_events_list"] = "Hide View → Clear event log",
        ["hide_menu_item_settings"] = "Hide the settings group (Tools → Configuration)",
        ["hide_menu_item_settings_workflow"] = "Hide the Tabs and Scripts menus",
        ["hide_menu_item_settings_workflow_reorder_tabs"] = "Hide Tabs → Reorder tabs",
        ["hide_menu_item_settings_workflow_add_tab"] = "Hide Tabs → New tab",
        ["hide_menu_item_settings_workflow_remove_current_tab"] = "Hide Tabs → Remove current tab",
        ["hide_menu_item_settings_workflow_rename_current_tab"] = "Hide Tabs → Rename current tab",
        ["hide_menu_item_settings_workflow_add_button_to_current_tab"] = "Hide Scripts → Add script button",
        ["hide_menu_item_settings_workflow_reorder_buttons_in_current_tab"] = "Hide Scripts → Reorder script buttons",
        ["hide_menu_item_settings_configuration"] = "Hide Tools → Configuration",
        ["hide_menu_item_help"] = "Hide the Help menu",
        ["hide_menu_item_help_troubleshooting"] = "Hide Help → Troubleshooting",
        ["hide_menu_item_help_about"] = "Hide Help → About EasyJob",
        ["hide_menu_item_tools"] = "Hide the Tools menu",
        ["hide_menu_item_view_event_log"] = "Hide the event log",
        ["config_readonly"] = "Read-only configuration (nothing can be changed or saved)",
        ["script_editor_readonly"] = "Script editor opens files read-only",
        ["confirm_before_run"] = "Ask for confirmation before every run",
        ["block_script_editor"] = "Block the script editor",
        ["block_script_editor_run"] = "Block running scripts from the editor",
        ["block_script_editor_open_files"] = "Block opening other files in the editor",
        ["block_powershell_commands"] = "Block the PowerShell command browser",
        ["block_open_script_externally"] = "Block \"Open with default app\"",
        ["block_show_in_folder"] = "Block \"Show in folder\"",
        ["block_updates"] = "Block update checks and updates",
        ["block_view_settings"] = "Block theme and button size changes",
        ["hide_menu_item_tools"] = "Hide the Tools menu",
        ["hide_menu_item_view_event_log"] = "Hide the event log"
    };

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
        bool forced = PolicyService.IsForced(p.Name);
        var own = _config.UserRestrictions ?? _config.restrictions; // the user's own value, without what the policy adds
        var box = new CheckBox { Content = Labels.GetValueOrDefault(p.Name) ?? Describe(p.Name), IsChecked = forced || (bool)p.GetValue(own)!, IsEnabled = !forced };
        if (forced) ToolTip.SetTip(box, "Set by your administrator");
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
        _config.check_updates_on_startup = CheckUpdates.IsChecked == true;
        _config.clear_events_when_reload = ClearEvents.IsChecked == true;
        _config.UserRestrictions ??= _config.restrictions;
        bool editorBlocked = _restrictions.Any(r => r.Property.Name == "block_script_editor" && r.Box.IsChecked == true);
        foreach (var (p, box) in _restrictions)
        {
            if (PolicyService.IsForced(p.Name)) continue;
            // While the editor is blocked its options are shown off; keep what the user had chosen for when it is unblocked.
            bool value = editorBlocked && PolicyService.EditorOptions.Contains(p.Name) ? _editorOptionValues[p.Name] : box.IsChecked == true;
            p.SetValue(_config.UserRestrictions, value);
        }
        PolicyService.Recompute(_config);
        Close(true);
    }

    void OnCancel(object? s, RoutedEventArgs e) => Close(false);
}
