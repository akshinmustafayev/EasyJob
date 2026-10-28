using System.Text.Json;
using EasyJob.Models;

namespace EasyJob.Services;

/// <summary>
/// Loads and saves config.json. If a config.json sits next to the executable it is used (portable mode),
/// otherwise the per-user application data folder is used, so the app also works from a read-only location
/// such as a macOS .app bundle.
/// </summary>
public static class ConfigService
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static string ConfigPath { get; } = ResolveConfigPath();

    /// <summary>Folder that relative script paths are resolved against.</summary>
    public static string BaseDirectory => Path.GetDirectoryName(ConfigPath) ?? AppContext.BaseDirectory;

    static string ResolveConfigPath()
    {
        var portable = Path.Combine(AppContext.BaseDirectory, "config.json");
        if (File.Exists(portable)) return portable;

        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyJob");
        return Path.Combine(dir, "config.json");
    }

    public static AppConfig Load()
    {
        AppConfig config;
        if (File.Exists(ConfigPath))
        {
            config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), Options) ?? new AppConfig();
        }
        else
        {
            config = CreateDefault();
            Save(config);
        }

        Normalize(config);
        return config;
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        // Write to a temp file first so a crash never leaves a half-written config behind.
        var tmp = ConfigPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, Options));
        File.Move(tmp, ConfigPath, overwrite: true);
    }

    static AppConfig CreateDefault() => new()
    {
        tabs = new List<ConfigTab>
        {
            new() { Header = "Common actions" }
        }
    };

    static void Normalize(AppConfig c)
    {
        c.restrictions ??= new ConfigRestrictions();
        c.tabs ??= new List<ConfigTab>();
        foreach (var tab in c.tabs)
        {
            if (tab.ID == Guid.Empty) tab.ID = Guid.NewGuid();
            tab.Buttons ??= new List<ConfigButton>();
            foreach (var b in tab.Buttons)
            {
                if (b.Id == Guid.Empty) b.Id = Guid.NewGuid();
                b.Arguments ??= new List<ConfigArgument>();
            }
        }
    }
}
