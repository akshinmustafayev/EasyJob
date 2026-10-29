using Avalonia.Controls;
using Avalonia.Threading;

namespace EasyJob.Views;

/// <summary>Rendered view of the Markdown in the editor. Updates shortly after typing stops.</summary>
public partial class MarkdownPreviewPanel : UserControl
{
    const int MaxChars = 1_000_000;

    readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(300) };
    string _pending = "";

    public MarkdownPreviewPanel()
    {
        InitializeComponent();
        _debounce.Tick += (_, _) => { _debounce.Stop(); Render(_pending); };
    }

    public void Update(string markdown, bool immediate = false)
    {
        _pending = markdown;
        _debounce.Stop();
        if (immediate) Render(markdown); else _debounce.Start();
    }

    void Render(string markdown) =>
        Viewer.Markdown = markdown.Length > MaxChars ? "_This file is too large to preview._" : markdown;
}
