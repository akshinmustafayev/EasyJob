namespace EasyJob.Services;

/// <summary>A language the script editor knows: how to highlight it and, for scripts, how to run it.</summary>
public sealed record EditorLanguage(string Id, string Name, string? TextMateId, string? ScriptType, string[] Extensions, string Template)
{
    public bool CanRun => ScriptType != null;
    public override string ToString() => Name;

    public static readonly EditorLanguage PowerShell = new("powershell", "PowerShell", "powershell", "powershell",
        new[] { ".ps1", ".psm1", ".psd1" }, "# New PowerShell script\nparam()\n\nWrite-Host \"Hello from PowerShell\"\n");
    public static readonly EditorLanguage Python = new("python", "Python", "python", "python",
        new[] { ".py", ".pyw" }, "# New Python script\nimport sys\n\n\ndef main():\n    print(\"Hello from Python\", sys.argv[1:])\n\n\nif __name__ == \"__main__\":\n    main()\n");
    public static readonly EditorLanguage Shell = new("shell", "Shell", "shellscript", "shell",
        new[] { ".sh", ".bash", ".zsh" }, "#!/bin/sh\n# New shell script\necho \"Hello from sh\"\n");
    public static readonly EditorLanguage Batch = new("batch", "Batch", "bat", "cmd",
        new[] { ".bat", ".cmd" }, "@echo off\nrem New batch script\necho Hello from cmd\n");
    public static readonly EditorLanguage Json = new("json", "JSON", "json", null, new[] { ".json" }, "{\n}\n");
    public static readonly EditorLanguage Yaml = new("yaml", "YAML", "yaml", null, new[] { ".yml", ".yaml" }, "");
    public static readonly EditorLanguage Xml = new("xml", "XML", "xml", null, new[] { ".xml", ".xaml", ".axaml", ".csproj", ".props" }, "");
    public static readonly EditorLanguage Markdown = new("markdown", "Markdown", "markdown", null, new[] { ".md", ".markdown" }, "");
    public static readonly EditorLanguage Plain = new("plain", "Plain text", null, null, new[] { ".txt", ".log" }, "");

    public static readonly IReadOnlyList<EditorLanguage> All = new[] { PowerShell, Python, Shell, Batch, Json, Yaml, Xml, Markdown, Plain };
    public static readonly IReadOnlyList<EditorLanguage> Scripts = new[] { PowerShell, Python, Shell, Batch };

    public static EditorLanguage ForPath(string? path)
    {
        var ext = Path.GetExtension(path ?? "").ToLowerInvariant();
        return All.FirstOrDefault(l => l.Extensions.Contains(ext)) ?? Plain;
    }

    public static EditorLanguage ForScriptType(string scriptType) =>
        All.FirstOrDefault(l => string.Equals(l.ScriptType, scriptType, StringComparison.OrdinalIgnoreCase)) ?? Plain;
}
