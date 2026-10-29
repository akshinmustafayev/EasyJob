using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasyJob.Models;

namespace EasyJob.Dialogs;

/// <summary>Asks for the arguments configured on a button and returns the values in order.</summary>
public partial class ArgumentsDialog : Window
{
    readonly List<TextBox> _inputs = new();

    public ArgumentsDialog() => InitializeComponent();

    public ArgumentsDialog(ConfigButton button) : this()
    {
        Title = button.Text;
        Shell.Heading = button.Text;
        foreach (var arg in button.Arguments)
        {
            var label = new TextBlock { Text = arg.ArgumentQuestion, Margin = new Avalonia.Thickness(0, _inputs.Count == 0 ? 0 : 14, 0, 5) };
            label.Classes.Add("label");
            Fields.Children.Add(label);
            var box = new TextBox();
            _inputs.Add(box);
            Fields.Children.Add(box);
        }
        Opened += (_, _) => _inputs.FirstOrDefault()?.Focus();
    }

    void OnOk(object? s, RoutedEventArgs e) => Close(_inputs.Select(i => i.Text ?? "").ToArray());
    void OnCancel(object? s, RoutedEventArgs e) => Close(null);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close(null);
        base.OnKeyDown(e);
    }
}
