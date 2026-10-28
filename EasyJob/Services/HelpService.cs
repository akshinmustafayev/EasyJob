using System.Reflection;
using System.Xml.Linq;

namespace EasyJob.Services;

public record HelpItem(string Name, string Heading, string Description, string Used);

public static class HelpService
{
    static Dictionary<string, HelpItem>? _items;

    public static string ReadEmbedded(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().Single(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(asm.GetManifestResourceStream(name)!);
        return reader.ReadToEnd();
    }

    public static HelpItem? Find(string name)
    {
        _items ??= Load();
        return _items.GetValueOrDefault(name);
    }

    static Dictionary<string, HelpItem> Load()
    {
        var result = new Dictionary<string, HelpItem>();
        try
        {
            var doc = XDocument.Parse(ReadEmbedded("HelpDocumentation.xml"));
            foreach (var e in doc.Root!.Elements("item"))
            {
                string Get(string n) => ((string?)e.Element(n))?.Trim() ?? "";
                var item = new HelpItem(Get("name"), Get("heading"), Get("description"), Get("used"));
                result[item.Name] = item;
            }
        }
        catch { /* help is optional */ }
        return result;
    }
}
