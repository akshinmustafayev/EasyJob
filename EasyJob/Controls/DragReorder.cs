using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

#pragma warning disable CS0618 // DataObject-based drag API: still supported in Avalonia 11.3; DataTransfer has no in-process object payloads.

namespace EasyJob.Controls;

/// <summary>
/// Adds drag-to-reorder to any container. The container supplies <c>find</c>, which maps the element under the pointer
/// to a (host control, item) pair; dropping one item on another calls <c>onDrop(dragged, target)</c>.
/// The host being dragged over gets the "droptarget" style class so it can be highlighted.
/// </summary>
public sealed class DragReorder<T> where T : class
{
    const double Threshold = 8;

    readonly Control _root;
    readonly string _format;
    readonly Func<object?, (Control Host, T Item)?> _find;
    readonly Action<T, T> _onDrop;
    readonly Func<bool> _enabled;

    T? _pressed;
    Point _pressPoint;
    bool _dragging;
    Control? _highlight;

    public DragReorder(Control root, string format, Func<object?, (Control Host, T Item)?> find, Action<T, T> onDrop, Func<bool> enabled)
    {
        _root = root;
        _format = format;
        _find = find;
        _onDrop = onDrop;
        _enabled = enabled;

        DragDrop.SetAllowDrop(root, true);
        // Tunnel so we see the press even when a button inside the item handles it.
        root.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        root.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        root.AddHandler(InputElement.PointerReleasedEvent, (_, _) => _pressed = null, RoutingStrategies.Tunnel);
        root.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        root.AddHandler(DragDrop.DragLeaveEvent, (_, _) => Highlight(null));
        root.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    /// <summary>Finds the nearest control (starting at <paramref name="source"/>) that satisfies <paramref name="match"/>.</summary>
    public static Control? Ancestor(object? source, Func<Control, bool> match) =>
        (source as Visual)?.GetSelfAndVisualAncestors().OfType<Control>().FirstOrDefault(match);

    void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        _pressed = null;
        if (!_enabled() || !e.GetCurrentPoint(_root).Properties.IsLeftButtonPressed) return;
        if (_find(e.Source) is { } hit)
        {
            _pressed = hit.Item;
            _pressPoint = e.GetPosition(_root);
        }
    }

    async void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pressed == null || _dragging || !e.GetCurrentPoint(_root).Properties.IsLeftButtonPressed) return;
        var delta = e.GetPosition(_root) - _pressPoint;
        if (Math.Abs(delta.X) < Threshold && Math.Abs(delta.Y) < Threshold) return;

        _dragging = true;
        var data = new DataObject();
        data.Set(_format, _pressed);
        try { await DragDrop.DoDragDrop(e, data, DragDropEffects.Move); }
        finally
        {
            _dragging = false;
            _pressed = null;
            Highlight(null);
        }
    }

    void OnDragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(_format)) return;
        var target = _find(e.Source);
        e.DragEffects = target != null ? DragDropEffects.Move : DragDropEffects.None;
        Highlight(target?.Host);
        e.Handled = true;
    }

    void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(_format)) return;
        Highlight(null);
        if (e.Data.Get(_format) is T source && _find(e.Source) is { } target && !ReferenceEquals(source, target.Item))
            _onDrop(source, target.Item);
        e.Handled = true;
    }

    void Highlight(Control? host)
    {
        if (ReferenceEquals(_highlight, host)) return;
        _highlight?.Classes.Remove("droptarget");
        _highlight = host;
        _highlight?.Classes.Add("droptarget");
    }
}
