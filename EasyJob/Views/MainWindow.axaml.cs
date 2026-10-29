using Avalonia;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasyJob.Controls;
using EasyJob.ViewModels;

namespace EasyJob.Views;

public partial class MainWindow : Window
{
    readonly MainViewModel _vm = new();
    bool _closeConfirmed;

    /// <summary>Modifier keys held during the most recent pointer press (Ctrl/Cmd+click and Shift+click open the script).</summary>
    public static KeyModifiers LastModifiers { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        // Keyboard shortcuts use Cmd on macOS and Ctrl elsewhere; they are shown next to the menu items.
        var mod = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
        void Shortcut(MenuItem item, Key key, KeyModifiers m)
        {
            var gesture = new KeyGesture(key, m);
            // On macOS the native menu bar owns the shortcuts (a second HotKey would run the command twice).
            if (!OperatingSystem.IsMacOS()) item.HotKey = gesture;
            item.InputGesture = gesture;
        }
        Shortcut(MenuReload, Key.F5, KeyModifiers.None);
        Shortcut(MenuNewTab, Key.T, mod);
        Shortcut(MenuAddScript, Key.N, mod);
        Shortcut(MenuNewScript, Key.N, mod | KeyModifiers.Shift);
        Shortcut(MenuOpenFile, Key.O, mod);
        Shortcut(MenuEditor, Key.E, mod);
        Shortcut(MenuCommands, Key.P, mod | KeyModifiers.Shift);
        Shortcut(MenuEvents, Key.L, mod);

        AddHandler(PointerPressedEvent, (_, e) => LastModifiers = e.KeyModifiers, RoutingStrategies.Tunnel);
        _vm.Events.CollectionChanged += (_, _) =>
        {
            if (_vm.Events.Count > 0 && _vm.IsEventsOpen) EventsList.ScrollIntoView(_vm.Events[^1]);
        };
        // Drag a tab header onto another tab to reorder.
        _ = new DragReorder<TabViewModel>(Tabs, "EasyJob.Tab",
            src => DragReorder<TabViewModel>.Ancestor(src, c => c is TabItem) is TabItem { DataContext: TabViewModel t } item ? (item, t) : null,
            (source, target) => _vm.MoveTab(source, target),
            () => _vm.ShowReorderTabs && _vm.Tabs.Count > 1);

        Opened += async (_, _) =>
        {
            await _vm.InitializeAsync();
            // Export the menu once the config is loaded and the menu items have settled: building it earlier made the
            // native menu bar flicker through "everything hidden" states, and macOS once kept showing only the app menu.
            if (OperatingSystem.IsMacOS()) Dispatcher.UIThread.Post(UseNativeMenu, DispatcherPriority.Loaded);
        };
        // Hide the direct tab controls when the config forbids them.
        TabViewModel? watched = null;
        void UpdateTabActions()
        {
            NewTabButton.IsVisible = _vm.ShowAddTab;
            TabMenuButton.IsVisible = _vm.ShowRenameTab || _vm.ShowRemoveTab || _vm.ShowReorderTabs;
            AddScriptButton.IsVisible = _vm.ShowAddButton && _vm.HasSelectedTab;
            ReorderButton.IsVisible = _vm.ShowReorderButtons && _vm.SelectedTab?.CanReorder == true;
            PasteButton.IsVisible = _vm.CanPasteButton && _vm.HasSelectedTab;
            DensityButton.IsVisible = _vm.SelectedTab?.HasButtons == true && _vm.ShowViewSettings;
        }
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedTab))
            {
                if (watched != null) watched.PropertyChanged -= OnTabChanged;
                watched = _vm.SelectedTab;
                if (watched != null) watched.PropertyChanged += OnTabChanged;
                FilterToggle.IsChecked = !string.IsNullOrEmpty(watched?.Filter);
            }
            UpdateTabActions();
        };
        void OnTabChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e) => UpdateTabActions();

        FilterToggle.IsCheckedChanged += (_, _) =>
        {
            bool on = FilterToggle.IsChecked == true;
            FilterBox.IsVisible = on;
            if (on) FilterBox.Focus();
            else if (_vm.SelectedTab != null) _vm.SelectedTab.Filter = "";
        };
        FilterBox.KeyDown += (_, e) => { if (e.Key == Key.Escape) FilterToggle.IsChecked = false; };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F && e.KeyModifiers == mod) { OpenFilter(); e.Handled = true; }
        };
    }

    void OpenFilter()
    {
        if (_vm.SelectedTab is { HasButtons: true }) { FilterToggle.IsChecked = true; FilterBox.Focus(); FilterBox.SelectAll(); }
    }

    void OnFilterMenuClick(object? sender, RoutedEventArgs e) => OpenFilter();

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_closeConfirmed) { base.OnClosing(e); return; }

        e.Cancel = true;
        if (EditorWindow.Current is { HasUnsaved: true } editor)
        {
            editor.Activate();
            await Dialogs.DialogService.ShowMessage("Unsaved changes", "The script editor has unsaved files. Save or close them first.");
            return;
        }
        if (await _vm.ConfirmCloseAsync())
        {
            _closeConfirmed = true;
            Close();
        }
    }

    void OnTabHeaderReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right) return;
        if (sender is not Control { DataContext: TabViewModel tab } c) return;

        _vm.SelectedTab = tab;
        if (BuildTabMenu(tab) is { } menu)
        {
            menu.Open(c);
            e.Handled = true;
        }
    }

    void OnTabMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control c && _vm.SelectedTab is { } tab && BuildTabMenu(tab) is { } menu)
            menu.Open(c);
    }

    ContextMenu? BuildTabMenu(TabViewModel tab)
    {
        var menu = new ContextMenu();
        if (_vm.ShowRenameTab) menu.Items.Add(Item("Rename tab…", _vm.RenameTabCommand, tab));
        if (_vm.ShowReorderTabs && _vm.Tabs.Count > 1) menu.Items.Add(Item("Reorder tabs…", _vm.ReorderTabsCommand, null));
        if (_vm.ShowAddTab) menu.Items.Add(Item("New tab…", _vm.AddTabCommand, null));
        if (_vm.ShowRemoveTab)
        {
            if (menu.Items.Count > 0) menu.Items.Add(new Separator());
            menu.Items.Add(Item("Remove tab…", _vm.RemoveTabCommand, tab));
        }
        return menu.Items.Count == 0 ? null : menu;
    }

    static MenuItem Item(string header, System.Windows.Input.ICommand command, object? parameter) =>
        new() { Header = header, Command = command, CommandParameter = parameter };

    // ------------------------------------------------------------ macOS menu bar

    /// <summary>
    /// Shows the window's menu in the macOS menu bar. The in-window Menu stays the single source of truth (headers,
    /// commands, visibility, enabled and checked state); the native items mirror it.
    /// </summary>
    NativeMenu? _nativeRoot;

    void UseNativeMenu()
    {
        var root = new NativeMenu();
        foreach (var top in MainMenu.Items.OfType<MenuItem>())
            if (Mirror(top) is { } item) root.Items.Add(item);
        _nativeRoot = root;
        NativeMenu.SetMenu(this, root);
        MainMenu.IsVisible = false;

        // macOS occasionally drops the first export and keeps showing only the application menu. Exporting the same
        // menu again a moment later (and whenever the app is re-activated) puts it back; it is cheap and idempotent.
        foreach (var delay in new[] { 400, 1500, 4000 })
            DispatcherTimer.RunOnce(ReexportNativeMenu, TimeSpan.FromMilliseconds(delay));
    }

    void ReexportNativeMenu()
    {
        if (_nativeRoot == null) return;
        NativeMenu.SetMenu(this, null);
        NativeMenu.SetMenu(this, _nativeRoot);
    }

    static void Watch<T>(IObservable<T> source, Action<T> action) => source.Subscribe(new AnonymousObserver<T>(action));

    static string PlainHeader(object? header) => (header?.ToString() ?? "").Replace("_", "");

    static NativeMenuItem? Mirror(MenuItem source)
    {
        if (source.Header?.ToString() == "E_xit") return null; // Quit is in the application menu (Cmd+Q)

        var native = new NativeMenuItem();
        Watch(source.GetObservable(HeaderedSelectingItemsControl.HeaderProperty), h => native.Header = PlainHeader(h));
        Watch(source.GetObservable(IsVisibleProperty), v => native.IsVisible = v);
        Watch(source.GetObservable(IsEnabledProperty), v => native.IsEnabled = v);
        Watch(source.GetObservable(MenuItem.InputGestureProperty), g => native.Gesture = g);

        var children = source.Items.Cast<object?>().ToList();
        if (children.Count > 0)
        {
            native.Menu = new NativeMenu();
            foreach (var child in children)
            {
                if (child is MenuItem mi && Mirror(mi) is { } item) native.Menu.Items.Add(item);
                else if (child is Separator) native.Menu.Items.Add(new NativeMenuItemSeparator());
            }
            return native;
        }

        if (source.ToggleType != MenuItemToggleType.None)
        {
            native.ToggleType = (NativeMenuItemToggleType)(int)source.ToggleType;
            Watch(source.GetObservable(MenuItem.IsCheckedProperty), v => native.IsChecked = v);
        }
        native.Click += (_, _) =>
        {
            if (source.Command is { } cmd)
            {
                if (cmd.CanExecute(source.CommandParameter)) cmd.Execute(source.CommandParameter);
            }
            else source.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        };
        return native;
    }
}
