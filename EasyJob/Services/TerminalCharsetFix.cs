using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Iciclecreek.Terminal;
using XTerm.Events.Parser;

namespace EasyJob.Services;

/// <summary>
/// XTerm.NET (the emulator inside the terminal control) does not clear the escape-sequence intermediates between
/// sequences, so ESC ( B followed by ESC ) 0 ("G1 = line drawing", sent by lf, tcell, ncurses...) arrives as "()" and is
/// applied to G0. G0 then stays line drawing and the whole screen (and the shell prompt) shows box-drawing characters
/// instead of letters. This reads the real designator (the last intermediate), keeps G0 and sets the right set.
/// Remove when the library handles this itself.
/// </summary>
public static class TerminalCharsetFix
{
    static readonly ConditionalWeakTable<object, object> Patched = new();
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Apply(TerminalControl control)
    {
        try
        {
            var view = control.GetVisualDescendants().FirstOrDefault(v => v.GetType().Name == "TerminalView");
            var terminal = view?.GetType().GetProperty("Terminal", Any)?.GetValue(view);
            if (terminal == null || Patched.TryGetValue(terminal, out _)) return;

            var parser = terminal.GetType().GetField("_parser", Any)?.GetValue(terminal);
            var input = terminal.GetType().GetField("_inputHandler", Any)?.GetValue(terminal);
            var setCharset = input?.GetType().GetMethod("SetCharset", Any);
            var evt = parser?.GetType().GetEvent("Esc", Any | BindingFlags.Static);
            if (parser == null || input == null || setCharset == null || evt == null) return;

            var modeType = setCharset.GetParameters()[0].ParameterType;
            object Mode(string name) => Enum.Parse(modeType, name);
            var g0 = "B";

            EventHandler<EscEventArgs> handler = (_, e) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(e.Collected)) return;
                    switch (e.Collected[^1])
                    {
                        case '(':
                            g0 = e.FinalChar;
                            setCharset.Invoke(input, new[] { Mode("G0"), g0 });
                            break;
                        case ')' or '*' or '+':
                            // The library may have put this designation into G0: restore G0, store it where it belongs.
                            setCharset.Invoke(input, new[] { Mode("G0"), g0 });
                            setCharset.Invoke(input, new[] { Mode(e.Collected[^1] == ')' ? "G1" : e.Collected[^1] == '*' ? "G2" : "G3"), e.FinalChar });
                            break;
                    }
                }
                catch { }
            };
            evt.AddEventHandler(parser, handler);
            Patched.Add(terminal, handler);
        }
        catch { }
    }
}
