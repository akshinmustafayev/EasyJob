using System.Text;
using Avalonia.Media;
using EasyJob.Models;

namespace EasyJob.Services;

/// <summary>
/// Turns raw process output into styled lines. Understands ANSI SGR colour sequences (what pwsh, git, dotnet, npm
/// and most CLI tools emit). One instance per output stream
/// so that an ANSI style which spans several lines carries over correctly.
/// </summary>
public sealed class OutputParser
{
    static readonly Color[] Ansi16 =
    {
        Color.Parse("#1E1E1E"), Color.Parse("#E5534B"), Color.Parse("#57AB5A"), Color.Parse("#C69026"),
        Color.Parse("#539BF5"), Color.Parse("#B083F0"), Color.Parse("#39C5CF"), Color.Parse("#ADBAC7"),
        Color.Parse("#636E7B"), Color.Parse("#FF938A"), Color.Parse("#6BC46D"), Color.Parse("#DAAA3F"),
        Color.Parse("#6CB6FF"), Color.Parse("#DCBDFB"), Color.Parse("#56D4DD"), Color.Parse("#F0F6FC")
    };

    Color? _fg;
    bool _bold, _underline;

    public OutputLine Parse(string raw, LineKind kind)
    {
        // A carriage return rewinds the line (progress bars): keep what would be visible in a terminal.
        raw = raw.TrimEnd('\r', '\n');
        int cr = raw.LastIndexOf('\r');
        if (cr >= 0) raw = raw[(cr + 1)..];

        var segments = new List<Segment>();
        var sb = new StringBuilder();

        void Flush()
        {
            if (sb.Length == 0) return;
            segments.Add(new Segment(sb.ToString(), _fg, _bold, _underline));
            sb.Clear();
        }

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            if (c == '\u001b' && i + 1 < raw.Length && raw[i + 1] == '[')
            {
                int j = i + 2;
                while (j < raw.Length && (raw[j] is >= '0' and <= '?')) j++;       // parameter bytes
                while (j < raw.Length && (raw[j] is >= ' ' and <= '/')) j++;       // intermediate bytes
                if (j < raw.Length)
                {
                    if (raw[j] == 'm')
                    {
                        Flush();
                        ApplySgr(raw.Substring(i + 2, j - i - 2));
                    }
                    i = j;
                    continue;
                }
            }
            else if (c == '\u001b')
            {
                continue; // stray escape (OSC titles etc.): drop it
            }

            if (c == '\t') sb.Append("    ");
            else if (!char.IsControl(c)) sb.Append(c);
        }
        Flush();

        if (segments.Count == 0) segments.Add(new Segment("", null, false, false));
        return new OutputLine(segments.ToArray(), kind);
    }

    void ApplySgr(string parameters)
    {
        var parts = parameters.Length == 0 ? new[] { "0" } : parameters.Split(';', ':');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int p)) p = 0;
            switch (p)
            {
                case 0: _fg = null; _bold = false; _underline = false; break;
                case 1: _bold = true; break;
                case 22: _bold = false; break;
                case 4: _underline = true; break;
                case 24: _underline = false; break;
                case >= 30 and <= 37: _fg = Ansi16[p - 30]; break;
                case 39: _fg = null; break;
                case >= 90 and <= 97: _fg = Ansi16[p - 90 + 8]; break;
                case 38:
                    if (i + 2 < parts.Length && parts[i + 1] == "5" && int.TryParse(parts[i + 2], out int idx))
                    {
                        _fg = Ansi256(idx); i += 2;
                    }
                    else if (i + 4 < parts.Length && parts[i + 1] == "2"
                             && byte.TryParse(parts[i + 2], out byte r) && byte.TryParse(parts[i + 3], out byte g) && byte.TryParse(parts[i + 4], out byte b))
                    {
                        _fg = Color.FromRgb(r, g, b); i += 4;
                    }
                    break;
            }
        }
    }

    static Color Ansi256(int i)
    {
        if (i < 0) i = 0;
        if (i < 16) return Ansi16[i];
        if (i < 232)
        {
            i -= 16;
            static byte Level(int v) => (byte)(v == 0 ? 0 : 55 + v * 40);
            return Color.FromRgb(Level(i / 36), Level(i / 6 % 6), Level(i % 6));
        }
        byte gray = (byte)Math.Min(255, 8 + (i - 232) * 10);
        return Color.FromRgb(gray, gray, gray);
    }

    public static OutputLine Meta(string text) =>
        new(new[] { new Segment(text, null, false, false) }, LineKind.Meta);
}
