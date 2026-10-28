using System.Collections.Concurrent;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyJob.Models;
using EasyJob.Services;

namespace EasyJob.ViewModels;

public enum RunStatus { Starting, Running, Succeeded, Failed, Stopped }

/// <summary>One execution of one script. Owns its process and its own output buffer, so runs never mix.</summary>
public partial class RunViewModel : ObservableObject
{
    const int MaxLines = 50_000;
    const int TrimLines = 10_000;

    readonly MainViewModel _host;
    readonly ConfigButton _button;
    readonly IReadOnlyList<string> _answers;
    readonly ConcurrentQueue<OutputLine> _pending = new();
    int _flushScheduled;
    Process? _process;
    bool _stopRequested;

    public RunViewModel(MainViewModel host, TabViewModel tab, ConfigButton button, IReadOnlyList<string> answers)
    {
        _host = host;
        Tab = tab;
        _button = button;
        _answers = answers;
        Title = string.IsNullOrWhiteSpace(button.Text) ? Path.GetFileName(button.Script) : button.Text;
        ScriptName = Path.GetFileName(ScriptCommand.NormalizeSeparators(button.Script));
        ScriptType = button.ScriptType;
        Started = DateTime.Now;
    }

    public TabViewModel Tab { get; }
    public string Title { get; }
    public string ScriptName { get; }
    public string ScriptType { get; }
    public DateTime Started { get; }
    public ConfigButton Button => _button;
    public IReadOnlyList<string> Answers => _answers;

    public List<OutputLine> Lines { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsRunning), nameof(IsFinished), nameof(StatusText), nameof(CanStop), nameof(IsOk), nameof(IsFailed), nameof(IsStopped))]
    RunStatus _status = RunStatus.Starting;

    [ObservableProperty] bool _isExpanded = true;
    [ObservableProperty] bool _isTall;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasProcessId))] int _processId;
    [ObservableProperty] int? _exitCode;
    [ObservableProperty] string _durationText = "0s";
    [ObservableProperty] string _commandLine = "";

    public bool IsRunning => Status is RunStatus.Starting or RunStatus.Running;
    public bool IsFinished => !IsRunning;
    public bool HasProcessId => ProcessId != 0;
    public bool CanStop => IsRunning;
    public bool IsOk => Status == RunStatus.Succeeded;
    public bool IsFailed => Status == RunStatus.Failed;
    public bool IsStopped => Status == RunStatus.Stopped;
    public string StartedText => Started.ToString("HH:mm:ss");

    public string StatusText => Status switch
    {
        RunStatus.Starting => "Starting",
        RunStatus.Running => "Running",
        RunStatus.Succeeded => "Completed",
        RunStatus.Failed => ExitCode is { } c ? $"Failed ({c})" : "Failed",
        RunStatus.Stopped => "Stopped",
        _ => ""
    };

    /// <summary>Raised on the UI thread with each batch of new lines.</summary>
    public event Action<IReadOnlyList<OutputLine>>? LinesAdded;
    /// <summary>Raised on the UI thread with the number of lines dropped from the start of the buffer.</summary>
    public event Action<int>? LinesTrimmed;
    public event Action? Cleared;

    DateTime _finished;

    public void Tick()
    {
        var end = IsRunning ? DateTime.Now : _finished;
        DurationText = FormatDuration(end - Started);
    }

    static string FormatDuration(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s"
        : t.TotalMinutes >= 1 ? $"{t.Minutes}m {t.Seconds}s"
        : t.TotalSeconds >= 10 ? $"{(int)t.TotalSeconds}s"
        : $"{t.TotalSeconds:0.0}s";

    public async Task StartAsync()
    {
        var parserOut = new OutputParser(!_host.Config.console_ignore_color_tags);
        var parserErr = new OutputParser(!_host.Config.console_ignore_color_tags);

        try
        {
            var psi = ScriptCommand.Build(_host.Config, _button, _answers, out var display);
            CommandLine = display;
            Enqueue(OutputParser.Meta("$ " + display));

            _process = Process.Start(psi) ?? throw new InvalidOperationException("The process could not be started.");
            try { _process.StandardInput.Close(); } catch { }
            ProcessId = _process.Id;
            Status = RunStatus.Running;
            _host.Log($"Started '{Title}' (pid {ProcessId}) in tab '{Tab.Header}'");

            var stdout = PumpAsync(_process.StandardOutput, parserOut, LineKind.Output);
            var stderr = PumpAsync(_process.StandardError, parserErr, LineKind.Error);
            await _process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);

            ExitCode = _process.ExitCode;
            Status = _stopRequested ? RunStatus.Stopped : ExitCode == 0 ? RunStatus.Succeeded : RunStatus.Failed;
        }
        catch (Exception ex)
        {
            Enqueue(new OutputLine(new[] { new Segment(ex.Message, null, false, false) }, LineKind.Error));
            Status = _stopRequested ? RunStatus.Stopped : RunStatus.Failed;
        }
        finally
        {
            _finished = DateTime.Now;
            Tick();
            Enqueue(OutputParser.Meta(Status switch
            {
                RunStatus.Succeeded => $"✓ Completed in {DurationText}",
                RunStatus.Stopped => $"■ Stopped after {DurationText}",
                _ => ExitCode is { } c ? $"✗ Failed with exit code {c} after {DurationText}" : $"✗ Failed after {DurationText}"
            }));
            _host.Log($"'{Title}' {StatusText.ToLowerInvariant()} in {DurationText}");
            _process?.Dispose();
            _process = null;
            _host.NotifyRunFinished(this);
        }
    }

    async Task PumpAsync(StreamReader reader, OutputParser parser, LineKind kind)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
                Enqueue(parser.Parse(line, kind));
        }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }

    void Enqueue(OutputLine line)
    {
        _pending.Enqueue(line);
        // Coalesce bursts: at most one UI flush every ~40ms no matter how chatty the script is.
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
            Dispatcher.UIThread.Post(() => DispatcherTimer.RunOnce(Flush, TimeSpan.FromMilliseconds(40)));
    }

    void Flush()
    {
        Volatile.Write(ref _flushScheduled, 0);
        var batch = new List<OutputLine>();
        while (_pending.TryDequeue(out var l)) batch.Add(l);
        if (batch.Count == 0) return;

        Lines.AddRange(batch);
        if (Lines.Count > MaxLines)
        {
            Lines.RemoveRange(0, TrimLines);
            LinesTrimmed?.Invoke(TrimLines);
        }
        LinesAdded?.Invoke(batch);
    }

    [RelayCommand]
    public void Stop()
    {
        if (!IsRunning) return;
        _stopRequested = true;
        try { _process?.Kill(entireProcessTree: true); }
        catch (Exception ex) { _host.Log($"Could not stop '{Title}': {ex.Message}"); }
    }

    [RelayCommand] void Rerun() => _host.RerunCommand.Execute(this);
    [RelayCommand] void Dismiss() => _host.DismissRunCommand.Execute(this);
    [RelayCommand] void ToggleExpanded() => IsExpanded = !IsExpanded;
    [RelayCommand] void ToggleTall() => IsTall = !IsTall;

    public void ClearOutput()
    {
        Lines.Clear();
        Cleared?.Invoke();
    }

    public string GetAllText() => string.Join(Environment.NewLine, Lines.Select(l => l.PlainText));
}
