namespace Lumi.Text;

using SkiaSharp;
using Lumi.Core;

/// <summary>
/// A single line of shaped text with one or more glyph runs and position.
/// </summary>
public sealed class ShapedTextLine
{
    public List<ShapedGlyphRun> Runs { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }

    /// <summary>Convenience: the first (or only) run. For backward compatibility.</summary>
    public ShapedGlyphRun Run => Runs[0];

    public ShapedTextLine(ShapedGlyphRun run, float x, float y, float width)
    {
        Runs = [run];
        X = x;
        Y = y;
        Width = width;
    }

    public ShapedTextLine(List<ShapedGlyphRun> runs, float x, float y, float width)
    {
        Runs = runs;
        X = x;
        Y = y;
        Width = width;
    }
}

/// <summary>
/// Result of laying out shaped text within constrained dimensions.
/// </summary>
public sealed class ShapedTextLayoutResult
{
    public List<ShapedTextLine> Lines { get; } = [];
    public float TotalHeight { get; set; }
    public float MaxWidth { get; set; }
    public bool WasTruncated { get; set; }
}

/// <summary>
/// Performs line breaking on shaped text using glyph advances for precise width calculation.
/// Supports word wrap, text alignment, and ellipsis truncation.
/// </summary>
public static class ShapedTextLayout
{
    /// <summary>
    /// Lay out text within the given constraints using HarfBuzz shaping for precise glyph measurement.
    /// </summary>
    public static ShapedTextLayoutResult Layout(
        string text,
        float availableWidth,
        float availableHeight,
        ComputedStyle style,
        TextShaper shaper)
    {
        var result = new ShapedTextLayoutResult();
        if (string.IsNullOrEmpty(text) || availableWidth <= 0)
            return result;

        string fontFamily = style.FontFamily;
        float fontSize = style.FontSize;
        int fontWeight = style.FontWeight;
        bool italic = style.FontStyle == FontStyle.Italic;

        using var skFont = ShapedTextRenderer.CreateFont(fontFamily, fontSize, fontWeight, italic);
        skFont.GetFontMetrics(out var metrics);
        float rawLineHeight = -metrics.Ascent + metrics.Descent + metrics.Leading;
        float lineHeight = rawLineHeight * style.LineHeight;
        float ascent = -metrics.Ascent;

        // No-wrap mode
        if (style.WhiteSpace == WhiteSpace.NoWrap)
        {
            var runs = shaper.ShapeMultiScript(text, fontFamily, fontSize, fontWeight, italic);
            float textWidth = runs.Sum(r => r.TotalWidth);

            if (style.TextOverflow == TextOverflow.Ellipsis && textWidth > availableWidth)
            {
                var run = TruncateWithEllipsis(text, availableWidth, shaper, fontFamily, fontSize, fontWeight, italic);
                textWidth = run.TotalWidth;
                result.WasTruncated = true;
                float x = AlignX(textWidth, availableWidth, style.TextAlign);
                result.Lines.Add(new ShapedTextLine(run, x, ascent, textWidth));
            }
            else
            {
                float x = AlignX(textWidth, availableWidth, style.TextAlign);
                result.Lines.Add(new ShapedTextLine(runs, x, ascent, textWidth));
            }
            result.TotalHeight = lineHeight;
            result.MaxWidth = textWidth;
            return result;
        }

        // Word wrap mode
        var words = SplitWords(text);
        float spaceWidth = shaper.Shape(" ", fontFamily, fontSize, fontWeight, italic).TotalWidth;

        var currentLineWords = new List<string>();
        float currentLineWidth = 0;
        float y = ascent;
        List<string>? lastEmittedWords = null;

        for (int wi = 0; wi < words.Count; wi++)
        {
            string word = words[wi];
            float wordWidth = shaper.Shape(word, fontFamily, fontSize, fontWeight, italic).TotalWidth;

            if (currentLineWords.Count > 0)
            {
                float projectedWidth = currentLineWidth + spaceWidth + wordWidth;

                if (projectedWidth > availableWidth)
                {
                    // Emit current line
                    EmitShapedLine(result, currentLineWords, availableWidth, style.TextAlign, y,
                        shaper, fontFamily, fontSize, fontWeight, italic);
                    result.MaxWidth = Math.Max(result.MaxWidth, currentLineWidth);
                    lastEmittedWords = new List<string>(currentLineWords);
                    currentLineWords.Clear();
                    currentLineWidth = 0;
                    y += lineHeight;

                    // Check height limit
                    if (availableHeight > 0 && y + (lineHeight - ascent) > availableHeight)
                    {
                        if (style.TextOverflow == TextOverflow.Ellipsis
                            && result.Lines.Count > 0
                            && lastEmittedWords != null)
                        {
                            string lastLineText = string.Join(' ', lastEmittedWords);
                            string combined = lastLineText + " " + word;
                            var truncRun = TruncateWithEllipsis(combined, availableWidth,
                                shaper, fontFamily, fontSize, fontWeight, italic);
                            var lastLine = result.Lines[^1];
                            float truncX = AlignX(truncRun.TotalWidth, availableWidth, style.TextAlign);
                            result.Lines[^1] = new ShapedTextLine(truncRun, truncX, lastLine.Y, truncRun.TotalWidth);
                        }

                        result.WasTruncated = true;
                        break;
                    }
                }
                else
                {
                    currentLineWidth += spaceWidth;
                }
            }

            // Handle words wider than available width
            if (wordWidth > availableWidth && currentLineWords.Count == 0 && style.WordBreak == WordBreak.BreakAll)
            {
                BreakLongWord(result, word, availableWidth, style.TextAlign, ref y, lineHeight,
                    shaper, fontFamily, fontSize, fontWeight, italic);
                continue;
            }

            currentLineWords.Add(word);
            currentLineWidth += wordWidth;
        }

        // Emit remaining words
        if (currentLineWords.Count > 0 && !result.WasTruncated)
        {
            EmitShapedLine(result, currentLineWords, availableWidth, style.TextAlign, y,
                shaper, fontFamily, fontSize, fontWeight, italic);
            result.MaxWidth = Math.Max(result.MaxWidth, currentLineWidth);
        }

        result.TotalHeight = result.Lines.Count > 0
            ? result.Lines[^1].Y - ascent + lineHeight
            : 0;

        return result;
    }

    /// <summary>
    /// Measure shaped text dimensions without producing full layout (for Yoga measure callback).
    /// </summary>
    public static (float Width, float Height) Measure(
        string text, float maxWidth, ComputedStyle style, TextShaper shaper)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);

        var layout = Layout(text, maxWidth > 0 ? maxWidth : float.MaxValue, float.MaxValue, style, shaper);
        return (layout.MaxWidth, layout.TotalHeight);
    }

    private static void EmitShapedLine(
        ShapedTextLayoutResult result, List<string> words, float availableWidth,
        TextAlign align, float y,
        TextShaper shaper, string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        string lineText = string.Join(' ', words);
        var runs = shaper.ShapeMultiScript(lineText, fontFamily, fontSize, fontWeight, italic);
        float totalWidth = runs.Sum(r => r.TotalWidth);
        float x = AlignX(totalWidth, availableWidth, align);
        result.Lines.Add(new ShapedTextLine(runs, x, y, totalWidth));
    }

    private static float AlignX(float textWidth, float availableWidth, TextAlign align) => align switch
    {
        TextAlign.Center => (availableWidth - textWidth) / 2f,
        TextAlign.Right => availableWidth - textWidth,
        _ => 0
    };

    private static ShapedGlyphRun TruncateWithEllipsis(
        string text, float maxWidth,
        TextShaper shaper, string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        const string ellipsis = "\u2026";
        var ellipsisRun = shaper.Shape(ellipsis, fontFamily, fontSize, fontWeight, italic);
        float targetWidth = maxWidth - ellipsisRun.TotalWidth;

        if (targetWidth <= 0)
            return ellipsisRun;

        // Binary search for the longest prefix that fits
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            float w = shaper.Shape(text[..mid], fontFamily, fontSize, fontWeight, italic).TotalWidth;
            if (w <= targetWidth)
                lo = mid;
            else
                hi = mid - 1;
        }

        string truncated = text[..lo].TrimEnd() + ellipsis;
        return shaper.Shape(truncated, fontFamily, fontSize, fontWeight, italic);
    }

    private static void BreakLongWord(
        ShapedTextLayoutResult result, string word, float availableWidth,
        TextAlign align, ref float y, float lineHeight,
        TextShaper shaper, string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        int start = 0;
        while (start < word.Length)
        {
            int lo = start, hi = word.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                float w = shaper.Shape(word[start..mid], fontFamily, fontSize, fontWeight, italic).TotalWidth;
                if (w <= availableWidth)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            if (lo == start) lo = start + 1;

            string chunk = word[start..lo];
            var run = shaper.Shape(chunk, fontFamily, fontSize, fontWeight, italic);
            float x = AlignX(run.TotalWidth, availableWidth, align);
            result.Lines.Add(new ShapedTextLine(run, x, y, run.TotalWidth));
            result.MaxWidth = Math.Max(result.MaxWidth, run.TotalWidth);

            start = lo;
            if (start < word.Length) y += lineHeight;
        }
    }

    private static List<string> SplitWords(string text)
    {
        var words = new List<string>();
        int start = 0;
        bool inWord = false;

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                if (inWord)
                {
                    words.Add(text[start..i]);
                    inWord = false;
                }
            }
            else if (!inWord)
            {
                start = i;
                inWord = true;
            }
        }

        if (inWord)
            words.Add(text[start..]);

        return words;
    }
}
