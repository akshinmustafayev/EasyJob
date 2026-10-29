using EasyJob.Models;

namespace EasyJob.ViewModels;

/// <summary>Anything an <see cref="Controls.OutputView"/> can show: a script run or an interactive terminal session.</summary>
public interface IOutputSource
{
    List<OutputLine> Lines { get; }
    /// <summary>Raised on the UI thread with each batch of new lines.</summary>
    event Action<IReadOnlyList<OutputLine>>? LinesAdded;
    /// <summary>Raised on the UI thread with the number of lines dropped from the start of the buffer.</summary>
    event Action<int>? LinesTrimmed;
    event Action? Cleared;
}
