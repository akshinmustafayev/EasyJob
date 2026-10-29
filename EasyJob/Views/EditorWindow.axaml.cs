using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Search;
using AvaloniaEdit.TextMate;
using EasyJob.Dialogs;
using EasyJob.Models;
using EasyJob.Services;
using EasyJob.ViewModels;
using TextMateSharp.Grammars;

namespace EasyJob.Views;

/// <summary>A small script IDE: edit, syntax-color by language, save, and run with live output.</summary>
public partial class EditorWindow : Window
{
    static EditorWindow? _current;
    public static EditorWindow? Current => _current;

    readonly System.Collections.ObjectModel.ObservableCollection<EditorDocument> _docs = new();
    readonly TabViewModel _runs = new(new ConfigTab { Header = "Editor" });
    readonly RegistryOptions _registry = new(ThemeName.DarkPlus);
    readonly TextMate.Installation _textMate;
    internal static bool? _sideOpenLast; // the panel state of the last editor window; null until one was used
    bool _sideOpen;
    readonly List<string> _tempFiles = new();
    bool _switching;
    bool _closeConfirmed;
    EditorDocument? _current_doc;

    // Administrator limits (see PolicyService); read once when the window opens.
    static ConfigRestrictions Limits => MainViewModel.Current?.Config.restrictions ?? new ConfigRestrictions();
    readonly bool _readOnly = Limits.script_editor_readonly;
    readonly bool _canRun = !Limits.block_script_editor_run;
    readonly bool _canOpenFiles = !Limits.block_script_editor_open_files;
    readonly bool _canUseCommands = !Limits.block_powershell_commands;

    public bool HasUnsaved => _docs.Any(d => d.IsDirty);

    /// <summary>Opens the editor (or brings it to the front) and shows <paramref name="path"/>, or a new script if null.</summary>
    public static EditorWindow? ShowEditor(string? path = null, EditorLanguage? newLanguage = null, bool showCommands = false)
    {
        if (MainViewModel.Current is { CanUseEditor: false })
        {
            _ = DialogService.ShowMessage("Script editor", "The script editor is turned off by your administrator.");
            return null;
        }
        var window = _current ?? new EditorWindow();
        if (!window.IsVisible) window.Show();
        window.Activate();
        if (showCommands) window.ShowCommandBrowser();
        if (path != null) window.OpenFile(path);
        else if (newLanguage != null) window.NewDocument(newLanguage);
        return window;
    }

    public EditorWindow()
    {
        InitializeComponent();
        _current = this;

        _textMate = Editor.InstallTextMate(_registry);
        SearchPanel.Install(Editor);
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.IndentationSize = 4;
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.EnableHyperlinks = false;
        Editor.Options.EnableEmailHyperlinks = false;

        LanguageBox.ItemsSource = EditorLanguage.All;
        DocList.ItemsSource = _docs;
        RunsList.ItemsSource = _runs.Runs;
        _runs.Runs.CollectionChanged += (_, e) =>
        {
            if (_runs.Runs.Count > 0 && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) { ShowOutput(true); SelectOutputTab(false); }
            UpdateRunButtons();
        };

        DocList.SelectionChanged += (_, _) => SelectDocument(DocList.SelectedItem as EditorDocument);
        LanguageBox.SelectionChanged += (_, _) =>
        {
            if (_switching || _current_doc == null || LanguageBox.SelectedItem is not EditorLanguage l) return;
            _current_doc.Language = l;
            ApplyLanguage(l);
        };
        WrapToggle.IsCheckedChanged += (_, _) => Editor.WordWrap = WrapToggle.IsChecked == true;
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdatePosition();
        ActualThemeVariantChanged += (_, _) => ApplyTheme();

        var mod = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        void Key(Avalonia.Input.Key key, KeyModifiers m, Action a) =>
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(key, m), Command = new RelayAction(a) });
        Key(Avalonia.Input.Key.S, mod, () => _ = SaveAsync(_current_doc));
        Key(Avalonia.Input.Key.O, mod, () => _ = OpenDialogAsync());
        Key(Avalonia.Input.Key.W, mod, () => _ = CloseDocumentAsync(_current_doc));
        Key(Avalonia.Input.Key.N, mod, () => NewDocument(_current_doc?.Language.CanRun == true ? _current_doc.Language : EditorLanguage.PowerShell));
        Key(Avalonia.Input.Key.F5, KeyModifiers.None, () => _ = RunAsync());
        Key(Avalonia.Input.Key.OemTilde, mod, () => { if (_canTerminal) TerminalToggle.IsChecked = TerminalToggle.IsChecked != true; });
        Key(Avalonia.Input.Key.P, mod | KeyModifiers.Shift, () => { if (SideAvailable != SideKind.None) CommandsToggle.IsChecked = CommandsToggle.IsChecked != true; });

        Commands.InsertRequested += name => InsertText(name);
        Editor.IsReadOnly = _readOnly;
        LanguageBox.IsEnabled = !_readOnly;
        NewButton.IsVisible = SaveButton.IsVisible = EmptyNewButton.IsVisible = !_readOnly;
        OpenButton.IsVisible = EmptyOpenButton.IsVisible = _canOpenFiles;
        RunBar.IsVisible = _canRun;
        _sideOpen = _sideOpenLast ?? MainViewModel.Current?.Config.editor_open_commands_panel ?? true;
        Editor.TextChanged += (_, _) => { switch (SideAvailable)
        {
            case SideKind.Json: JsonView.Update(Editor.Text); break;
            case SideKind.Markdown: MarkdownView.Update(Editor.Text); break;
        } };
        RefreshSidePanel();

        ApplyTheme();
        InitTerminal();
        if (_canTerminal && MainViewModel.Current?.Config.editor_open_terminal != false)
            Opened += (_, _) => TerminalToggle.IsChecked = true;
        Closed += (_, _) =>
        {
            if (_current == this) _current = null;
            foreach (var f in _tempFiles) try { File.Delete(f); } catch { }
        };
        SelectDocument(null);
    }

    sealed class RelayAction : System.Windows.Input.ICommand
    {
        readonly Action _a;
        public RelayAction(Action a) => _a = a;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? p) => true;
        public void Execute(object? p) => _a();
    }

    // ------------------------------------------------------------ documents

    public void NewDocument(EditorLanguage language)
    {
        if (_readOnly) return;
        var doc = new EditorDocument(null, language.Template, language);
        _docs.Add(doc);
        DocList.SelectedItem = doc;
        Editor.Focus();
    }

    public void OpenFile(string path)
    {
        path = Path.GetFullPath(path);
        if (MainViewModel.Current is { } main && !PolicyService.IsPathAllowed(main.Config, path))
        {
            _ = DialogService.ShowMessage("Not allowed", "This file is outside the folders your administrator allows.");
            return;
        }
        var existing = _docs.FirstOrDefault(d => d.Path == path);
        if (existing != null) { DocList.SelectedItem = existing; return; }

        try
        {
            var bytes = File.ReadAllBytes(path);
            bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = new UTF8Encoding(false).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
            var doc = new EditorDocument(path, text, EditorLanguage.ForPath(path), bom);
            _docs.Add(doc);
            DocList.SelectedItem = doc;
            Editor.Focus();
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowMessage("Could not open file", $"{path}\n\n{ex.Message}");
        }
    }

    /// <summary>The command browser belongs to PowerShell files: switch to one (a new script if needed) and open the panel.</summary>
    void ShowCommandBrowser()
    {
        if (!_canUseCommands) return;
        if (_current_doc?.Language != EditorLanguage.PowerShell && !_readOnly)
            NewDocument(EditorLanguage.PowerShell);
        ShowCommandsPanel(true);
    }

    /// <summary>Inserts text at the caret (used by the PowerShell command browser).</summary>
    public void InsertText(string text)
    {
        if (_readOnly) return;
        if (_current_doc == null) NewDocument(EditorLanguage.PowerShell);
        Editor.Document.Insert(Editor.CaretOffset, text);
        Editor.Focus();
    }

    void SelectDocument(EditorDocument? doc)
    {
        _switching = true;
        try
        {
            _current_doc = doc;
            Editor.IsVisible = doc != null;
            EmptyState.IsVisible = doc == null;
            DocList.IsVisible = _docs.Count > 0;
            if (doc == null)
            {
                Title = "Script editor";
                UpdateStatus();
                RefreshSidePanel();
                return;
            }
            Editor.Document = doc.Document;
            LanguageBox.SelectedItem = doc.Language;
            ApplyLanguage(doc.Language);
            Title = doc.Title + (_readOnly ? " (read-only)" : "") + " — Script editor";
            doc.PropertyChanged -= OnDocChanged;
            doc.PropertyChanged += OnDocChanged;
            UpdateStatus();
            UpdatePosition();
        }
        finally { _switching = false; }
        UpdateRunButtons();
    }

    void OnDocChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender == _current_doc && e.PropertyName is nameof(EditorDocument.Title) or nameof(EditorDocument.IsDirty))
            Title = (_current_doc!.IsDirty ? "● " : "") + _current_doc.Title + " — Script editor";
        UpdateStatus();
    }

    // ------------------------------------------------------------ highlighting

    void ApplyLanguage(EditorLanguage language)
    {
        try
        {
            if (language.TextMateId != null)
                _textMate.SetGrammar(_registry.GetScopeByLanguageId(language.TextMateId));
            else
                _textMate.SetGrammar(null);
        }
        catch { /* highlighting is a nicety: never fail editing because of it */ }
        UpdateStatus();
        UpdateRunButtons();
        RefreshSidePanel();
    }

    void ApplyTheme()
    {
        bool dark = ActualThemeVariant == ThemeVariant.Dark;
        try { _textMate.SetTheme(_registry.LoadTheme(dark ? ThemeName.DarkPlus : ThemeName.LightPlus)); } catch { }
        JsonView.ApplyTheme(dark);
        Editor.Background = new SolidColorBrush(Color.Parse(dark ? "#1E1E1E" : "#FFFFFF"));
        Editor.Foreground = new SolidColorBrush(Color.Parse(dark ? "#D4D4D4" : "#1F2328"));
        Editor.LineNumbersForeground = new SolidColorBrush(Color.Parse(dark ? "#6E7681" : "#8C959F"));
    }

    // ------------------------------------------------------------ status

    void UpdatePosition()
    {
        var c = Editor.TextArea.Caret;
        PositionText.Text = $"Ln {c.Line}, Col {c.Column}";
    }

    void UpdateStatus()
    {
        PathText.Text = _current_doc?.Path ?? (_current_doc == null ? "" : "Not saved yet");
        LanguageText.Text = _current_doc?.Language.Name ?? "";
        EncodingText.Text = _current_doc == null ? "" : (_current_doc.HasBom ? "UTF-8 with BOM" : "UTF-8") + (_readOnly ? " · read-only" : "");
        SaveButton.IsEnabled = _current_doc != null && !_readOnly;
    }

    void UpdateRunButtons()
    {
        bool running = _runs.Runs.Any(r => r.IsRunning);
        RunButton.IsEnabled = _current_doc?.Language.CanRun == true && !running;
        StopButton.IsVisible = running;
        ArgsBox.IsEnabled = !running;
    }

    // ------------------------------------------------------------ file actions

    async Task<bool> SaveAsync(EditorDocument? doc, bool saveAs = false)
    {
        if (doc == null) return true;
        if (_readOnly) return false;

        var path = doc.Path;
        if (path == null || saveAs)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save script",
                SuggestedFileName = doc.Path != null ? Path.GetFileName(doc.Path) : "script" + doc.Language.Extensions[0],
                DefaultExtension = doc.Language.Extensions[0].TrimStart('.'),
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(ConfigService.BaseDirectory)
            });
            path = file?.TryGetLocalPath();
            if (path == null) return false;
        }
        if (MainViewModel.Current is { } main && !PolicyService.IsPathAllowed(main.Config, path))
        {
            await DialogService.ShowMessage("Not allowed", "Your administrator only allows saving scripts in certain folders. Pick a file inside them.");
            return false;
        }

        try
        {
            File.WriteAllText(path, doc.Document.Text, new UTF8Encoding(doc.HasBom));
            doc.Path = path;
            if (saveAs) doc.Language = EditorLanguage.ForPath(path) is { } l && l != EditorLanguage.Plain ? l : doc.Language;
            doc.MarkSaved();
            MainViewModel.Current?.Log("Saved " + path);
            if (doc == _current_doc) { LanguageBox.SelectedItem = doc.Language; UpdateStatus(); Title = doc.Title + " — Script editor"; }
            return true;
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Could not save file", $"{path}\n\n{ex.Message}");
            return false;
        }
    }

    async Task OpenDialogAsync()
    {
        if (!_canOpenFiles) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open file",
            AllowMultiple = true,
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(ConfigService.BaseDirectory)
        });
        foreach (var f in files)
            if (f.TryGetLocalPath() is { } p) OpenFile(p);
    }

    /// <summary>Closes a document, asking to save first. Returns false if the user cancelled.</summary>
    async Task<bool> CloseDocumentAsync(EditorDocument? doc)
    {
        if (doc == null) return true;
        if (doc.IsDirty)
        {
            DocList.SelectedItem = doc;
            switch (await DialogService.AskSave(doc.Title))
            {
                case DialogService.SaveChoice.Cancel: return false;
                case DialogService.SaveChoice.Save: if (!await SaveAsync(doc)) return false; break;
            }
        }
        int index = _docs.IndexOf(doc);
        _docs.Remove(doc);
        DocList.IsVisible = _docs.Count > 0;
        DocList.SelectedItem = _docs.Count == 0 ? null : _docs[Math.Min(index, _docs.Count - 1)];
        if (_docs.Count == 0) SelectDocument(null);
        return true;
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_closeConfirmed) { base.OnClosing(e); return; }
        e.Cancel = true;

        foreach (var doc in _docs.Where(d => d.IsDirty).ToList())
            if (!await CloseDocumentAsync(doc)) return;

        foreach (var r in _runs.Runs.Where(r => r.IsRunning)) r.Stop();
        _closeConfirmed = true;
        Close();
    }

    // ------------------------------------------------------------ running

    async Task RunAsync()
    {
        var doc = _current_doc;
        if (!_canRun || doc == null || !doc.Language.CanRun || _runs.Runs.Any(r => r.IsRunning) || MainViewModel.Current is not { } main) return;

        // Run exactly what is in the editor. A file that is saved and unchanged runs in place; otherwise the
        // text goes to a temporary copy (beside the file when possible, so relative paths keep working).
        string scriptPath;
        try { scriptPath = doc.Path != null && !doc.IsDirty ? doc.Path : WriteTempCopy(doc); }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Could not run script", ex.Message);
            return;
        }

        var button = new ConfigButton
        {
            Text = doc.Title,
            Script = scriptPath,
            ScriptPathType = "absolute",
            ScriptType = doc.Language.ScriptType!
        };
        if (!await main.ConfirmRunAsync(button)) return;
        foreach (var other in _runs.Runs.Where(r => r.IsFinished)) other.IsExpanded = false;
        var args = string.IsNullOrWhiteSpace(ArgsBox.Text) ? Array.Empty<string>() : new[] { ArgsBox.Text! };
        var run = new RunViewModel(main, _runs, button, args)
        {
            ScriptName = scriptPath == doc.Path ? Path.GetFileName(scriptPath) : (doc.Path != null ? Path.GetFileName(doc.Path) + " (unsaved changes)" : "unsaved")
        };
        _runs.Runs.Insert(0, run);
        run.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(RunViewModel.Status)) Dispatcher.UIThread.Post(UpdateRunButtons); };
        _ = run.StartAsync();
        UpdateRunButtons();
    }

    string WriteTempCopy(EditorDocument doc)
    {
        var ext = doc.Language.Extensions[0];
        var name = $".easyjob-run-{Guid.NewGuid():N}{ext}";
        var text = doc.Document.Text;
        var enc = new UTF8Encoding(doc.HasBom);
        string path;
        try
        {
            if (doc.Path == null) throw new InvalidOperationException();
            path = Path.Combine(Path.GetDirectoryName(doc.Path)!, name);
            File.WriteAllText(path, text, enc);
        }
        catch
        {
            path = Path.Combine(Path.GetTempPath(), name);
            File.WriteAllText(path, text, enc);
        }
        _tempFiles.Add(path);
        return path;
    }

    // The side panel depends on the language of the open file: the PowerShell command browser for PowerShell,
    // a formatted preview for JSON, nothing for the rest.
    enum SideKind { None, Commands, Json, Markdown }

    double _sideWidth = 380;

    bool _syncingToggle;

    SideKind SideAvailable =>
        _current_doc == null ? SideKind.None
        : _current_doc.Language == EditorLanguage.PowerShell && _canUseCommands ? SideKind.Commands
        : _current_doc.Language == EditorLanguage.Json ? SideKind.Json
        : _current_doc.Language == EditorLanguage.Markdown ? SideKind.Markdown
        : SideKind.None;

    void RefreshSidePanel()
    {
        var kind = SideAvailable;
        bool show = kind != SideKind.None && _sideOpen;
        _syncingToggle = true;
        CommandsToggle.IsVisible = kind != SideKind.None;
        CommandsToggle.IsChecked = show;
        _syncingToggle = false;
        (SideToggleText.Text, var tip) = kind switch
        {
            SideKind.Json => ("JSON", "Show or hide the formatted JSON"),
            SideKind.Markdown => ("Preview", "Show or hide the rendered Markdown"),
            _ => ("Commands", "Show or hide the PowerShell command browser")
        };
        ToolTip.SetTip(CommandsToggle, tip + " (Ctrl/Cmd+Shift+P)");

        // Remember the width the user dragged the panel to, and give the space back to the editor while it is hidden.
        if (SidePanel.IsVisible && MainGrid.ColumnDefinitions[2].Width.IsAbsolute && MainGrid.ColumnDefinitions[2].Width.Value > 0) _sideWidth = MainGrid.ColumnDefinitions[2].Width.Value;
        SidePanel.IsVisible = SideSplitter.IsVisible = show;
        MainGrid.ColumnDefinitions[2].MinWidth = show ? 260 : 0;
        MainGrid.ColumnDefinitions[2].Width = new GridLength(show ? _sideWidth : 0);
        Commands.IsVisible = kind == SideKind.Commands;
        JsonView.IsVisible = kind == SideKind.Json;
        MarkdownView.IsVisible = kind == SideKind.Markdown;
        if (!show) return;
        if (kind == SideKind.Commands) Commands.EnsureLoaded();
        else if (kind == SideKind.Json) JsonView.Update(Editor.Text, immediate: true);
        else MarkdownView.Update(Editor.Text, immediate: true);
    }

    void ShowCommandsPanel(bool show)
    {
        _sideOpen = show;
        _sideOpenLast = show;
        RefreshSidePanel();
    }

    // ------------------------------------------------------------ terminal

    enum TerminalShell { PowerShell, System }

    bool _canTerminal;
    bool _termTabActive;
    bool _termLaunched;

    void InitTerminal()
    {
        var main = MainViewModel.Current;
        _canTerminal = main != null && PolicyService.TerminalAllowed(main.Config);
        TerminalToggle.IsVisible = TerminalTab.IsVisible = _canTerminal;
        OutputTab.IsChecked = true;
        if (main == null || !_canTerminal) return;

        if (Color.TryParse(main.ConsoleBackground, out var bg)) { Term.Background = new SolidColorBrush(bg); TerminalView.Background = Term.Background; }
        if (Color.TryParse(main.ConsoleForeground, out var fg)) Term.Foreground = new SolidColorBrush(fg);

        if (ShellResolver.ResolvePowerShell(main.Config) != null) ShellBox.Items.Add("PowerShell");
        ShellBox.Items.Add(ShellResolver.IsWindows ? "Command Prompt" : "System shell");
        ShellBox.SelectedIndex = 0;

        Term.ProcessExited += (_, _) => Dispatcher.UIThread.Post(() => _termLaunched = false);
        Closed += (_, _) => { try { Term.Kill(); } catch { } };
    }

    string? TerminalStartDirectory() =>
        _current_doc?.Path is { } p ? Path.GetDirectoryName(p) : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    void LaunchTerminal()
    {
        var config = MainViewModel.Current!.Config;
        string exe;
        string[] args = Array.Empty<string>();
        if (SelectedShell == TerminalShell.PowerShell)
        {
            exe = ShellResolver.ResolvePowerShell(config) ?? "pwsh";
            args = new[] { "-NoLogo" };
        }
        else if (ShellResolver.IsWindows) exe = ShellResolver.ResolveShell(config) ?? "cmd.exe";
        else exe = Environment.GetEnvironmentVariable("SHELL") is { Length: > 0 } sh && File.Exists(sh) ? sh : ShellResolver.ResolveShell(config) ?? "/bin/sh";

        try { Term.Kill(); } catch { }
        var dir = TerminalStartDirectory();
        Term.LaunchProcess(dir != null && Directory.Exists(dir) ? dir : null, exe, args);
        _termLaunched = true;
        Dispatcher.UIThread.Post(() => TerminalCharsetFix.Apply(Term), DispatcherPriority.Background);
    }

    void SelectOutputTab(bool terminal)
    {
        _termTabActive = terminal && _canTerminal;
        OutputTab.IsChecked = !_termTabActive;
        TerminalTab.IsChecked = _termTabActive;
        RunsScroll.IsVisible = !_termTabActive;
        TerminalView.IsVisible = _termTabActive;
        ShellBox.IsVisible = TermRestartButton.IsVisible = _termTabActive;
        ClearOutputButton.IsVisible = !_termTabActive;
        if (TerminalToggle.IsChecked != _termTabActive && OutputPanel.IsVisible) TerminalToggle.IsChecked = _termTabActive;
        if (_termTabActive)
        {
            if (!_termLaunched) LaunchTerminal();
            Dispatcher.UIThread.Post(() => Term.Focus(), DispatcherPriority.Background);
        }
    }

    void OnTerminalToggled(object? sender, RoutedEventArgs e)
    {
        if (TerminalToggle.IsChecked == true)
        {
            ShowOutput(true);
            SelectOutputTab(true);
        }
        else if (_termTabActive)
        {
            if (_runs.Runs.Count > 0) SelectOutputTab(false); else ShowOutput(false);
        }
    }

    void OnOutputTabClick(object? sender, RoutedEventArgs e) => SelectOutputTab(false);
    void OnTerminalTabClick(object? sender, RoutedEventArgs e) => SelectOutputTab(true);

    void OnShellChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_termLaunched && _termTabActive) LaunchTerminal();
    }

    void OnTermRestartClick(object? sender, RoutedEventArgs e)
    {
        LaunchTerminal();
        Term.Focus();
    }

    TerminalShell SelectedShell => ShellBox.SelectedItem as string == "PowerShell" ? TerminalShell.PowerShell : TerminalShell.System;

    void ShowOutput(bool show)
    {
        OutputPanel.IsVisible = show;
        Splitter.IsVisible = show;
        Body.RowDefinitions[3].Height = show ? new GridLength(260) : new GridLength(0);
    }

    // ------------------------------------------------------------ handlers

    void OnNewClick(object? sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var lang in EditorLanguage.Scripts)
        {
            var l = lang;
            var item = new MenuItem { Header = $"New {l.Name} script" };
            item.Click += (_, _) => NewDocument(l);
            menu.Items.Add(item);
        }
        var plain = new MenuItem { Header = "New text file" };
        plain.Click += (_, _) => NewDocument(EditorLanguage.Plain);
        menu.Items.Add(new Separator());
        menu.Items.Add(plain);
        menu.Open(sender as Control ?? NewButton);
    }

    void OnOpenClick(object? sender, RoutedEventArgs e) => _ = OpenDialogAsync();
    void OnSaveClick(object? sender, RoutedEventArgs e) => _ = SaveAsync(_current_doc);
    void OnRunClick(object? sender, RoutedEventArgs e) => _ = RunAsync();
    void OnStopClick(object? sender, RoutedEventArgs e) { foreach (var r in _runs.Runs.Where(r => r.IsRunning)) r.Stop(); }
    void OnCommandsToggled(object? sender, RoutedEventArgs e) { if (!_syncingToggle) ShowCommandsPanel(CommandsToggle.IsChecked == true); }
    void OnHideOutputClick(object? sender, RoutedEventArgs e)
    {
        ShowOutput(false);
        TerminalToggle.IsChecked = false;
    }
    void OnClearOutputClick(object? sender, RoutedEventArgs e)
    {
        foreach (var r in _runs.Runs.Where(r => r.IsFinished).ToList()) _runs.Runs.Remove(r);
        if (_runs.Runs.Count == 0) ShowOutput(false);
    }

    void OnCloseDocClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: EditorDocument doc }) _ = CloseDocumentAsync(doc);
        e.Handled = true;
    }
}
