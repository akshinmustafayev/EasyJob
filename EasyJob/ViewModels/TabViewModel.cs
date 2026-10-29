using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyJob.Models;

namespace EasyJob.ViewModels;

public partial class TabViewModel : ObservableObject
{
    public TabViewModel(ConfigTab model)
    {
        Id = model.ID;
        _header = model.Header;
        foreach (var b in model.Buttons) Buttons.Add(new ButtonViewModel(b));
        Buttons.CollectionChanged += (_, _) => RefreshButtons();
        RebuildFiltered();
        Runs.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(HasRuns)); OnPropertyChanged(nameof(RunningCount)); OnPropertyChanged(nameof(RunningBadge)); OnPropertyChanged(nameof(HasRunning)); };
    }

    public Guid Id { get; }

    [ObservableProperty] string _header;

    public ObservableCollection<ButtonViewModel> Buttons { get; } = new();

    /// <summary>Runs started from (or sent to) this tab, newest first.</summary>
    public ObservableCollection<RunViewModel> Runs { get; } = new();

    public bool HasRuns => Runs.Count > 0;
    public bool HasButtons => Buttons.Count > 0;

    /// <summary>Text typed in the filter box; empty shows every button.</summary>
    [ObservableProperty] string _filter = "";

    /// <summary>The buttons currently shown (all of them unless a filter is active).</summary>
    public ObservableCollection<ButtonViewModel> FilteredButtons { get; } = new();

    public bool ShowFilter => Buttons.Count > 5;

    /// <summary>Compact buttons: chosen in View → Button size, or automatic once the tab has many buttons.</summary>
    public bool IsCompact => (MainViewModel.Current?.Config.button_density ?? "auto").ToLowerInvariant() switch
    {
        "compact" => true,
        "cards" => false,
        _ => Buttons.Count > 12
    };
    public void RefreshDensity() => OnPropertyChanged(nameof(IsCompact));
    public bool CanReorder => Buttons.Count > 1;
    public bool NoMatches => HasButtons && FilteredButtons.Count == 0;

    partial void OnFilterChanged(string value) => RebuildFiltered();

    void RebuildFiltered()
    {
        var f = Filter?.Trim() ?? "";
        var wanted = Buttons.Where(b => f.Length == 0
            || b.Text.Contains(f, StringComparison.OrdinalIgnoreCase)
            || b.Description.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();

        // Update in place so the list does not flicker while typing.
        if (FilteredButtons.SequenceEqual(wanted)) { OnPropertyChanged(nameof(NoMatches)); return; }
        FilteredButtons.Clear();
        foreach (var b in wanted) FilteredButtons.Add(b);
        OnPropertyChanged(nameof(NoMatches));
    }
    public int RunningCount => Runs.Count(r => r.IsRunning);
    public bool HasRunning => RunningCount > 0;
    public string RunningBadge => RunningCount.ToString();

    public void RefreshRunning()
    {
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(RunningBadge));
        OnPropertyChanged(nameof(HasRunning));
    }

    public void RefreshButtons()
    {
        OnPropertyChanged(nameof(HasButtons));
        OnPropertyChanged(nameof(ShowFilter));
        OnPropertyChanged(nameof(IsCompact));
        OnPropertyChanged(nameof(CanReorder));
        RebuildFiltered();
    }

    public ConfigTab ToModel() => new()
    {
        ID = Id,
        Header = Header,
        Buttons = Buttons.Select(b => b.Model).ToList()
    };
}
