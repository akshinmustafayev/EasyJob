using Avalonia.Media;

namespace EasyJob.Models;

public enum LineKind { Output, Error, Meta }

public readonly record struct Segment(string Text, Color? Foreground, bool Bold, bool Underline);

public sealed class OutputLine
{
    public OutputLine(Segment[] segments, LineKind kind)
    {
        Segments = segments;
        Kind = kind;
    }

    public Segment[] Segments { get; }
    public LineKind Kind { get; }

    public string PlainText => string.Concat(Segments.Select(s => s.Text));
}
