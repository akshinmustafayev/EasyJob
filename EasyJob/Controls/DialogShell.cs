using Avalonia;
using Avalonia.Controls;

namespace EasyJob.Controls;

/// <summary>Common dialog layout: heading + optional subheading on top, content in the middle, a footer bar with the actions at the bottom.</summary>
public class DialogShell : ContentControl
{
    public static readonly StyledProperty<string?> HeadingProperty =
        AvaloniaProperty.Register<DialogShell, string?>(nameof(Heading));

    public static readonly StyledProperty<string?> SubheadingProperty =
        AvaloniaProperty.Register<DialogShell, string?>(nameof(Subheading));

    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<DialogShell, object?>(nameof(Footer));

    public string? Heading { get => GetValue(HeadingProperty); set => SetValue(HeadingProperty, value); }
    public string? Subheading { get => GetValue(SubheadingProperty); set => SetValue(SubheadingProperty, value); }
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
}
