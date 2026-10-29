using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
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
    public static readonly StyledProperty<IOutputSource?> RunProperty =
        AvaloniaProperty.Register<OutputView, IOutputSource?>(nameof(Run));

    public static readonly StyledProperty<string?> ConsoleBackgroundProperty =
        AvaloniaProperty.Register<OutputView, string?>(nameof(ConsoleBackground));

    public static readonly StyledProperty<string?> ConsoleForegroundProperty =
        AvaloniaProperty.Register<OutputView, string?>(nameof(ConsoleForeground));

    static readonly Color ErrorColor = Color.Parse("#FF7B72");
    static readonly Color MetaColor = Color.Parse("#7D8590");

    readonly TextEditor _editor;
    readonly LineColorizer _colorizer;
    readonly Dictionary<Color, IBrush> _brushes = new();
    readonly List<OutputLine> _lines = new();
    IOutputSource? _attached;
    IBrush _foreground = Brushes.White;

    static OutputView()
    {
        RunProperty.Changed.AddClassHandler<OutputView>((v, _) => v.Rebind());
        ConsoleBackgroundProperty.Changed.AddClassHandler<OutputView>((v, _) => v.ApplyColors());
        ConsoleForegroundProperty.Changed.AddClassHandler<OutputView>((v, _) => v.ApplyColors());
    }

    public OutputView()
    {
        // AvaloniaEdit only lays out and draws the visible lines, so 100k lines cost the same as 100.
        _editor = new TextEditor
        {
            FontFamily = new FontFamily("Cascadia Mono, Menlo, Consolas, DejaVu Sans Mono, Liberation Mono, monospace"),
            FontSize = 12.5,
            IsReadOnly = true,
            WordWrap = true,
            ShowLineNumbers = false,
            Background = Brushes.Transparent,
            Padding = new Thickness(12, 10),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Document = new TextDocument()
        };
        _editor.Options.EnableHyperlinks = false;
        _editor.Options.EnableEmailHyperlinks = false;
        _editor.TextArea.Caret.CaretBrush = Brushes.Transparent;
        _colorizer = new LineColorizer(this);
        _editor.TextArea.TextView.LineTransformers.Add(_colorizer);
        Child = _editor;
        CornerRadius = new CornerRadius(0, 0, 10, 10);
        ApplyColors();
    }

    public IOutputSource? Run { get => GetValue(RunProperty); set => SetValue(RunProperty, value); }
    public string? ConsoleBackground { get => GetValue(ConsoleBackgroundProperty); set => SetValue(ConsoleBackgroundProperty, value); }
    public string? ConsoleForeground { get => GetValue(ConsoleForegroundProperty); set => SetValue(ConsoleForegroundProperty, value); }

    void ApplyColors()
    {
        Background = Parse(ConsoleBackground, Color.Parse("#0C0C0C"));
        _foreground = Parse(ConsoleForeground, Color.Parse("#E6E6E6"));
        _editor.Foreground = _foreground;
        _editor.TextArea.TextView.Redraw();
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
        Reset();
        if (Run == null || VisualRoot == null) return;

        _attached = Run;
        _attached.LinesAdded += OnLinesAdded;
        _attached.LinesTrimmed += OnLinesTrimmed;
        _attached.Cleared += OnCleared;

        Append(_attached.Lines);
        ScrollToEndSoon();
    }

    void Reset()
    {
        _lines.Clear();
        _editor.Document = new TextDocument();
    }

    // Every line is stored as its text plus "\n", so document line N is _lines[N-1] (the last document line is empty).
    void Append(IReadOnlyList<OutputLine> lines)
    {
        if (lines.Count == 0) return;
        var sb = new System.Text.StringBuilder();
        foreach (var l in lines) sb.Append(l.PlainText).Append('\n');
        _lines.AddRange(lines);
        _editor.Document.Insert(_editor.Document.TextLength, sb.ToString());
    }

    void OnLinesAdded(IReadOnlyList<OutputLine> lines)
    {
        bool follow = IsNearBottom();
        Append(lines);
        if (follow) ScrollToEndSoon();
    }

    void OnLinesTrimmed(int count)
    {
        count = Math.Min(count, _lines.Count);
        if (count <= 0) return;
        var doc = _editor.Document;
        int end = doc.GetLineByNumber(count).EndOffset + 1; // include the newline of the last dropped line
        _lines.RemoveRange(0, count);
        doc.Remove(0, Math.Min(end, doc.TextLength));
    }

    void OnCleared() => Reset();

    bool IsNearBottom()
    {
        var tv = _editor.TextArea.TextView;
        return tv.VerticalOffset + tv.Bounds.Height >= tv.DocumentHeight - 24;
    }

    void ScrollToEndSoon() => Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Background);

    IBrush Brush(Color c)
    {
        if (!_brushes.TryGetValue(c, out var b)) _brushes[c] = b = new SolidColorBrush(c);
        return b;
    }

    public void ScrollToTop() => _editor.ScrollToHome();

    public void ScrollToBottom()
    {
        // The document ends with an empty line; scroll so the last real line sits at the bottom of the view.
        int last = Math.Max(1, _editor.Document.LineCount - 1);
        _editor.ScrollToLine(last);
    }

    sealed class LineColorizer : DocumentColorizingTransformer
    {
        readonly OutputView _owner;
        public LineColorizer(OutputView owner) => _owner = owner;

        protected override void ColorizeLine(DocumentLine docLine)
        {
            int idx = docLine.LineNumber - 1;
            if (idx < 0 || idx >= _owner._lines.Count) return;
            var line = _owner._lines[idx];

            int offset = docLine.Offset;
            foreach (var seg in line.Segments)
            {
                int len = seg.Text.Length;
                if (len == 0) continue;
                int from = offset, to = offset + len;
                offset = to;

                IBrush? fg = seg.Foreground is { } c ? _owner.Brush(c)
                    : line.Kind == LineKind.Error ? _owner.Brush(ErrorColor)
                    : line.Kind == LineKind.Meta ? _owner.Brush(MetaColor) : null;
                bool italic = line.Kind == LineKind.Meta && seg.Foreground == null;

                if (fg == null && !seg.Bold && !seg.Underline && !italic) continue;
                ChangeLinePart(from, to, el =>
                {
                    if (fg != null) el.TextRunProperties.SetForegroundBrush(fg);
                    if (seg.Bold || italic)
                    {
                        var tf = el.TextRunProperties.Typeface;
                        el.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily,
                            italic ? FontStyle.Italic : tf.Style, seg.Bold ? FontWeight.Bold : tf.Weight));
                    }
                    if (seg.Underline) el.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                });
            }
        }
    }
}
