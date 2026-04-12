namespace Lumi.Rendering;

using SkiaSharp;
using Lumi.Core;
using Lumi.Text;

/// <summary>
/// A single line of laid-out text with its position and dimensions.
/// </summary>
public readonly record struct TextLine(string Text, float X, float Y, float Width);

/// <summary>
/// Result of laying out text within a constrained width.
/// </summary>
public sealed class TextLayoutResult
{
    public List<TextLine> Lines { get; } = [];
    public float TotalHeight { get; set; }
    public float MaxWidth { get; set; }
    public bool WasTruncated { get; set; }
}

/// <summary>
/// Performs text layout: word wrapping, line breaking, and text alignment
/// within a constrained width.
/// </summary>
public static class TextLayout
{
    /// <summary>
    /// Lay out text within the given constraints, producing positioned lines.
    /// </summary>
    public static TextLayoutResult Layout(
        string text,
        float availableWidth,
        float availableHeight,
        ComputedStyle style)
    {
        var result = new TextLayoutResult();
        if (string.IsNullOrEmpty(text) || availableWidth <= 0)
            return result;

        using var font = TextMeasurer.CreateFont(
            style.FontFamily, style.FontSize, style.FontWeight,
            style.FontStyle == FontStyle.Italic);
        using var paint = new SKPaint();

        font.GetFontMetrics(out var metrics);
        float rawLineHeight = -metrics.Ascent + metrics.Descent + metrics.Leading;
        float lineHeight = rawLineHeight * style.LineHeight;
        float ascent = -metrics.Ascent;

        // Handle no-wrap mode
        if (style.WhiteSpace == WhiteSpace.NoWrap)
        {
            float textWidth = font.MeasureText(text, paint);
            string displayText = text;

            if (style.TextOverflow == TextOverflow.Ellipsis && textWidth > availableWidth)
            {
                displayText = TruncateWithEllipsis(text, availableWidth, font, paint);
                textWidth = Math.Min(textWidth, availableWidth);
                result.WasTruncated = true;
            }

            float x = AlignX(textWidth, availableWidth, style.TextAlign);
            result.Lines.Add(new TextLine(displayText, x, ascent, textWidth));
            result.TotalHeight = lineHeight;
            result.MaxWidth = textWidth;
            return result;
        }

        // Word wrap mode
        var words = SplitWords(text);
        float currentLineWidth = 0;
        var currentLineWords = new List<string>();
        float y = ascent;

        foreach (var word in words)
        {
            float wordWidth = font.MeasureText(word, paint);

            // Check if adding this word exceeds the available width
            if (currentLineWords.Count > 0)
            {
                float spaceWidth = font.MeasureText(" ", paint);
                float projectedWidth = currentLineWidth + spaceWidth + wordWidth;

                if (projectedWidth > availableWidth)
                {
                    // Emit current line
                    EmitLine(result, currentLineWords, currentLineWidth, availableWidth, style.TextAlign, y);
                    result.MaxWidth = Math.Max(result.MaxWidth, currentLineWidth);
                    currentLineWords.Clear();
                    currentLineWidth = 0;
                    y += lineHeight;

                    // Check if we've exceeded available height
                    if (availableHeight > 0 && y + (lineHeight - ascent) > availableHeight)
                    {
                        // Truncate with ellipsis on previous line if needed
                        if (style.TextOverflow == TextOverflow.Ellipsis && result.Lines.Count > 0)
                        {
                            var lastLine = result.Lines[^1];
                            string truncated = TruncateWithEllipsis(lastLine.Text + " " + word, availableWidth, font, paint);
                            float truncWidth = font.MeasureText(truncated, paint);
                            float truncX = AlignX(truncWidth, availableWidth, style.TextAlign);
                            result.Lines[^1] = new TextLine(truncated, truncX, lastLine.Y, truncWidth);
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

            // Handle words wider than available width (break-all)
            if (wordWidth > availableWidth && currentLineWords.Count == 0 && style.WordBreak == WordBreak.BreakAll)
            {
                BreakLongWord(result, word, availableWidth, style.TextAlign, ref y, lineHeight, ascent, font, paint);
                continue;
            }

            currentLineWords.Add(word);
            currentLineWidth += wordWidth;
        }

        // Emit remaining words as the last line
        if (currentLineWords.Count > 0 && !result.WasTruncated)
        {
            EmitLine(result, currentLineWords, currentLineWidth, availableWidth, style.TextAlign, y);
            result.MaxWidth = Math.Max(result.MaxWidth, currentLineWidth);
        }

        result.TotalHeight = result.Lines.Count > 0
            ? result.Lines[^1].Y - ascent + lineHeight
            : 0;

        return result;
    }

    /// <summary>
    /// Measure text dimensions without producing full layout (for Yoga measure callback).
    /// When <see cref="TextRenderingOptions.UseHarfBuzz"/> is enabled, delegates to
    /// <see cref="ShapedTextLayout.Measure"/> for more accurate glyph-based measurement.
    /// </summary>
    public static (float Width, float Height) Measure(string text, float maxWidth, ComputedStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);

        if (TextRenderingOptions.UseHarfBuzz)
        {
            var shaper = TextRenderingOptions.GetShaper();
            return ShapedTextLayout.Measure(text, maxWidth, style, shaper);
        }

        // Use a large available height since we just want to measure, not truncate
        var layout = Layout(text, maxWidth > 0 ? maxWidth : float.MaxValue, float.MaxValue, style);
        return (layout.MaxWidth, layout.TotalHeight);
    }

    private static void EmitLine(TextLayoutResult result, List<string> words, float lineWidth, float availableWidth, TextAlign align, float y)
    {
        string lineText = string.Join(' ', words);
        float x = AlignX(lineWidth, availableWidth, align);
        result.Lines.Add(new TextLine(lineText, x, y, lineWidth));
    }

    private static float AlignX(float textWidth, float availableWidth, TextAlign align) => align switch
    {
        TextAlign.Center => (availableWidth - textWidth) / 2f,
        TextAlign.Right => availableWidth - textWidth,
        _ => 0
    };

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
            else
            {
                if (!inWord)
                {
                    start = i;
                    inWord = true;
                }
            }
        }

        if (inWord)
            words.Add(text[start..]);

        return words;
    }

    private static string TruncateWithEllipsis(string text, float maxWidth, SKFont font, SKPaint paint)
    {
        const string ellipsis = "…";
        float ellipsisWidth = font.MeasureText(ellipsis, paint);
        float targetWidth = maxWidth - ellipsisWidth;

        if (targetWidth <= 0)
            return ellipsis;

        // Binary search for the longest prefix that fits
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (font.MeasureText(text.AsSpan(0, mid), paint) <= targetWidth)
                lo = mid;
            else
                hi = mid - 1;
        }

        return text[..lo].TrimEnd() + ellipsis;
    }

    private static void BreakLongWord(TextLayoutResult result, string word, float availableWidth, TextAlign align, ref float y, float lineHeight, float ascent, SKFont font, SKPaint paint)
    {
        int start = 0;
        while (start < word.Length)
        {
            // Find how many characters fit on this line
            int lo = start, hi = word.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (font.MeasureText(word.AsSpan(start, mid - start), paint) <= availableWidth)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            // At least one character per line to avoid infinite loop
            if (lo == start) lo = start + 1;

            string chunk = word[start..lo];
            float chunkWidth = font.MeasureText(chunk, paint);
            float x = AlignX(chunkWidth, availableWidth, align);
            result.Lines.Add(new TextLine(chunk, x, y, chunkWidth));
            result.MaxWidth = Math.Max(result.MaxWidth, chunkWidth);

            start = lo;
            if (start < word.Length) y += lineHeight;
        }
    }
}
