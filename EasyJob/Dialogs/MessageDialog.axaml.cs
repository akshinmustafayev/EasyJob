using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace EasyJob.Dialogs;

/// <summary>An info box (okText == null) or an OK/Cancel confirmation.</summary>
public partial class MessageDialog : Window
{
    public MessageDialog() => InitializeComponent();

    /// <summary>True when the user picked the optional third button.</summary>
    public bool Discarded { get; private set; }

    public MessageDialog(string title, string message, string? okText, string? altText = null) : this()
    {
        if (altText != null) { AltButton.Content = altText; AltButton.IsVisible = true; }
        Title = title;
        Shell.Heading = title;
        MessageText.Text = message;
        CancelButton.IsVisible = okText != null;
        OkButton.Content = okText ?? "OK";
        if (okText == null) OkButton.Classes.Remove("accent");
    }

    void OnOk(object? s, RoutedEventArgs e) => Close(true);
    void OnAlt(object? s, RoutedEventArgs e) { Discarded = true; Close(false); }
    void OnCancel(object? s, RoutedEventArgs e) => Close(false);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close(false);
        base.OnKeyDown(e);
    }
}
