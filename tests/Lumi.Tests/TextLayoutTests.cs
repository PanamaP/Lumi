using Lumi.Core;
using Lumi.Rendering;

namespace Lumi.Tests;

public class TextLayoutTests
{
    private static ComputedStyle DefaultStyle() => new()
    {
        FontFamily = "sans-serif",
        FontSize = 16,
        FontWeight = 400,
        LineHeight = 1.2f,
        TextAlign = TextAlign.Left,
        WhiteSpace = WhiteSpace.Normal,
        TextOverflow = TextOverflow.Clip,
        WordBreak = WordBreak.Normal,
    };

    // --- TextLayout.Layout ---

    [Fact]
    public void Layout_ShortText_ProducesSingleLine()
    {
        var style = DefaultStyle();
        var result = TextLayout.Layout("Hi", 500, 500, style);

        Assert.Single(result.Lines);
        Assert.Equal("Hi", result.Lines[0].Text);
    }

    [Fact]
    public void Layout_LongText_WrapsToMultipleLines()
    {
        var style = DefaultStyle();
        var longText = "The quick brown fox jumps over the lazy dog and keeps on running far far away";
        var result = TextLayout.Layout(longText, 120, 1000, style);

        Assert.True(result.Lines.Count > 1, $"Expected multiple lines, got {result.Lines.Count}");
    }

    [Fact]
    public void Layout_TextAlignCenter_LinesHaveCorrectXOffset()
    {
        var style = DefaultStyle();
        style.TextAlign = TextAlign.Center;
        var result = TextLayout.Layout("Hi", 500, 500, style);

        Assert.Single(result.Lines);
        // Centered line should have X > 0 (offset to center within 500px)
        Assert.True(result.Lines[0].X > 0, $"Center-aligned X should be > 0, got {result.Lines[0].X}");
    }

    [Fact]
    public void Layout_TextAlignRight_LinesHaveCorrectXOffset()
    {
        var style = DefaultStyle();
        style.TextAlign = TextAlign.Right;
        var result = TextLayout.Layout("Hi", 500, 500, style);

        Assert.Single(result.Lines);
        // Right-aligned line should have X near (500 - lineWidth)
        Assert.True(result.Lines[0].X > 0, $"Right-aligned X should be > 0, got {result.Lines[0].X}");
        // Right-aligned X should be greater than center-aligned X for the same text
        var centerStyle = DefaultStyle();
        centerStyle.TextAlign = TextAlign.Center;
        var centerResult = TextLayout.Layout("Hi", 500, 500, centerStyle);
        Assert.True(result.Lines[0].X > centerResult.Lines[0].X,
            "Right-aligned X should be greater than center-aligned X");
    }

    [Fact]
    public void Layout_WhiteSpaceNoWrap_ProducesSingleLineEvenIfLong()
    {
        var style = DefaultStyle();
        style.WhiteSpace = WhiteSpace.NoWrap;
        var longText = "This is a very long line of text that would normally wrap to multiple lines";
        var result = TextLayout.Layout(longText, 100, 500, style);

        Assert.Single(result.Lines);
    }

    [Fact]
    public void Layout_TextOverflowEllipsisWithNoWrap_TruncatesWithEllipsis()
    {
        var style = DefaultStyle();
        style.WhiteSpace = WhiteSpace.NoWrap;
        style.TextOverflow = TextOverflow.Ellipsis;
        var longText = "This is a very long line of text that should be truncated with an ellipsis";
        var result = TextLayout.Layout(longText, 100, 500, style);

        Assert.Single(result.Lines);
        Assert.True(result.WasTruncated, "Should be truncated");
        Assert.EndsWith("\u2026", result.Lines[0].Text);
    }

    // --- TextLayout.Measure ---

    [Fact]
    public void Measure_SingleLine_ReturnsCorrectDimensions()
    {
        var style = DefaultStyle();
        var (width, height) = TextLayout.Measure("Hello", 500, style);

        Assert.True(width > 0, "Width should be positive for non-empty text");
        Assert.True(height > 0, "Height should be positive for non-empty text");
    }

    [Fact]
    public void Measure_MultiLine_ReturnsCorrectHeight()
    {
        var style = DefaultStyle();
        var shortText = "Hi";
        var longText = "The quick brown fox jumps over the lazy dog and keeps on running far away";

        var (_, shortHeight) = TextLayout.Measure(shortText, 120, style);
        var (_, longHeight) = TextLayout.Measure(longText, 120, style);

        Assert.True(longHeight > shortHeight,
            $"Multi-line height ({longHeight}) should be greater than single-line height ({shortHeight})");
    }

    // --- TextMeasurer ---

    [Fact]
    public void TextMeasurer_MeasureWidth_ReturnsPositiveForNonEmptyText()
    {
        var measurer = new TextMeasurer();
        float width = measurer.MeasureWidth("Hello World", "sans-serif", 16, 400, false);

        Assert.True(width > 0, $"MeasureWidth should return positive, got {width}");
    }

    [Fact]
    public void TextMeasurer_GetLineHeight_ReturnsPositive()
    {
        var measurer = new TextMeasurer();
        float lineHeight = measurer.GetLineHeight("sans-serif", 16, 400, false, 1.2f);

        Assert.True(lineHeight > 0, $"GetLineHeight should return positive, got {lineHeight}");
    }

    [Fact]
    public void TextMeasurer_CreateFont_ReturnsValidSKFont()
    {
        var font = TextMeasurer.CreateFont("sans-serif", 16, 400, false);

        Assert.NotNull(font);
        Assert.True(font.Size > 0, "Font size should be positive");
        font.Dispose();
    }
}
