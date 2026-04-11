namespace Lumi.Text;

using SkiaSharp;

/// <summary>
/// Renders shaped glyph runs to an SKCanvas using precise glyph positioning.
/// </summary>
public static class ShapedTextRenderer
{
    /// <summary>
    /// Draw a shaped glyph run at the specified origin using pre-computed glyph positions.
    /// </summary>
    public static void Draw(SKCanvas canvas, ShapedGlyphRun run, float x, float y, SKPaint paint)
    {
        if (run.GlyphIds.Length == 0)
            return;

        using var font = CreateFont(run.FontFamily, run.FontSize, run.FontWeight, run.Italic);

        int count = run.GlyphIds.Length;
        using var builder = new SKTextBlobBuilder();
        var buffer = builder.AllocatePositionedRun(font, count);

        var glyphSpan = buffer.Glyphs;
        var posSpan = buffer.Positions;

        for (int i = 0; i < count; i++)
        {
            glyphSpan[i] = run.GlyphIds[i];
            posSpan[i] = new SKPoint(run.Positions[i * 2], run.Positions[i * 2 + 1]);
        }

        using var blob = builder.Build();
        if (blob != null)
            canvas.DrawText(blob, x, y, paint);
    }

    /// <summary>
    /// Create an SKFont matching the given parameters. Checks
    /// <see cref="TextShaper.CustomTypefaceResolver"/> for registered custom fonts
    /// before falling back to system fonts.
    /// </summary>
    internal static SKFont CreateFont(string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        SKTypeface? typeface = TextShaper.CustomTypefaceResolver?.Invoke(fontFamily, fontWeight, italic);

        if (typeface == null)
        {
            typeface = TypefaceCache.GetOrCreate(fontFamily, fontWeight >= 700, italic);
        }

        return new SKFont(typeface, fontSize)
        {
            Edging = SKFontEdging.SubpixelAntialias
        };
    }
}
