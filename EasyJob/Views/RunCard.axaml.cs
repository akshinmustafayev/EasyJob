using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasyJob.ViewModels;
using Avalonia.VisualTree;

namespace EasyJob.Views;

public partial class RunCard : UserControl
{
    public RunCard()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ApplyConsoleColors();
        AttachedToVisualTree += (_, _) => ApplyConsoleColors();
    }

    void ApplyConsoleColors()
    {
        if (MainViewModel.Current is { } main)
        {
            Output.ConsoleBackground = main.ConsoleBackground;
            Output.ConsoleForeground = main.ConsoleForeground;
        }
    }

    void OnHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        // Clicks on the header's buttons are handled by the buttons themselves.
        if (e.Source is Visual v && v.FindAncestorOfType<Button>(includeSelf: true) != null) return;
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && DataContext is RunViewModel run)
            run.ToggleExpandedCommand.Execute(null);
    }

    async void OnCopyOutput(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RunViewModel run && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(run.GetAllText());
    }
}
