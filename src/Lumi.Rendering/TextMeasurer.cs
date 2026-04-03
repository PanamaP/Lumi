namespace Lumi.Rendering;

using SkiaSharp;
using System.Collections.Concurrent;

/// <summary>
/// Utility for measuring text dimensions using SkiaSharp fonts.
/// Caches font metrics per (family, size, weight, style) to avoid repeated lookups.
/// </summary>
public sealed class TextMeasurer
{
    private readonly ConcurrentDictionary<FontKey, CachedFontMetrics> _metricsCache = new();

    private readonly record struct FontKey(string Family, float Size, int Weight, bool Italic);

    private readonly record struct CachedFontMetrics(float Ascent, float Descent, float Leading, float LineHeight);

    /// <summary>
    /// Measure the pixel width of a string with the given font settings.
    /// </summary>
    public float MeasureWidth(string text, string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        using var font = CreateFont(fontFamily, fontSize, fontWeight, italic);
        using var paint = new SKPaint();
        return font.MeasureText(text, paint);
    }

    /// <summary>
    /// Measure a single word's width. Useful for line-breaking calculations.
    /// </summary>
    public float MeasureWordWidth(ReadOnlySpan<char> word, SKFont font, SKPaint paint)
    {
        return font.MeasureText(word, paint);
    }

    /// <summary>
    /// Get the computed line height for a font with a line-height multiplier.
    /// </summary>
    public float GetLineHeight(string fontFamily, float fontSize, int fontWeight, bool italic, float lineHeightMultiplier)
    {
        var metrics = GetCachedMetrics(fontFamily, fontSize, fontWeight, italic);
        return metrics.LineHeight * lineHeightMultiplier;
    }

    /// <summary>
    /// Get font ascent (distance from baseline to top of glyphs, as a positive value).
    /// </summary>
    public float GetAscent(string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        var metrics = GetCachedMetrics(fontFamily, fontSize, fontWeight, italic);
        return -metrics.Ascent; // SKFontMetrics.Ascent is negative
    }

    /// <summary>
    /// Create an SKFont with the given parameters.
    /// </summary>
    public static SKFont CreateFont(string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        var skStyle = fontWeight >= 700
            ? (italic ? SKFontStyle.BoldItalic : SKFontStyle.Bold)
            : (italic ? SKFontStyle.Italic : SKFontStyle.Normal);

        var typeface = SKTypeface.FromFamilyName(fontFamily, skStyle) ?? SKTypeface.Default;

        return new SKFont(typeface, fontSize)
        {
            Edging = SKFontEdging.SubpixelAntialias
        };
    }

    private CachedFontMetrics GetCachedMetrics(string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        var key = new FontKey(fontFamily, fontSize, fontWeight, italic);
        return _metricsCache.GetOrAdd(key, k =>
        {
            using var font = CreateFont(k.Family, k.Size, k.Weight, k.Italic);
            font.GetFontMetrics(out var metrics);
            float lineHeight = -metrics.Ascent + metrics.Descent + metrics.Leading;
            return new CachedFontMetrics(metrics.Ascent, metrics.Descent, metrics.Leading, lineHeight);
        });
    }
}
