using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace EasyJob.Dialogs;

public partial class PromptDialog : Window
{
    public PromptDialog() => InitializeComponent();

    public PromptDialog(string title, string label, string initial, string okText) : this()
    {
        Title = title;
        Shell.Heading = title;
        LabelText.Text = label;
        Input.Text = initial;
        OkButton.Content = okText;
        Opened += (_, _) => { Input.Focus(); Input.SelectAll(); };
    }

    void OnOk(object? s, RoutedEventArgs e) => Close(Input.Text);
    void OnCancel(object? s, RoutedEventArgs e) => Close(null);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close(null);
        base.OnKeyDown(e);
    }
}
