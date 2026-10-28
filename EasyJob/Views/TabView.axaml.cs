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

    /// <summary>Shows the toolbar actions the config allows, and only when they make sense.</summary>
    void UpdateToolbar()
    {
        if (Main is not { } m) return;
        AddScriptButton.IsVisible = m.ShowAddButton;
        ReorderButton.IsVisible = m.ShowReorderButtons && Tab?.CanReorder == true;
        PasteButton.IsVisible = m.CanPasteButton;
    }

    void OnPasteScript(object? sender, RoutedEventArgs e) => Main?.PasteButtonCommand.Execute(null);
    void OnReorderScripts(object? sender, RoutedEventArgs e) => Main?.ReorderButtonsCommand.Execute(null);

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

    void OnAddScript(object? sender, RoutedEventArgs e) => Main?.AddButtonCommand.Execute(null);

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

        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Edit in script editor", () => main.EditScriptCommand.Execute(button)));
        menu.Items.Add(Item("Open with default app", () => main.OpenScriptCommand.Execute(button)));
        menu.Items.Add(Item("Show in folder", () => main.RevealScriptCommand.Execute(button)));

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
