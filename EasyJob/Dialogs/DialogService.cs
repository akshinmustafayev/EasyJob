using Avalonia.Controls;
using EasyJob.Models;
using EasyJob.Services;

namespace EasyJob.Dialogs;

/// <summary>Opens the application's modal dialogs over the main window.</summary>
public static class DialogService
{
    static Window? Owner => App.ActiveWindow;

    static async Task<T?> Show<T>(Window dialog, Window? owner = null)
    {
        owner ??= Owner;
        return owner == null ? default : await dialog.ShowDialog<T?>(owner);
    }

    public enum SaveChoice { Save, Discard, Cancel }

    /// <summary>"Save changes?" prompt for the editor.</summary>
    public static async Task<SaveChoice> AskSave(string fileName)
    {
        var dialog = new MessageDialog("Unsaved changes", $"Save changes to {fileName} before closing?", "Save", "Don't save");
        var save = await Show<bool>(dialog);
        return save ? SaveChoice.Save : dialog.Discarded ? SaveChoice.Discard : SaveChoice.Cancel;
    }

    public static Task ShowMessage(string title, string message) =>
        Show<bool>(new MessageDialog(title, message, null));

    public static async Task<bool> Confirm(string title, string message, string okText = "OK") =>
        await Show<bool>(new MessageDialog(title, message, okText));

    public static Task<string?> PromptText(string title, string label, string initial, string okText) =>
        Show<string>(new PromptDialog(title, label, initial, okText));

    public static Task<ConfigButton?> EditButton(ConfigButton? existing) =>
        Show<ConfigButton>(new ButtonEditorDialog(existing));

    public static async Task<List<string>?> AskArguments(ConfigButton button)
    {
        var result = await Show<string[]>(new ArgumentsDialog(button));
        return result?.ToList();
    }

    public static async Task<List<T>?> Reorder<T>(string title, List<T> items, Func<T, string> label) where T : class
    {
        var result = await Show<List<object>>(new ReorderDialog(title, items.Cast<object>().ToList(), o => label((T)o)));
        return result?.Cast<T>().ToList();
    }

    public static async Task<bool> EditConfiguration(AppConfig config) =>
        await Show<bool>(new ConfigurationDialog(config));

    public static Task ShowAbout() => Show<bool>(new AboutDialog());
    public static Task ShowTroubleshooting(AppConfig config) => Show<bool>(new TroubleshootingDialog(config));
}
