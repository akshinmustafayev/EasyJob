using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using EasyJob.Services;

namespace EasyJob.Controls;

/// <summary>A small "?" badge. Clicking it shows the help text for <see cref="Topic"/> in a popup next to it.</summary>
public class HelpButton : Button
{
    public static readonly StyledProperty<string?> TopicProperty =
        AvaloniaProperty.Register<HelpButton, string?>(nameof(Topic));

    protected override Type StyleKeyOverride => typeof(Button);

    public HelpButton()
    {
        Classes.Add("help");
        Content = new Border
        {
            Classes = { "helpdot" },
            Child = new TextBlock { Text = "?", Classes = { "helpmark" } }
        };
    }

    public string? Topic { get => GetValue(TopicProperty); set => SetValue(TopicProperty, value); }

    protected override void OnClick()
    {
        base.OnClick();
        if (Topic == null || HelpService.Find(Topic) is not { } item) return;

        var text = new StackPanel { Spacing = 6, MaxWidth = 340 };
        text.Children.Add(new TextBlock { Text = item.Heading, FontWeight = Avalonia.Media.FontWeight.SemiBold });
        text.Children.Add(new TextBlock { Text = item.Description, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        new Flyout
        {
            Content = text,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Transient
        }.ShowAt(this);
    }
}
