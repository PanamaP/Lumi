namespace Lumi.Rendering;

using Lumi.Text;
using SkiaSharp;

/// <summary>
/// Global configuration for text rendering. When <see cref="UseHarfBuzz"/> is enabled,
/// text is shaped through HarfBuzz before rendering for more accurate glyph positioning
/// (especially beneficial for non-Latin scripts and complex ligatures).
/// </summary>
public static class TextRenderingOptions
{
    private static volatile bool _useHarfBuzz = true;
    private static volatile TextShaper? _shaper;
    private static readonly object _lock = new();

    /// <summary>
    /// When true, text goes through HarfBuzz shaping before rendering.
    /// Default is true (uses HarfBuzz shaping for accurate glyph positioning).
    /// </summary>
    public static bool UseHarfBuzz
    {
        get => _useHarfBuzz;
        set
        {
            _useHarfBuzz = value;
            if (value)
                EnsureInitialized();
        }
    }

    /// <summary>
    /// Get the shared TextShaper instance. Creates one on first call and wires
    /// FontManager as the custom typeface resolver.
    /// </summary>
    public static TextShaper GetShaper()
    {
        EnsureInitialized();
        return _shaper!;
    }

    private static void EnsureInitialized()
    {
        if (_shaper != null) return;
        lock (_lock)
        {
            if (_shaper != null) return;
            TextShaper.CustomTypefaceResolver = ResolveTypeface;
            _shaper = new TextShaper();
        }
    }

    private static SKTypeface? ResolveTypeface(string family, int weight, bool italic)
    {
        if (FontManager.IsRegistered(family))
            return FontManager.GetTypeface(family, weight, italic);
        return null;
    }

    /// <summary>
    /// Reset state. Primarily useful for test isolation.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _useHarfBuzz = true;
            _shaper?.Dispose();
            _shaper = null;
            TextShaper.CustomTypefaceResolver = null;
        }
    }
}
