using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyJob.Controls;
using EasyJob.Dialogs;
using EasyJob.Services;
using EasyJob.ViewModels;

namespace EasyJob.Views;

/// <summary>Browse every cmdlet, function and alias PowerShell knows about, with syntax, description and examples. Hosted in the script editor.</summary>
public partial class CommandsPanel : UserControl
{
    // The command list takes a few seconds to build, so keep it for the whole session.
    static List<PsCommand>? _cache;

    List<PsCommand> _all = new();
    string? _pwsh;
    CancellationTokenSource? _helpCts;
    PsCommandHelp? _help;
    bool _loaded;

    /// <summary>Raised when the user wants a command name placed in the editor.</summary>
    public event Action<string>? InsertRequested;

    public CommandsPanel()
    {
        InitializeComponent();
        SearchBox.TextChanged += (_, _) => ApplyFilter();
        TypeBox.SelectionChanged += (_, _) => ApplyFilter();
        CommandList.SelectionChanged += (_, _) => _ = ShowSelectedAsync();
        DetachedFromVisualTree += (_, _) => _helpCts?.Cancel();
    }

    /// <summary>Loads the command list the first time the panel is shown.</summary>
    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _ = LoadAsync(forceReload: false);
    }

    async Task LoadAsync(bool forceReload)
    {
        _pwsh = ShellResolver.ResolvePowerShell(MainViewModel.Current?.Config ?? new Models.AppConfig());
        if (_pwsh == null)
        {
            ShowMessage("PowerShell was not found",
                "Install PowerShell 7 (pwsh) or set its path in Tools → Configuration, then press refresh.");
            CountText.Text = "";
            return;
        }

        if (!forceReload && _cache != null)
        {
            _all = _cache;
            ApplyFilter();
            ShowMessage("Select a command", "Pick a command on the left to see its syntax, description and examples.");
            return;
        }

        CommandList.ItemsSource = null;
        CountText.Text = "Loading commands from PowerShell…";
        ShowMessage("Loading commands…", "Asking PowerShell for its cmdlets, functions and aliases. This takes a few seconds the first time.");
        try
        {
            _cache = _all = await PowerShellCommands.ListAsync(_pwsh);
            ApplyFilter();
            ShowMessage("Select a command", "Pick a command on the left to see its syntax, description and examples.");
        }
        catch (Exception ex)
        {
            CountText.Text = "";
            ShowMessage("Could not load commands", ex.Message);
        }
    }

    void ApplyFilter()
    {
        if (_all.Count == 0) return;
        var q = SearchBox.Text?.Trim() ?? "";
        var type = (TypeBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "";

        IEnumerable<PsCommand> items = _all;
        if (type.Length > 0) items = items.Where(c => c.Type == type);
        if (q.Length > 0)
        {
            // Prefix matches first, then the rest.
            items = items.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Module.Contains(q, StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(c => c.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                         .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }
        var list = items.ToList();
        CommandList.ItemsSource = list;
        CountText.Text = list.Count == _all.Count ? $"{list.Count:N0} commands" : $"{list.Count:N0} of {_all.Count:N0} commands";
    }

    async Task ShowSelectedAsync()
    {
        _helpCts?.Cancel();
        if (CommandList.SelectedItem is not PsCommand cmd || _pwsh == null)
        {
            SetButtons(false);
            return;
        }

        Heading.Text = cmd.Name;
        Meta.Text = string.IsNullOrEmpty(cmd.Module) ? cmd.Type : $"{cmd.Type} · {cmd.Module}";
        Message.IsVisible = false;
        Details.IsVisible = false;
        SetButtons(true);
        _help = null;

        var cts = _helpCts = new CancellationTokenSource();
        ShowMessage(cmd.Name, "Loading help…", keepHeading: true);
        try
        {
            var help = await PowerShellCommands.HelpAsync(_pwsh, cmd.Name, cts.Token);
            if (cts.IsCancellationRequested) return;
            _help = help;
            Render(help);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!cts.IsCancellationRequested) ShowMessage(cmd.Name, ex.Message, keepHeading: true);
        }
    }

    void Render(PsCommandHelp help)
    {
        Message.IsVisible = false;
        Details.IsVisible = true;
        Synopsis.Text = help.Synopsis;
        Synopsis.IsVisible = help.Synopsis.Length > 0 && !help.Synopsis.StartsWith((CommandList.SelectedItem as PsCommand)?.Name ?? "\0");
        Syntax.Text = help.Syntax.Length > 0 ? help.Syntax : "No syntax information available.";
        Description.Text = help.Description;
        DescriptionBlock.IsVisible = help.Description.Length > 0;

        Examples.Children.Clear();
        foreach (var ex in help.Examples)
        {
            var text = new SelectableTextBlock { Text = ex, TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontFamily = new Avalonia.Media.FontFamily("Menlo, Cascadia Mono, Consolas, monospace"), FontSize = 12.5 };
            var card = new Border { Padding = new Avalonia.Thickness(14, 10), Child = text };
            card.Classes.Add("card");
            Examples.Children.Add(card);
        }
        ExamplesBlock.IsVisible = help.Examples.Count > 0;
    }

    void ShowMessage(string title, string text, bool keepHeading = false)
    {
        if (!keepHeading) { Heading.Text = ""; Meta.Text = ""; }
        Details.IsVisible = false;
        Message.IsVisible = true;
        MessageTitle.Text = keepHeading ? "" : title;
        MessageText.Text = text;
        if (!keepHeading) SetButtons(false);
    }

    void SetButtons(bool selected)
    {
        CopyNameButton.IsEnabled = selected;
        CopySyntaxButton.IsEnabled = selected;
        InsertButton.IsEnabled = selected;
    }

    async void OnCopyNameClick(object? s, RoutedEventArgs e)
    {
        if (CommandList.SelectedItem is PsCommand c && TopLevel.GetTopLevel(this)?.Clipboard is { } cb) await cb.SetTextAsync(c.Name);
    }

    async void OnCopySyntaxClick(object? s, RoutedEventArgs e)
    {
        if (_help?.Syntax is { Length: > 0 } syntax && TopLevel.GetTopLevel(this)?.Clipboard is { } cb) await cb.SetTextAsync(syntax);
    }

    void OnInsertClick(object? s, RoutedEventArgs e)
    {
        if (CommandList.SelectedItem is PsCommand c) InsertRequested?.Invoke(c.Name);
    }

    void OnItemDoubleTapped(object? s, TappedEventArgs e) => OnInsertClick(s, e);
    void OnRefreshClick(object? s, RoutedEventArgs e) => _ = LoadAsync(forceReload: true);
}
