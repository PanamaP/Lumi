using Lumi.Platform;
using Lumi.Rendering;

namespace Lumi;

/// <summary>
/// A secondary window that can be opened from the main window via <see cref="WindowManager"/>.
/// Each secondary window has its own element tree, style resolver, layout engine, and renderer.
/// </summary>
public class SecondaryWindow : Window
{
    internal Sdl3Window? PlatformWindow { get; set; }
    internal SkiaRenderer? SecondaryRenderer { get; set; }

    /// <summary>
    /// Whether this secondary window is currently open and visible.
    /// </summary>
    public bool IsOpen { get; internal set; }

    /// <summary>
    /// Request this secondary window to close. The <see cref="WindowManager"/> will
    /// dispose its platform resources on the next update cycle.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
    }
}
