using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using EasyJob.Models;
using EasyJob.ViewModels;

namespace EasyJob.Controls;

/// <summary>
/// Terminal-style, selectable, coloured output for a single script run. Replaces the old rich-text box:
/// it renders only the lines of its own run, so output from concurrent scripts can no longer mix.
/// </summary>
public class OutputView : Border
{
    public static readonly StyledProperty<RunViewModel?> RunProperty =
        AvaloniaProperty.Register<OutputView, RunViewModel?>(nameof(Run));

    public static readonly StyledProperty<string?> ConsoleBackgroundProperty =
        AvaloniaProperty.Register<OutputView, string?>(nameof(ConsoleBackground));

    public static readonly StyledProperty<string?> ConsoleForegroundProperty =
        AvaloniaProperty.Register<OutputView, string?>(nameof(ConsoleForeground));

    static readonly Color ErrorColor = Color.Parse("#FF7B72");
    static readonly Color MetaColor = Color.Parse("#7D8590");

    readonly ScrollViewer _scroll;
    readonly SelectableTextBlock _text;
    readonly Dictionary<Color, IBrush> _brushes = new();
    RunViewModel? _attached;
    IBrush _foreground = Brushes.White;

    static OutputView()
    {
        RunProperty.Changed.AddClassHandler<OutputView>((v, _) => v.Rebind());
        ConsoleBackgroundProperty.Changed.AddClassHandler<OutputView>((v, _) => v.ApplyColors());
        ConsoleForegroundProperty.Changed.AddClassHandler<OutputView>((v, _) => v.ApplyColors());
    }

    public OutputView()
    {
        _text = new SelectableTextBlock
        {
            FontFamily = new FontFamily("Cascadia Mono, Menlo, Consolas, DejaVu Sans Mono, Liberation Mono, monospace"),
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(12, 10)
        };
        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _text
        };
        Child = _scroll;
        CornerRadius = new CornerRadius(0, 0, 10, 10);
        ApplyColors();
    }

    public RunViewModel? Run { get => GetValue(RunProperty); set => SetValue(RunProperty, value); }
    public string? ConsoleBackground { get => GetValue(ConsoleBackgroundProperty); set => SetValue(ConsoleBackgroundProperty, value); }
    public string? ConsoleForeground { get => GetValue(ConsoleForegroundProperty); set => SetValue(ConsoleForegroundProperty, value); }

    void ApplyColors()
    {
        Background = Parse(ConsoleBackground, Color.Parse("#0C0C0C"));
        _foreground = Parse(ConsoleForeground, Color.Parse("#E6E6E6"));
        _text.Foreground = _foreground;
    }

    static IBrush Parse(string? value, Color fallback) =>
        new SolidColorBrush(!string.IsNullOrWhiteSpace(value) && Color.TryParse(value, out var c) ? c : fallback);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Rebind();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Detach();
        base.OnDetachedFromVisualTree(e);
    }

    void Detach()
    {
        if (_attached == null) return;
        _attached.LinesAdded -= OnLinesAdded;
        _attached.LinesTrimmed -= OnLinesTrimmed;
        _attached.Cleared -= OnCleared;
        _attached = null;
    }

    void Rebind()
    {
        Detach();
        _text.Inlines?.Clear();
        if (Run == null || VisualRoot == null) return;

        _attached = Run;
        _attached.LinesAdded += OnLinesAdded;
        _attached.LinesTrimmed += OnLinesTrimmed;
        _attached.Cleared += OnCleared;

        foreach (var line in _attached.Lines) _text.Inlines!.Add(Build(line));
        ScrollToEndSoon();
    }

    void OnLinesAdded(IReadOnlyList<OutputLine> lines)
    {
        bool follow = IsNearBottom();
        foreach (var line in lines) _text.Inlines!.Add(Build(line));
        if (follow) ScrollToEndSoon();
    }

    void OnLinesTrimmed(int count)
    {
        var inlines = _text.Inlines;
        if (inlines == null) return;
        for (int i = 0; i < count && inlines.Count > 0; i++) inlines.RemoveAt(0);
    }

    void OnCleared() => _text.Inlines?.Clear();

    bool IsNearBottom() =>
        _scroll.Offset.Y + _scroll.Viewport.Height >= _scroll.Extent.Height - 24;

    void ScrollToEndSoon() => Dispatcher.UIThread.Post(() => _scroll.ScrollToEnd(), DispatcherPriority.Background);

    Inline Build(OutputLine line)
    {
        var span = new Span();
        foreach (var seg in line.Segments)
        {
            var run = new Run(seg.Text);
            if (seg.Foreground is { } c) run.Foreground = Brush(c);
            else if (line.Kind == LineKind.Error) run.Foreground = Brush(ErrorColor);
            else if (line.Kind == LineKind.Meta) { run.Foreground = Brush(MetaColor); run.FontStyle = FontStyle.Italic; }
            if (seg.Bold) run.FontWeight = FontWeight.Bold;
            if (seg.Underline) run.TextDecorations = TextDecorations.Underline;
            span.Inlines.Add(run);
        }
        span.Inlines.Add(new LineBreak());
        return span;
    }

    IBrush Brush(Color c)
    {
        if (!_brushes.TryGetValue(c, out var b)) _brushes[c] = b = new SolidColorBrush(c);
        return b;
    }

    public void ScrollToTop() => _scroll.ScrollToHome();
    public void ScrollToBottom() => _scroll.ScrollToEnd();
}
