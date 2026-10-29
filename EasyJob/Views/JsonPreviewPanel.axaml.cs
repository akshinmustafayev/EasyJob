using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace EasyJob.Views;

/// <summary>Read-only, pretty-printed view of the JSON in the editor. Updates shortly after typing stops.</summary>
public partial class JsonPreviewPanel : UserControl
{
    const int MaxChars = 4_000_000;

    static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    static readonly JsonDocumentOptions ParseOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    readonly RegistryOptions _registry = new(ThemeName.DarkPlus);
    readonly TextMate.Installation _textMate;
    readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(250) };
    string _pending = "";
    bool _hasValid;

    public JsonPreviewPanel()
    {
        InitializeComponent();
        _textMate = View.InstallTextMate(_registry);
        try { _textMate.SetGrammar(_registry.GetScopeByLanguageId("json")); } catch { }
        View.Options.EnableHyperlinks = false;
        _debounce.Tick += (_, _) => { _debounce.Stop(); Render(_pending); };
    }

    public void ApplyTheme(bool dark)
    {
        try { _textMate.SetTheme(_registry.LoadTheme(dark ? ThemeName.DarkPlus : ThemeName.LightPlus)); } catch { }
        View.Background = new SolidColorBrush(Color.Parse(dark ? "#1E1E1E" : "#FFFFFF"));
        View.Foreground = new SolidColorBrush(Color.Parse(dark ? "#D4D4D4" : "#1F2328"));
    }

    /// <summary>Shows the formatted version of <paramref name="json"/> (debounced while the user is typing).</summary>
    public void Update(string json, bool immediate = false)
    {
        _pending = json;
        _debounce.Stop();
        if (immediate) Render(json); else _debounce.Start();
    }

    void Render(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            View.Text = "";
            _hasValid = false;
            Status("Nothing to show yet.", error: false);
            return;
        }
        if (json.Length > MaxChars)
        {
            Status("This file is too large to preview.", error: true);
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(json, ParseOptions);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, WriterOptions)) doc.WriteTo(writer);
            View.Text = Encoding.UTF8.GetString(stream.ToArray());
            _hasValid = true;
            Status("Valid JSON", error: false);
        }
        catch (JsonException ex)
        {
            // Keep the last good result so the panel doesn't flicker while the user is mid-edit.
            Status($"Invalid JSON — line {(ex.LineNumber ?? 0) + 1}, position {(ex.BytePositionInLine ?? 0) + 1}: {ex.Message.Split(" Path:")[0]}"
                   + (_hasValid ? "\nShowing the last valid version." : ""), error: true);
        }
    }

    void Status(string text, bool error)
    {
        StatusText.Text = text;
        StatusText.Foreground = error ? new SolidColorBrush(Color.Parse("#D1242F")) : null;
        if (!error) StatusText.ClearValue(TextBlock.ForegroundProperty);
    }

    async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && View.Text.Length > 0)
            await clipboard.SetTextAsync(View.Text);
    }
}
