using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EasyJob.Controls;
using EasyJob.ViewModels;

namespace EasyJob.Views;

public partial class TabView : UserControl
{
    public TabView()
    {
        InitializeComponent();
        // Drag a script card onto another card to reorder.
        _ = new DragReorder<ButtonViewModel>(this, "EasyJob.Button",
            src => DragReorder<ButtonViewModel>.Ancestor(src, c => c.Classes.Contains("scriptcard")) is { DataContext: ButtonViewModel b } card ? (card, b) : null,
            (source, target) => Main?.MoveButton(source, target),
            () => Main?.ShowReorderButtons == true);

        WireGrip();
        ButtonScroll.Resources["CardWidth"] = 270.0;
        ButtonScroll.SizeChanged += (_, e) => { if (e.WidthChanged) FitCards(); };

        AttachedToVisualTree += (_, _) =>
        {
            if (Main is { } m) m.PropertyChanged += OnStateChanged;
            if (Tab is { } t) t.PropertyChanged += OnStateChanged;
            UpdateToolbar();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (Main is { } m) m.PropertyChanged -= OnStateChanged;
            if (Tab is { } t) t.PropertyChanged -= OnStateChanged;
        };
    }

    void OnStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => UpdateToolbar();

    const double DefaultPanelHeight = 260;
    static double _panelHeight = DefaultPanelHeight; // shared by all tabs, for this session
    double _dragStartHeight;
    Avalonia.Point _dragStart;

    void UpdateToolbar()
    {
        ButtonScroll.Classes.Set("compact", Tab?.IsCompact == true);
        ButtonScroll.MaxHeight = _panelHeight;
        FitCards();
    }

    /// <summary>Stretches the cards so every row is filled edge to edge, with as many columns as fit.</summary>
    void FitCards()
    {
        bool compact = Tab?.IsCompact == true;
        double min = compact ? 215 : 270, gap = compact ? 8 : 10;
        double avail = ButtonScroll.Bounds.Width - 16; // room for the scrollbar
        if (avail <= 0) { ButtonScroll.Resources["CardWidth"] = min; return; }
        int columns = Math.Max(1, (int)(avail / (min + gap)));
        ButtonScroll.Resources["CardWidth"] = Math.Floor(avail / columns - gap);
    }

    void WireGrip()
    {
        PanelGrip.PointerPressed += (_, e) =>
        {
            _dragStart = e.GetPosition(this);
            _dragStartHeight = ButtonScroll.Bounds.Height;
            e.Pointer.Capture(PanelGrip);
        };
        PanelGrip.PointerMoved += (_, e) =>
        {
            if (e.Pointer.Captured != PanelGrip) return;
            var max = Math.Max(120, Bounds.Height - 160); // always leave room for the output
            _panelHeight = Math.Clamp(_dragStartHeight + e.GetPosition(this).Y - _dragStart.Y, 70, max);
            ButtonScroll.MaxHeight = _panelHeight;
        };
        PanelGrip.PointerReleased += (_, e) => { if (e.Pointer.Captured == PanelGrip) e.Pointer.Capture(null); };
        PanelGrip.DoubleTapped += (_, _) => { _panelHeight = DefaultPanelHeight; ButtonScroll.MaxHeight = _panelHeight; };
    }

    MainViewModel? Main => this.FindAncestorOfType<Window>()?.DataContext as MainViewModel;
    TabViewModel? Tab => DataContext as TabViewModel;

    void OnRunClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ButtonViewModel button } || Main is not { } main) return;

        var mods = MainWindow.LastModifiers;
        if ((mods & (KeyModifiers.Control | KeyModifiers.Meta)) != 0) main.RevealScriptCommand.Execute(button);
        else if ((mods & KeyModifiers.Shift) != 0) main.EditScriptCommand.Execute(button);
        else _ = main.RunButtonAsync(button, Tab);
    }

    void OnButtonReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right) return;
        if (sender is not Button { DataContext: ButtonViewModel button } source) return;
        if (BuildButtonMenu(button) is { } menu)
        {
            menu.Open(source);
            e.Handled = true;
        }
    }

    void OnMoreClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ButtonViewModel button } source && BuildButtonMenu(button) is { } menu)
            menu.Open(source);
    }

    ContextMenu? BuildButtonMenu(ButtonViewModel button)
    {
        if (Main is not { } main || Tab is not { } tab) return null;

        var menu = new ContextMenu();
        menu.Items.Add(Item("Run", () => _ = main.RunButtonAsync(button, tab)));

        var others = main.Tabs.Where(t => t != tab).ToList();
        if (others.Count > 0)
        {
            var sub = new MenuItem { Header = "Run in tab" };
            foreach (var t in others)
            {
                var target = t;
                sub.Items.Add(Item(target.Header, () => _ = main.RunButtonAsync(button, target)));
            }
            menu.Items.Add(sub);
        }

        if (main.CanUseEditor || main.CanOpenExternally || main.CanShowInFolder) menu.Items.Add(new Separator());
        if (main.CanUseEditor) menu.Items.Add(Item("Edit in script editor", () => main.EditScriptCommand.Execute(button)));
        if (main.CanOpenExternally) menu.Items.Add(Item("Open with default app", () => main.OpenScriptCommand.Execute(button)));
        if (main.CanShowInFolder) menu.Items.Add(Item("Show in folder", () => main.RevealScriptCommand.Execute(button)));

        if (main.CanEditButton || main.CanCopyButton || main.CanRemoveButton) menu.Items.Add(new Separator());
        if (main.CanEditButton) menu.Items.Add(Item("Edit…", () => main.EditButtonCommand.Execute(button)));
        if (main.CanCopyButton) menu.Items.Add(Item("Copy", () => main.CopyButtonCommand.Execute(button)));
        if (main.CanRemoveButton) menu.Items.Add(Item("Remove…", () => main.RemoveButtonCommand.Execute(button)));
        return menu;
    }

    void OnPanelReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right || e.Handled) return;
        if (sender is not Control c || Main is not { } main) return;

        var menu = new ContextMenu();
        if (main.CanAddButton) menu.Items.Add(Item("Add button…", () => main.AddButtonCommand.Execute(null)));
        if (main.CanReorderButtons) menu.Items.Add(Item("Reorder buttons…", () => main.ReorderButtonsCommand.Execute(null)));
        if (!main.Config.restrictions.block_buttons_paste)
        {
            var paste = Item("Paste button", () => main.PasteButtonCommand.Execute(null));
            paste.IsEnabled = main.CanPasteButton;
            menu.Items.Add(paste);
        }
        if (menu.Items.Count == 0) return;
        menu.Open(c);
    }

    static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    void OnStopAll(object? s, RoutedEventArgs e) => Main?.StopAllCommand.Execute(Tab);
    void OnCollapseAll(object? s, RoutedEventArgs e) => Main?.CollapseAllCommand.Execute(Tab);
    void OnExpandAll(object? s, RoutedEventArgs e) => Main?.ExpandAllCommand.Execute(Tab);
    void OnClearFinished(object? s, RoutedEventArgs e) => Main?.ClearFinishedCommand.Execute(Tab);
}
