using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EasyJob.Views;

namespace EasyJob;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = new MainWindow();
        }

        EasyJob.Services.MacDockIcon.Apply();
        base.OnFrameworkInitializationCompleted();
    }

    public static void Shutdown() =>
        (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

    /// <summary>The window the user is working in (dialogs open over it); falls back to the main window.</summary>
    public static Avalonia.Controls.Window? ActiveWindow =>
        (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.FirstOrDefault(w => w.IsActive && w.IsVisible) ?? MainWindow;

    public static Avalonia.Controls.Window? MainWindow =>
        (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
