using Avalonia.Controls;
using Avalonia.Interactivity;
using EasyJob.Services;

namespace EasyJob.Dialogs;

/// <summary>Shows a new release and downloads it. Closes with the staged update, or null if the user backed out.</summary>
public partial class UpdateDialog : Window
{
    readonly UpdateInfo _update;
    readonly string? _blocked;
    CancellationTokenSource? _cts;
    bool _busy;

    public UpdateDialog() : this(new UpdateInfo(new Version(0, 0), "", "", UpdateService.ReleasesPage, null, null, 0, null)) { }

    public UpdateDialog(UpdateInfo update)
    {
        InitializeComponent();
        _update = update;
        Shell.Heading = $"EasyJob {update.VersionText} is available";
        Shell.Subheading = $"You have {UpdateService.Format(UpdateService.CurrentVersion)}.";
        Notes.Text = update.Notes.Length == 0 ? "No release notes were published." : update.Notes;

        _blocked = update.AssetUrl == null
            ? "This release has no download for your system. Open the release page to get it."
            : UpdateService.CannotSelfUpdateReason();
        if (_blocked != null)
        {
            Status.Text = _blocked;
            InstallButton.IsEnabled = false;
        }
        else
        {
            Status.Text = $"{update.AssetName} · {update.AssetSize / 1048576.0:0.#} MB. Your config.json is kept. EasyJob restarts when it is done.";
        }

        Closing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    async void OnInstall(object? s, RoutedEventArgs e)
    {
        _busy = true;
        InstallButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        PageButton.IsEnabled = false;
        Progress.IsVisible = true;
        Status.Text = "Downloading…";
        _cts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<double>(p =>
            {
                Progress.Value = p;
                Status.Text = $"Downloading… {p:P0}";
            });
            var staged = await UpdateService.DownloadAsync(_update, progress, _cts.Token);
            _busy = false;
            Close(staged);
        }
        catch (Exception ex)
        {
            _busy = false;
            Progress.IsVisible = false;
            Status.Text = "Update failed: " + ex.Message;
            InstallButton.IsEnabled = true;
            InstallButton.Content = "Try again";
            LaterButton.IsEnabled = true;
            PageButton.IsEnabled = true;
        }
    }

    void OnPage(object? s, RoutedEventArgs e) => Services.Launcher.Open(_update.PageUrl);
    void OnLater(object? s, RoutedEventArgs e) => Close(null);
}
