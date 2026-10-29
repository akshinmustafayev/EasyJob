using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Avalonia.Threading;
using EasyJob.Models;
using EasyJob.Services;

namespace EasyJob.ViewModels;

public enum TerminalShell { PowerShell, System }

/// <summary>
/// A shell the user types into, shown under the script editor. It talks to the shell over redirected pipes
/// (no pseudo-terminal), so line-oriented commands work as expected but full-screen programs (vim, top) do not.
/// </summary>
public sealed class TerminalSession : IOutputSource
{
    const int MaxLines = 20_000;
    const int TrimLines = 5_000;

    readonly AppConfig _config;
    readonly ConcurrentQueue<OutputLine> _pending = new();
    int _flushScheduled;
    Process? _process;
    int _generation;

    public TerminalSession(AppConfig config) => _config = config;

    public List<OutputLine> Lines { get; } = new();
    public event Action<IReadOnlyList<OutputLine>>? LinesAdded;
    public event Action<int>? LinesTrimmed;
    public event Action? Cleared;
    /// <summary>Raised on the UI thread when the shell starts or exits.</summary>
    public event Action? StateChanged;

    public bool IsRunning => _process is { HasExited: false };
    public string? WorkingDirectory { get; private set; }

    public static bool IsAvailable(AppConfig config, TerminalShell shell) =>
        shell == TerminalShell.PowerShell ? ShellResolver.ResolvePowerShell(config) != null : ShellResolver.ResolveShell(config) != null;

    public void Start(TerminalShell shell, string? workingDirectory)
    {
        Stop();
        int gen = ++_generation;
        WorkingDirectory = !string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory)
            ? workingDirectory : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = WorkingDirectory
        };
        psi.Environment["FORCE_COLOR"] = "1";
        psi.Environment["PYTHONUNBUFFERED"] = "1";

        string? prelude = null;
        try
        {
            if (shell == TerminalShell.PowerShell)
            {
                psi.FileName = ShellResolver.ResolvePowerShell(_config) ?? throw new InvalidOperationException("PowerShell was not found. Install PowerShell (pwsh) or set its path in Settings → Configuration.");
                foreach (var a in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", "-" }) psi.ArgumentList.Add(a);
                if (_config.powershell_native_colors) prelude = ScriptCommand.ColorPrelude;
            }
            else if (ShellResolver.IsWindows)
            {
                psi.FileName = ShellResolver.ResolveShell(_config) ?? "cmd.exe";
                psi.Arguments = "/Q";
            }
            else
            {
                psi.FileName = ShellResolver.ResolveShell(_config) ?? throw new InvalidOperationException("No POSIX shell (/bin/sh) was found.");
            }

            var p = Process.Start(psi) ?? throw new InvalidOperationException("The shell could not be started.");
            _process = p;
            Enqueue(OutputParser.Meta($"{Path.GetFileName(psi.FileName)} started in {WorkingDirectory}"));
            if (prelude != null) Send(prelude, echo: false);

            _ = PumpAsync(p.StandardOutput, new OutputParser(), LineKind.Output, gen);
            _ = PumpAsync(p.StandardError, new OutputParser(), LineKind.Error, gen);
            _ = WatchAsync(p, gen);
        }
        catch (Exception ex)
        {
            Enqueue(new OutputLine(new[] { new Segment(ex.Message, null, false, false) }, LineKind.Error));
        }
        StateChanged?.Invoke();
    }

    async Task WatchAsync(Process p, int gen)
    {
        try { await p.WaitForExitAsync(); } catch { }
        if (gen != _generation) return;
        Enqueue(OutputParser.Meta($"■ shell exited with code {SafeExitCode(p)}"));
        Dispatcher.UIThread.Post(() => { if (gen == _generation) StateChanged?.Invoke(); });
    }

    static int SafeExitCode(Process p) { try { return p.ExitCode; } catch { return -1; } }

    async Task PumpAsync(StreamReader reader, OutputParser parser, LineKind kind, int gen)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
                if (gen == _generation) Enqueue(parser.Parse(line, kind));
        }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }

    /// <summary>Sends one command line to the shell and echoes it into the output.</summary>
    public void Send(string text, bool echo = true)
    {
        if (_process is not { HasExited: false } p) return;
        if (echo) Enqueue(new OutputLine(new[] { new Segment("> " + text, Avalonia.Media.Color.Parse("#57AB5A"), true, false) }, LineKind.Output));
        try
        {
            p.StandardInput.WriteLine(text);
            p.StandardInput.Flush();
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>Kills the shell and anything it started.</summary>
    public void Stop()
    {
        var p = _process;
        _process = null;
        _generation++;
        if (p == null) return;
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
        try { p.Dispose(); } catch { }
        StateChanged?.Invoke();
    }

    public void Clear()
    {
        while (_pending.TryDequeue(out _)) { }
        Lines.Clear();
        Cleared?.Invoke();
    }

    void Enqueue(OutputLine line)
    {
        _pending.Enqueue(line);
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
}
