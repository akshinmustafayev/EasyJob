using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EasyJob.Controls;
using EasyJob.Models;
using EasyJob.Services;

namespace EasyJob.Dialogs;

/// <summary>Add or edit a script button.</summary>
public partial class ButtonEditorDialog : Window
{
    readonly List<(TextBox Question, string Answer)> _rows = new();

    public ButtonEditorDialog() => InitializeComponent();

    public ButtonEditorDialog(ConfigButton? existing) : this()
    {
        Title = existing == null ? "Add script button" : "Edit script button";
        Shell.Heading = Title;
        ScriptBox.TextChanged += (_, _) => UpdateHint();
        TypeBox.SelectionChanged += (_, _) => UpdateHint();
        PathTypeBox.SelectionChanged += (_, _) => UpdateHint();
        Opened += (_, _) => TextBox.Focus();
        if (existing == null)
        {
            TypeBox.SelectedIndex = 0;
            UpdateHint();
            return;
        }

        TextBox.Text = existing.Text;
        DescriptionBox.Text = existing.Description;
        ScriptBox.Text = existing.Script;
        Select(PathTypeBox, existing.ScriptPathType);
        Select(TypeBox, existing.ScriptType);
        foreach (var a in existing.Arguments) AddRow(a.ArgumentQuestion, a.ArgumentAnswer);
        UpdateHint();
    }

    /// <summary>Tells the user which file will actually run, and flags a missing file before they save.</summary>
    void UpdateHint()
    {
        if (string.IsNullOrWhiteSpace(ScriptBox.Text) || TypeBox.SelectedItem == null || PathTypeBox.SelectedItem == null)
        {
            ScriptHint.Text = "";
            return;
        }
        var probe = new ConfigButton { Script = ScriptBox.Text.Trim(), ScriptPathType = Selected(PathTypeBox) };
        var resolved = ScriptCommand.ResolveScriptPath(probe);
        ScriptHint.Text = File.Exists(resolved) ? "Found: " + resolved : "File not found yet: " + resolved;
    }

    static void Select(ComboBox box, string value)
    {
        var item = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => string.Equals((string?)i.Tag, value, StringComparison.OrdinalIgnoreCase));
        if (item != null) box.SelectedItem = item;
    }

    static string Selected(ComboBox box) => (string)((ComboBoxItem)box.SelectedItem!).Tag!;

    void AddRow(string question, string answer)
    {
        var box = new TextBox { Text = question, Watermark = "Question, e.g. Enter the server name" };
        var remove = new Button { Content = new Icon { Kind = "trash" }, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        remove.Classes.Add("flat");
        ToolTip.SetTip(remove, "Remove question");
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(box);
        Grid.SetColumn(remove, 1);
        row.Children.Add(remove);

        var entry = (box, answer);
        _rows.Add(entry);
        ArgRows.Children.Add(row);
        remove.Click += (_, _) => { _rows.Remove(entry); ArgRows.Children.Remove(row); };
    }

    void OnAddArgument(object? s, RoutedEventArgs e) => AddRow("", "");

    async void OnBrowse(object? s, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Select script", AllowMultiple = false });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path == null) return;

        var baseDir = Path.GetFullPath(ConfigService.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (path.StartsWith(baseDir, StringComparison.Ordinal))
        {
            ScriptBox.Text = path[baseDir.Length..];
            Select(PathTypeBox, "relative");
        }
        else
        {
            ScriptBox.Text = path;
            Select(PathTypeBox, "absolute");
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".ps1") Select(TypeBox, "powershell");
        else if (ext == ".py") Select(TypeBox, "python");
        else if (ext is ".bat" or ".cmd") Select(TypeBox, "cmd");
        else if (ext is ".sh" or ".bash" or ".zsh") Select(TypeBox, "shell");
        if (string.IsNullOrWhiteSpace(TextBox.Text)) TextBox.Text = Path.GetFileNameWithoutExtension(path);
    }

    void OnOk(object? s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TextBox.Text)) { ErrorText.Text = "Enter a button text."; return; }
        if (string.IsNullOrWhiteSpace(ScriptBox.Text)) { ErrorText.Text = "Choose a script."; return; }

        Close(new ConfigButton
        {
            Text = TextBox.Text.Trim(),
            Description = DescriptionBox.Text?.Trim() ?? "",
            Script = ScriptBox.Text.Trim(),
            ScriptPathType = Selected(PathTypeBox),
            ScriptType = Selected(TypeBox),
            Arguments = _rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Question.Text))
                .Select(r => new ConfigArgument { ArgumentQuestion = r.Question.Text!.Trim(), ArgumentAnswer = r.Answer })
                .ToList()
        });
    }

    void OnCancel(object? s, RoutedEventArgs e) => Close(null);
}
