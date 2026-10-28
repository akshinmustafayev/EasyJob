using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyJob.Services;

namespace EasyJob.ViewModels;

/// <summary>One open file in the script editor.</summary>
public partial class EditorDocument : ObservableObject
{
    public EditorDocument(string? path, string text, EditorLanguage language, bool hasBom = false)
    {
        _path = path;
        Language = language;
        HasBom = hasBom;
        Document = new TextDocument(text);
        Document.UndoStack.MarkAsOriginalFile();
        Document.TextChanged += (_, _) => IsDirty = !Document.UndoStack.IsOriginalFile;
    }

    public TextDocument Document { get; }
    public bool HasBom { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Title), nameof(Folder))]
    string? _path;

    [ObservableProperty] EditorLanguage _language;
    [ObservableProperty] bool _isDirty;

    public string Title => Path == null ? "Untitled" : System.IO.Path.GetFileName(Path);
    public string Folder => Path == null ? "Not saved yet" : System.IO.Path.GetDirectoryName(Path) ?? "";

    public void MarkSaved()
    {
        Document.UndoStack.MarkAsOriginalFile();
        IsDirty = false;
    }
}
