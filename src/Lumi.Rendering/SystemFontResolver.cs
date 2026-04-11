namespace Lumi.Rendering;

using SkiaSharp;

/// <summary>
/// Detects and registers platform-specific system fonts (emoji, symbol, CJK)
/// as fallback fonts for rendering characters not covered by the primary font.
/// </summary>
public static class SystemFontResolver
{
    private static int _initialized;

    /// <summary>
    /// Detect and register system fallback fonts. Safe to call multiple times —
    /// only initializes once.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        RegisterEmojiFallback();
        RegisterSymbolFallback();
    }

    private static void RegisterEmojiFallback()
    {
        // Try platform-specific emoji fonts in priority order
        string[] emojiFonts = OperatingSystem.IsWindows()
            ? ["Segoe UI Emoji", "Segoe UI Symbol"]
            : OperatingSystem.IsMacOS()
                ? ["Apple Color Emoji"]
                : ["Noto Color Emoji", "Noto Emoji", "Twemoji"];

        foreach (var family in emojiFonts)
        {
            var typeface = SKTypeface.FromFamilyName(family);
            if (typeface != null && typeface.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase))
            {
                FontManager.RegisterFallbackTypeface("emoji", typeface);
                return;
            }
            typeface?.Dispose();
        }
    }

    private static void RegisterSymbolFallback()
    {
        // Try common symbol fonts
        string[] symbolFonts = OperatingSystem.IsWindows()
            ? ["Segoe UI Symbol", "Segoe MDL2 Assets"]
            : OperatingSystem.IsMacOS()
                ? ["Apple Symbols", "SF Pro"]
                : ["Noto Sans Symbols", "Noto Sans Symbols2"];

        foreach (var family in symbolFonts)
        {
            var typeface = SKTypeface.FromFamilyName(family);
            if (typeface != null && typeface.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase))
            {
                FontManager.RegisterFallbackTypeface("symbol", typeface);
                return;
            }
            typeface?.Dispose();
        }
    }

    /// <summary>
    /// Reset initialization state. For testing only.
    /// </summary>
    internal static void Reset()
    {
        Interlocked.Exchange(ref _initialized, 0);
    }
}
