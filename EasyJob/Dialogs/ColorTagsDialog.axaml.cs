using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EasyJob.Services;

namespace EasyJob.Dialogs;

public partial class ColorTagsDialog : Window
{
    public ColorTagsDialog()
    {
        InitializeComponent();
        for (int i = 0; i < OutputParser.TagColors.Length; i++)
        {
            var color = OutputParser.TagColors[i];
            var tag = $"c{i + 1:00}EJ";
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,90,*") };
            row.Children.Add(new Border { Width = 18, Height = 18, CornerRadius = new Avalonia.CornerRadius(9), Background = new SolidColorBrush(color), Margin = new Avalonia.Thickness(0, 0, 12, 0) });
            var hex = new TextBlock { Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = Brushes.Gray };
            Grid.SetColumn(hex, 1);
            row.Children.Add(hex);
            var sample = new SelectableTextBlock { Text = $"\\{tag}Some text/{tag}", Foreground = new SolidColorBrush(color), FontFamily = new FontFamily("Menlo, Cascadia Mono, Consolas, monospace") };
            Grid.SetColumn(sample, 2);
            row.Children.Add(sample);
            Rows.Children.Add(row);
        }
    }

    void OnClose(object? s, RoutedEventArgs e) => Close(true);
}
