using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EasyJob.Services;

namespace EasyJob.Dialogs;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        TitleText.Text = "EasyJob " + (version == null ? "" : $"{version.Major}.{version.Minor}.{version.Build}");
        Shell.Heading = null;
        License.Text = HelpService.ReadEmbedded("LICENSE.txt");
    }

    void OnGitHub(object? s, RoutedEventArgs e) => Services.Launcher.Open("https://github.com/akshinmustafayev/EasyJob");
    void OnReleases(object? s, RoutedEventArgs e) => Services.Launcher.Open("https://github.com/akshinmustafayev/EasyJob/releases");
    void OnClose(object? s, RoutedEventArgs e) => Close(true);
}
