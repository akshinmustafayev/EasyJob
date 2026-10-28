using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace EasyJob.Dialogs;

public partial class ReorderDialog : Window
{
    sealed partial class Entry : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public Entry(object source, string label) { Source = source; Label = label; }
        public object Source { get; }
        public string Label { get; }

        int _number;
        public int Number { get => _number; set => SetProperty(ref _number, value); }
    }

    readonly ObservableCollection<Entry> _items = new();

    public ReorderDialog() => InitializeComponent();

    public ReorderDialog(string title, List<object> items, Func<object, string> label) : this()
    {
        Title = title;
        Shell.Heading = title;
        foreach (var i in items) _items.Add(new Entry(i, label(i)));
        Renumber();
        List.ItemsSource = _items;
        if (_items.Count > 0) List.SelectedIndex = 0;
        Opened += (_, _) => List.Focus();

        _ = new Controls.DragReorder<Entry>(List, "EasyJob.Row",
            src => Controls.DragReorder<Entry>.Ancestor(src, c => c is ListBoxItem) is ListBoxItem { DataContext: Entry en } item ? (item, en) : null,
            (source, target) =>
            {
                int from = _items.IndexOf(source), to = _items.IndexOf(target);
                if (from < 0 || to < 0) return;
                _items.Move(from, to);
                Renumber();
                List.SelectedItem = source;
            },
            () => true);
    }

    void Renumber()
    {
        for (int i = 0; i < _items.Count; i++) _items[i].Number = i + 1;
    }

    void Move(int delta)
    {
        int i = List.SelectedIndex, j = i + delta;
        if (i < 0 || j < 0 || j >= _items.Count) return;
        _items.Move(i, j);
        Renumber();
        List.SelectedIndex = j;
        List.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.Key is Key.Up or Key.Down)
        {
            Move(e.Key == Key.Up ? -1 : 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape) Close(null);
        base.OnKeyDown(e);
    }

    void OnUp(object? s, RoutedEventArgs e) => Move(-1);
    void OnDown(object? s, RoutedEventArgs e) => Move(1);
    void OnOk(object? s, RoutedEventArgs e) => Close(_items.Select(i => i.Source).ToList());
    void OnCancel(object? s, RoutedEventArgs e) => Close(null);
}
