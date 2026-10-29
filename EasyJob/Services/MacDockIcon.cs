using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace EasyJob.Services;

/// <summary>
/// macOS takes the Dock icon from the .app bundle, not from the window icon, so a plain executable (dotnet run,
/// or an unpacked publish folder) would show the generic icon. This sets it at run time through the Objective-C runtime.
/// </summary>
public static class MacDockIcon
{
    const string ObjC = "/usr/lib/libobjc.A.dylib";

    [DllImport(ObjC)] static extern IntPtr objc_getClass(string name);
    [DllImport(ObjC)] static extern IntPtr sel_registerName(string name);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr receiver, IntPtr selector);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr receiver, IntPtr selector, byte[] bytes, UIntPtr length);

    /// <summary>Returns true when the icon was applied. Never throws: a missing icon is only cosmetic.</summary>
    public static bool Apply()
    {
        if (!OperatingSystem.IsMacOS()) return false;
        try
        {
            NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit"); // already loaded by Avalonia; makes the class lookups safe anywhere
            using var stream = AssetLoader.Open(new Uri("avares://EasyJob/Assets/icon.png"));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var png = memory.ToArray();

            var data = Send(objc_getClass("NSData"), sel_registerName("dataWithBytes:length:"), png, (UIntPtr)png.Length);
            var image = Send(Send(objc_getClass("NSImage"), sel_registerName("alloc")), sel_registerName("initWithData:"), data);
            if (image == IntPtr.Zero) return false;

            var app = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            Send(app, sel_registerName("setApplicationIconImage:"), image);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>For tests: whether NSApplication currently has an icon image.</summary>
    public static bool HasIcon()
    {
        var app = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
        return Send(app, sel_registerName("applicationIconImage")) != IntPtr.Zero;
    }
}
