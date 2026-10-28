using CommunityToolkit.Mvvm.ComponentModel;
using EasyJob.Models;

namespace EasyJob.ViewModels;

public partial class ButtonViewModel : ObservableObject
{
    public ButtonViewModel(ConfigButton model) => Model = model;

    public ConfigButton Model { get; private set; }

    public Guid Id => Model.Id;
    public string Text => Model.Text;
    public string Description => Model.Description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Model.Description);
    public string Tooltip => HasDescription ? $"{Model.Description}\n{Model.Script}" : Model.Script;
    public string TypeBadge => Model.ScriptType.ToLowerInvariant() switch
    {
        "powershell" => "PS",
        "python" => "PY",
        "cmd" => OperatingSystem.IsWindows() ? "CMD" : "SH",
        "shell" => "SH",
        _ => Model.ScriptType.ToUpperInvariant()
    };

    public void Replace(ConfigButton model)
    {
        Model = model;
        OnPropertyChanged(string.Empty);
    }
}
