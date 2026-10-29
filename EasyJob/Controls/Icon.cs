using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace EasyJob.Controls;

/// <summary>A small vector icon that takes its colour from the surrounding text foreground (so it follows themes and hover states).</summary>
public class Icon : Avalonia.Controls.Shapes.Path
{
    static readonly Dictionary<string, (string Data, bool Filled)> Kinds = new()
    {
        ["play"] = ("M7,4.5 L20,12 L7,19.5 Z", true),
        ["stop"] = ("M6,6 H18 V18 H6 Z", true),
        ["close"] = ("M6,6 L18,18 M18,6 L6,18", false),
        ["down"] = ("M6,9 L12,15 L18,9", false),
        ["up"] = ("M6,15 L12,9 L18,15", false),
        ["right"] = ("M9,6 L15,12 L9,18", false),
        ["plus"] = ("M12,5 V19 M5,12 H19", false),
        ["refresh"] = ("M20,12 A8,8 0 1 1 16.5,5.4 M17,2.5 V6.5 H13", false),
        ["copy"] = ("M9,9 H19 V20 H9 Z M5,15 V4 H15", false),
        ["trash"] = ("M4,7 H20 M10,7 V4 H14 V7 M6,7 L7,20 H17 L18,7", false),
        ["folder"] = ("M3,6 H10 L12,8.5 H21 V19 H3 Z", false),
        ["edit"] = ("M4,20 L5,15 L16,4 L20,8 L9,19 Z", false),
        ["file"] = ("M6,3 H14 L19,8 V21 H6 Z M14,3 V8 H19", false),
        ["terminal"] = ("M3,5 H21 V19 H3 Z M7,9 L10,12 L7,15 M12,15 H16", false),
        ["list"] = ("M4,6 H20 M4,12 H20 M4,18 H14", false),
        ["expand"] = ("M4,9 V4 H9 M15,4 H20 V9 M20,15 V20 H15 M9,20 H4 V15", false),
        ["collapse"] = ("M9,4 V9 H4 M15,4 V9 H20 M20,15 H15 V20 M4,15 H9 V20", false),
        ["more"] = ("M3.5,12 A1.6,1.6 0 1 0 6.7,12 A1.6,1.6 0 1 0 3.5,12 Z M10.4,12 A1.6,1.6 0 1 0 13.6,12 A1.6,1.6 0 1 0 10.4,12 Z M17.3,12 A1.6,1.6 0 1 0 20.5,12 A1.6,1.6 0 1 0 17.3,12 Z", true),
        ["reorder"] = ("M8,5 V19 M8,5 L5,8 M8,5 L11,8 M16,19 V5 M16,19 L13,16 M16,19 L19,16", false),
        ["paste"] = ("M8,5 H6 V21 H18 V5 H16 M9,3 H15 V7 H9 Z", false),
        ["save"] = ("M5,4 H16 L20,8 V20 H5 Z M8,4 V9 H15 V4 M8,20 V14 H17 V20", false),
        ["search"] = ("M4,11 A7,7 0 1 0 18,11 A7,7 0 1 0 4,11 Z M16,16 L21,21", false),
        ["density"] = ("M4,5 H9 V10 H4 Z M15,5 H20 V10 H15 Z M4,14 H9 V19 H4 Z M15,14 H20 V19 H15 Z", false),
        ["lock"] = ("M6,11 H18 V20 H6 Z M8,11 V8 A4,4 0 0 1 16,8 V11", false),
        ["help"] = ("M9,9 A3,3 0 1 1 13,11.8 C12.2,12.4 12,13 12,14 M12,17.5 V18", false),
    };

    public static readonly StyledProperty<string?> KindProperty =
        AvaloniaProperty.Register<Icon, string?>(nameof(Kind));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<Icon>();

    static Icon()
    {
        KindProperty.Changed.AddClassHandler<Icon>((i, _) => i.Apply());
    }

    public Icon()
    {
        Width = 14;
        Height = 14;
        Stretch = Stretch.Uniform;
        StrokeThickness = 1.8;
        StrokeLineCap = PenLineCap.Round;
        StrokeJoin = PenLineJoin.Round;
        IsHitTestVisible = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.Name == nameof(Foreground)) Apply();
    }

    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public string? Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    void Apply()
    {
        if (Kind == null || !Kinds.TryGetValue(Kind, out var k)) return;
        Data = Geometry.Parse(k.Data);
        var brush = Foreground;
        if (k.Filled) { Fill = brush; Stroke = null; }
        else { Stroke = brush; Fill = null; }
    }
}
