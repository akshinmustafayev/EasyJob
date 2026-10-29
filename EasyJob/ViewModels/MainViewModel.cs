using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyJob.Dialogs;
using EasyJob.Models;
using EasyJob.Services;
using EasyJob.Views;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace EasyJob.ViewModels;

public record EventEntry(DateTime Time, string Text)
{
    public string TimeText => Time.ToString("HH:mm:ss");
}

public partial class MainViewModel : ObservableObject
{
    readonly DispatcherTimer _clock;
    ConfigButton? _clipboardButton;

    public static MainViewModel? Current { get; private set; }

    public MainViewModel()
    {
        Current = this;
        Config = new AppConfig();
        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) =>
        {
            foreach (var tab in Tabs)
                foreach (var run in tab.Runs.Where(r => r.IsRunning))
                    run.Tick();
        };
        _clock.Start();
    }

    public AppConfig Config { get; private set; }
    public ObservableCollection<TabViewModel> Tabs { get; } = new();
    public ObservableCollection<EventEntry> Events { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSelectedTab))]
    TabViewModel? _selectedTab;

    [ObservableProperty] bool _isEventsOpen;
    [ObservableProperty] string _statusText = "";

    public bool HasSelectedTab => SelectedTab != null;
    public bool HasTabs => Tabs.Count > 0;
    public string ConfigPath => ConfigService.ConfigPath;

    // ---- restrictions (bound by the menu) ----
    ConfigRestrictions R => Config.restrictions;
    public bool ShowReloadConfig => !R.hide_menu_item_file_reload_config;
    public bool ShowOpenAppFolder => !R.hide_menu_item_file_open_app_folder;
    public bool ShowClearEvents => !R.hide_menu_item_file_clear_events_list;
    public bool ShowFileSeparator => ShowReloadConfig || ShowOpenAppFolder || ShowClearEvents;
    public bool ShowSettings => !R.hide_menu_item_settings;
    public bool ShowWorkflow => !R.hide_menu_item_settings_workflow;
    public bool ShowReorderTabs => !R.hide_menu_item_settings_workflow_reorder_tabs && !R.block_tabs_add;
    public bool ShowAddTab => !R.hide_menu_item_settings_workflow_add_tab && !R.block_tabs_add;
    public bool ShowRemoveTab => !R.hide_menu_item_settings_workflow_remove_current_tab && !R.block_tabs_remove;
    public bool ShowRenameTab => !R.hide_menu_item_settings_workflow_rename_current_tab && !R.block_tabs_rename;
    public bool ShowAddButton => !R.hide_menu_item_settings_workflow_add_button_to_current_tab && !R.block_buttons_add;
    public bool ShowReorderButtons => !R.hide_menu_item_settings_workflow_reorder_buttons_in_current_tab && !R.block_buttons_reorder;
    public bool ShowConfiguration => !R.hide_menu_item_settings_configuration;
    public bool ShowHelp => !R.hide_menu_item_help;
    public bool ShowTroubleshooting => !R.hide_menu_item_help_troubleshooting;
    public bool ShowAbout => !R.hide_menu_item_help_about;
    public bool ShowConfigurationItem => ShowSettings && ShowConfiguration;

    public bool CanAddTab => !R.block_tabs_add;
    public bool CanRemoveTab => !R.block_tabs_remove;
    public bool CanRenameTab => !R.block_tabs_rename;
    public bool CanAddButton => !R.block_buttons_add;
    public bool CanEditButton => !R.block_buttons_edit;
    public bool CanRemoveButton => !R.block_buttons_remove;
    public bool CanCopyButton => !R.block_buttons_copy;
    public bool CanPasteButton => !R.block_buttons_paste && _clipboardButton != null;
    public bool CanReorderButtons => !R.block_buttons_reorder;
    public bool AnyTabMenu => CanAddTab || CanRemoveTab || CanRenameTab;
    public bool AnyPanelMenu => CanAddButton || CanReorderButtons || !R.block_buttons_paste;

    public string ConsoleBackground => Config.console_background;
    public string ConsoleForeground => Config.console_foreground;

    // ---------------------------------------------------------------- config

    public async Task InitializeAsync()
    {
        try
        {
            LoadConfig(keepSelection: false);
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Could not load configuration", $"{ConfigService.ConfigPath}\n\n{ex.Message}");
            Config = new AppConfig();
            Tabs.Clear();
        }
    }

    void LoadConfig(bool keepSelection)
    {
        var selectedId = keepSelection ? SelectedTab?.Id : null;
        var running = Tabs.ToDictionary(t => t.Id, t => t.Runs.ToList());

        Config = ConfigService.Load();
        Tabs.Clear();
        foreach (var model in Config.tabs)
        {
            var tab = new TabViewModel(model);
            // Keep runs (and their output) alive across a config reload.
            if (running.TryGetValue(tab.Id, out var runs))
                foreach (var r in runs) tab.Runs.Add(r);
            Tabs.Add(tab);
        }
        SelectedTab = Tabs.FirstOrDefault(t => t.Id == selectedId) ?? Tabs.FirstOrDefault();
        ApplyTheme();
        RefreshRestrictions();
        Log("Config loaded from " + ConfigService.ConfigPath);
    }

    public void RefreshRestrictions()
    {
        OnPropertyChanged(string.Empty);
    }

    bool Save()
    {
        try
        {
            Config.tabs = Tabs.Select(t => t.ToModel()).ToList();
            ConfigService.Save(Config);
            return true;
        }
        catch (Exception ex)
        {
            Log("Saving config failed: " + ex.Message);
            _ = DialogService.ShowMessage("Could not save configuration", ex.Message);
            return false;
        }
    }

    public void Log(string text)
    {
        void Add()
        {
            Events.Add(new EventEntry(DateTime.Now, text));
            while (Events.Count > 2000) Events.RemoveAt(0);
            StatusText = text;
        }
        if (Dispatcher.UIThread.CheckAccess()) Add(); else Dispatcher.UIThread.Post(Add);
    }

    // ---------------------------------------------------------------- file menu

    [RelayCommand]
    void ReloadConfig()
    {
        try
        {
            if (Config.clear_events_when_reload) Events.Clear();
            LoadConfig(keepSelection: true);
        }
        catch (Exception ex)
        {
            Log("Reloading config failed: " + ex.Message);
            _ = DialogService.ShowMessage("Reloading config failed", ex.Message);
        }
    }

    [RelayCommand]
    void OpenAppFolder()
    {
        try { Launcher.Open(ConfigService.BaseDirectory); }
        catch (Exception ex) { Log("Could not open folder: " + ex.Message); }
    }

    [RelayCommand] void ClearEvents() => Events.Clear();
    [RelayCommand] void ToggleEvents() => IsEventsOpen = !IsEventsOpen;

    [RelayCommand]
    async Task Exit()
    {
        if (await ConfirmCloseAsync()) App.Shutdown();
    }

    /// <summary>Called when the window is about to close. Stops running scripts if the user agrees.</summary>
    public async Task<bool> ConfirmCloseAsync()
    {
        var running = Tabs.SelectMany(t => t.Runs).Where(r => r.IsRunning).ToList();
        if (running.Count == 0) return true;

        var ok = await DialogService.Confirm("Scripts are still running",
            $"{running.Count} script(s) are still running. Quit and stop them?", "Stop and quit");
        if (!ok) return false;
        foreach (var r in running) r.Stop();
        return true;
    }

    // ---------------------------------------------------------------- tabs

    [RelayCommand]
    async Task AddTab()
    {
        var name = await DialogService.PromptText("New tab", "Tab name", "", "Create");
        if (string.IsNullOrWhiteSpace(name)) return;
        var tab = new TabViewModel(new ConfigTab { Header = name.Trim() });
        Tabs.Add(tab);
        SelectedTab = tab;
        if (Save()) Log($"Tab '{tab.Header}' added");
    }

    [RelayCommand]
    async Task RenameTab(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        var name = await DialogService.PromptText("Rename tab", "Tab name", tab.Header, "Rename");
        if (string.IsNullOrWhiteSpace(name)) return;
        tab.Header = name.Trim();
        Save();
    }

    [RelayCommand]
    async Task RemoveTab(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;

        var running = tab.Runs.Count(r => r.IsRunning);
        var message = $"Remove tab '{tab.Header}' and its {tab.Buttons.Count} button(s)?";
        if (running > 0) message += $"\n\n{running} running script(s) in this tab will be stopped.";
        if (!await DialogService.Confirm("Remove tab", message, "Remove")) return;

        foreach (var r in tab.Runs.ToList()) r.Stop();
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Min(index, Tabs.Count - 1)];
        if (Save()) Log($"Tab '{tab.Header}' removed");
    }

    [RelayCommand]
    async Task ReorderTabs()
    {
        var order = await DialogService.Reorder("Reorder tabs", Tabs.ToList(), t => t.Header);
        if (order == null) return;
        var selected = SelectedTab;
        Tabs.Clear();
        foreach (var t in order) Tabs.Add(t);
        SelectedTab = selected;
        Save();
    }

    /// <summary>Drag-and-drop: puts <paramref name="source"/> where <paramref name="target"/> is.</summary>
    public void MoveTab(TabViewModel source, TabViewModel target)
    {
        int from = Tabs.IndexOf(source), to = Tabs.IndexOf(target);
        if (from < 0 || to < 0 || from == to) return;
        var selected = SelectedTab;
        Tabs.Move(from, to);
        SelectedTab = selected;
        Save();
    }

    public void MoveButton(ButtonViewModel source, ButtonViewModel target)
    {
        var tab = Tabs.FirstOrDefault(t => t.Buttons.Contains(source) && t.Buttons.Contains(target));
        if (tab == null) return;
        int from = tab.Buttons.IndexOf(source), to = tab.Buttons.IndexOf(target);
        if (from == to) return;
        tab.Buttons.Move(from, to);
        Save();
    }

    // ---------------------------------------------------------------- buttons

    [RelayCommand]
    async Task AddButton()
    {
        if (SelectedTab == null) return;
        var model = await DialogService.EditButton(null);
        if (model == null) return;
        SelectedTab.Buttons.Add(new ButtonViewModel(model));
        SelectedTab.RefreshButtons();
        if (Save()) Log($"Button '{model.Text}' added");
    }

    [RelayCommand]
    async Task EditButton(ButtonViewModel? button)
    {
        if (button == null) return;
        var model = await DialogService.EditButton(button.Model);
        if (model == null) return;
        model.Id = button.Model.Id;
        button.Replace(model);
        Tabs.FirstOrDefault(t => t.Buttons.Contains(button))?.RefreshButtons();
        Save();
    }

    [RelayCommand]
    async Task RemoveButton(ButtonViewModel? button)
    {
        if (button == null) return;
        var tab = Tabs.FirstOrDefault(t => t.Buttons.Contains(button));
        if (tab == null) return;
        if (!await DialogService.Confirm("Remove button", $"Remove button '{button.Text}'?", "Remove")) return;
        tab.Buttons.Remove(button);
        tab.RefreshButtons();
        Save();
    }

    [RelayCommand]
    void CopyButton(ButtonViewModel? button)
    {
        if (button == null) return;
        _clipboardButton = button.Model;
        OnPropertyChanged(nameof(CanPasteButton));
        Log($"Button '{button.Text}' copied");
    }

    [RelayCommand]
    void PasteButton()
    {
        if (_clipboardButton == null || SelectedTab == null) return;
        SelectedTab.Buttons.Add(new ButtonViewModel(_clipboardButton.Clone()));
        SelectedTab.RefreshButtons();
        Save();
    }

    [RelayCommand]
    async Task ReorderButtons()
    {
        var tab = SelectedTab;
        if (tab == null) return;
        var order = await DialogService.Reorder($"Reorder buttons in '{tab.Header}'", tab.Buttons.ToList(), b => b.Text);
        if (order == null) return;
        tab.Buttons.Clear();
        foreach (var b in order) tab.Buttons.Add(b);
        Save();
    }

    [RelayCommand]
    void OpenScript(ButtonViewModel? button)
    {
        if (button == null) return;
        var path = ScriptCommand.ResolveScriptPath(button.Model);
        try { Launcher.Open(path); Log("Opened script " + path); }
        catch (Exception ex) { Log("Could not open script: " + ex.Message); _ = DialogService.ShowMessage("Could not open script", ex.Message); }
    }

    [RelayCommand]
    void RevealScript(ButtonViewModel? button)
    {
        if (button == null) return;
        var path = ScriptCommand.ResolveScriptPath(button.Model);
        try { Launcher.Reveal(path); Log("Revealed script " + path); }
        catch (Exception ex) { Log("Could not open folder: " + ex.Message); _ = DialogService.ShowMessage("Could not open folder", ex.Message); }
    }

    // ---------------------------------------------------------------- running

    /// <summary>Runs a button, showing the output in <paramref name="target"/> (the current tab by default).</summary>
    public async Task RunButtonAsync(ButtonViewModel button, TabViewModel? target = null)
    {
        target ??= SelectedTab;
        if (target == null) return;

        var answers = new List<string>();
        if (button.Model.Arguments.Count > 0)
        {
            var result = await DialogService.AskArguments(button.Model);
            if (result == null) { Log($"Run of '{button.Text}' cancelled"); return; }
            answers = result;
        }
        StartRun(target, button.Model, answers);
    }

    void StartRun(TabViewModel target, ConfigButton model, IReadOnlyList<string> answers)
    {
        var run = new RunViewModel(this, target, model, answers);
        // Finished runs collapse so the newest one gets the space.
        foreach (var other in target.Runs.Where(r => r.IsFinished)) other.IsExpanded = false;
        target.Runs.Insert(0, run);
        _ = run.StartAsync();
    }

    [RelayCommand]
    void RunInTab(object? parameter)
    {
        if (parameter is not object[] { Length: 2 } p || p[0] is not ButtonViewModel b || p[1] is not TabViewModel t) return;
        _ = RunButtonAsync(b, t);
    }

    [RelayCommand]
    void StopRun(RunViewModel? run) => run?.Stop();

    [RelayCommand]
    void Rerun(RunViewModel? run)
    {
        if (run == null || run.IsRunning) return;
        StartRun(run.Tab, run.Button, run.Answers);
    }

    [RelayCommand]
    void DismissRun(RunViewModel? run)
    {
        if (run == null || run.IsRunning) return;
        run.Tab.Runs.Remove(run);
    }

    [RelayCommand]
    void ClearFinished(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        foreach (var r in tab.Runs.Where(r => r.IsFinished).ToList()) tab.Runs.Remove(r);
    }

    [RelayCommand]
    void StopAll(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        foreach (var r in tab.Runs.Where(r => r.IsRunning)) r.Stop();
    }

    [RelayCommand]
    void CollapseAll(TabViewModel? tab)
    {
        foreach (var r in (tab ?? SelectedTab)?.Runs ?? Enumerable.Empty<RunViewModel>()) r.IsExpanded = false;
    }

    [RelayCommand]
    void ExpandAll(TabViewModel? tab)
    {
        foreach (var r in (tab ?? SelectedTab)?.Runs ?? Enumerable.Empty<RunViewModel>()) r.IsExpanded = true;
    }

    public void NotifyRunFinished(RunViewModel run)
    {
        run.Tab.RefreshRunning();
    }

    // ---------------------------------------------------------------- settings / help

    [RelayCommand]
    async Task Configuration()
    {
        var changed = await DialogService.EditConfiguration(Config);
        if (!changed) return;
        Save();
        RefreshRestrictions();
        Log("Configuration saved");
    }

    // ---------------------------------------------------------------- editor & tools

    [RelayCommand] void OpenEditor() => EditorWindow.ShowEditor();
    [RelayCommand] void NewScript() => EditorWindow.ShowEditor(null, EditorLanguage.PowerShell);
    [RelayCommand] void ShowCommands() => EditorWindow.ShowEditor(showCommands: true);

    [RelayCommand]
    async Task OpenFileInEditor()
    {
        var owner = App.ActiveWindow;
        if (owner == null) return;
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open file in the script editor",
            AllowMultiple = true,
            SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(ConfigService.BaseDirectory)
        });
        foreach (var f in files)
            if (f.TryGetLocalPath() is { } path) EditorWindow.ShowEditor(path);
    }

    [RelayCommand]
    async Task EditScript(ButtonViewModel? button)
    {
        if (button == null) return;
        var path = ScriptCommand.ResolveScriptPath(button.Model);
        if (!File.Exists(path))
        {
            await DialogService.ShowMessage("Script not found", path);
            return;
        }
        EditorWindow.ShowEditor(path);
    }

    // ---------------------------------------------------------------- theme

    public bool IsThemeSystem => ThemeName == "system";
    public bool IsThemeLight => ThemeName == "light";
    public bool IsThemeDark => ThemeName == "dark";
    string ThemeName => (Config.theme ?? "system").ToLowerInvariant();

    public void ApplyTheme()
    {
        if (Avalonia.Application.Current is { } app)
            app.RequestedThemeVariant = ThemeName switch { "light" => ThemeVariant.Light, "dark" => ThemeVariant.Dark, _ => ThemeVariant.Default };
    }

    [RelayCommand]
    void SetTheme(string? theme)
    {
        Config.theme = theme ?? "system";
        ApplyTheme();
        OnPropertyChanged(nameof(IsThemeSystem));
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeDark));
        Save();
    }

    [RelayCommand] Task About() => DialogService.ShowAbout();
    [RelayCommand] Task Troubleshooting() => DialogService.ShowTroubleshooting(Config);
    [RelayCommand] void OpenGitHub() => Launcher.Open("https://github.com/akshinmustafayev/EasyJob");
}
