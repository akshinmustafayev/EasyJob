using System.Diagnostics;

namespace EasyJob.Services;

/// <summary>Opens files, folders and links with the operating system's default handler.</summary>
public static class Launcher
{
    public static void Open(string pathOrUrl)
    {
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(pathOrUrl) { UseShellExecute = true });
        else if (OperatingSystem.IsMacOS())
            Process.Start(new ProcessStartInfo("open") { ArgumentList = { pathOrUrl } });
        else
            Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { pathOrUrl } });
    }

    /// <summary>Reveals a file in the file manager (selected where the platform supports it).</summary>
    public static void Reveal(string filePath)
    {
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { "/select,", filePath } });
        else if (OperatingSystem.IsMacOS())
            Process.Start(new ProcessStartInfo("open") { ArgumentList = { "-R", filePath } });
        else
            Open(Path.GetDirectoryName(filePath) ?? filePath);
    }
}
