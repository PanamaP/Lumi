namespace Lumi.Tests;

using Lumi.Text;
using Lumi.Rendering;
using SkiaSharp;

/// <summary>
/// Integration tests verifying HarfBuzz text shaping is wired into the rendering pipeline.
/// </summary>
public class HarfBuzzIntegrationTests : IDisposable
{
    private readonly TextShaper _shaper = new();

    public HarfBuzzIntegrationTests()
    {
        // Ensure clean state for each test
        TextRenderingOptions.Reset();
        FontManager.Clear();
    }

    public void Dispose()
    {
        _shaper.Dispose();
        TextRenderingOptions.Reset();
        FontManager.Clear();
    }

    // -----------------------------------------------------------------------
    // TextShaper produces valid glyph runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Shape_SimpleLatinText_ProducesGlyphIds()
    {
        var run = _shaper.Shape("Hello", "Arial", 16f, 400, false);

        Assert.Equal(5, run.GlyphIds.Length);
        Assert.All(run.GlyphIds, id => Assert.NotEqual((ushort)0, id));
    }

    [Fact]
    public void Shape_SimpleText_ProducesPositions()
    {
        var run = _shaper.Shape("Test", "Arial", 14f, 400, false);

        // Positions array has 2 entries per glyph (x, y)
        Assert.Equal(run.GlyphIds.Length * 2, run.Positions.Length);
        // First glyph starts at x=0
        Assert.Equal(0f, run.Positions[0]);
    }

    [Fact]
    public void Shape_SimpleText_ProducesAdvances()
    {
        var run = _shaper.Shape("AB", "Arial", 16f, 400, false);

        Assert.Equal(2, run.Advances.Length);
        Assert.All(run.Advances, adv => Assert.True(adv > 0, "Each glyph should have a positive advance"));
    }

    [Fact]
    public void Shape_SimpleText_TotalWidthMatchesSumOfAdvances()
    {
        var run = _shaper.Shape("Hello World", "Arial", 16f, 400, false);

        float sumAdvances = run.Advances.Sum();
        Assert.Equal(sumAdvances, run.TotalWidth, precision: 2);
    }

    [Fact]
    public void Shape_EmptyString_ReturnsEmptyRun()
    {
        var run = _shaper.Shape("", "Arial", 16f, 400, false);

        Assert.Empty(run.GlyphIds);
        Assert.Empty(run.Positions);
        Assert.Empty(run.Advances);
        Assert.Equal(0f, run.TotalWidth);
    }

    [Fact]
    public void Shape_PreservesFontParameters()
    {
        var run = _shaper.Shape("X", "Arial", 24f, 400, false);

        Assert.Equal("Arial", run.FontFamily);
        Assert.Equal(24f, run.FontSize);
        Assert.Equal(400, run.FontWeight);
        Assert.False(run.Italic);
    }

    [Fact]
    public void Shape_LargerFontSize_ProducesLargerAdvances()
    {
        var runSmall = _shaper.Shape("A", "Arial", 12f, 400, false);
        var runLarge = _shaper.Shape("A", "Arial", 48f, 400, false);

        Assert.True(runLarge.TotalWidth > runSmall.TotalWidth,
            "Larger font size should produce larger total width");
    }

    // -----------------------------------------------------------------------
    // Shaped metrics match SkiaSharp direct measurement within tolerance
    // -----------------------------------------------------------------------

    [Fact]
    public void ShapedWidth_MatchesSkiaMeasurement_WithinTolerance()
    {
        const string text = "Hello World";
        const string family = "Arial";
        const float fontSize = 16f;
        const int weight = 400;

        var run = _shaper.Shape(text, family, fontSize, weight, false);

        using var font = TextMeasurer.CreateFont(family, fontSize, weight, false);
        using var paint = new SKPaint();
        float skiaWidth = font.MeasureText(text, paint);

        // HarfBuzz and Skia use different shaping; allow 15% tolerance
        float tolerance = skiaWidth * 0.15f;
        Assert.InRange(run.TotalWidth, skiaWidth - tolerance, skiaWidth + tolerance);
    }

    [Fact]
    public void ShapedWidth_MatchesSkia_ForMultipleStrings()
    {
        string[] testStrings = ["abc", "The quick brown fox", "12345", "Mixed 123 text!"];

        foreach (var text in testStrings)
        {
            var run = _shaper.Shape(text, "Arial", 14f, 400, false);

            using var font = TextMeasurer.CreateFont("Arial", 14f, 400, false);
            using var paint = new SKPaint();
            float skiaWidth = font.MeasureText(text, paint);

            float tolerance = skiaWidth * 0.15f;
            Assert.InRange(run.TotalWidth, skiaWidth - tolerance, skiaWidth + tolerance);
        }
    }

    // -----------------------------------------------------------------------
    // FontManager integration in TextShaper
    // -----------------------------------------------------------------------

    [Fact]
    public void TextShaper_UsesCustomTypefaceResolver_WhenSet()
    {
        bool resolverCalled = false;
        TextShaper.CustomTypefaceResolver = (family, weight, italic) =>
        {
            if (family == "TestCustomFont")
            {
                resolverCalled = true;
                // Return default typeface as a stand-in for a custom font
                return SKTypeface.Default;
            }
            return null;
        };

        try
        {
            using var shaper = new TextShaper();
            var run = shaper.Shape("Test", "TestCustomFont", 16f, 400, false);

            Assert.True(resolverCalled, "CustomTypefaceResolver should have been called for TestCustomFont");
            Assert.True(run.GlyphIds.Length > 0, "Should produce glyph IDs even with custom resolver");
        }
        finally
        {
            TextShaper.CustomTypefaceResolver = null;
        }
    }

    [Fact]
    public void TextShaper_FallsBackToSystem_WhenResolverReturnsNull()
    {
        TextShaper.CustomTypefaceResolver = (_, _, _) => null;

        try
        {
            using var shaper = new TextShaper();
            var run = shaper.Shape("Hello", "Arial", 16f, 400, false);

            // Should still work using system font fallback
            Assert.Equal(5, run.GlyphIds.Length);
            Assert.True(run.TotalWidth > 0);
        }
        finally
        {
            TextShaper.CustomTypefaceResolver = null;
        }
    }

    [Fact]
    public void TextRenderingOptions_WiresFontManager_IntoTextShaper()
    {
        // Register a custom typeface via FontManager
        FontManager.RegisterTypeface("TestWiredFont", SKTypeface.Default, 400, false);

        // Enabling UseHarfBuzz should wire FontManager as the resolver
        TextRenderingOptions.UseHarfBuzz = true;

        Assert.NotNull(TextShaper.CustomTypefaceResolver);

        // The resolver should find the registered font
        var typeface = TextShaper.CustomTypefaceResolver!("TestWiredFont", 400, false);
        Assert.NotNull(typeface);

        // Unregistered fonts should return null from the resolver
        var unknown = TextShaper.CustomTypefaceResolver!("NonExistentFont", 400, false);
        Assert.Null(unknown);
    }

    // -----------------------------------------------------------------------
    // UseHarfBuzz flag toggles rendering path
    // -----------------------------------------------------------------------

    [Fact]
    public void UseHarfBuzz_DefaultsTrue()
    {
        Assert.True(TextRenderingOptions.UseHarfBuzz);
    }

    [Fact]
    public void UseHarfBuzz_CanBeEnabled()
    {
        TextRenderingOptions.UseHarfBuzz = true;

        Assert.True(TextRenderingOptions.UseHarfBuzz);
    }

    [Fact]
    public void UseHarfBuzz_GetShaper_ReturnsSameInstance()
    {
        TextRenderingOptions.UseHarfBuzz = true;

        var shaper1 = TextRenderingOptions.GetShaper();
        var shaper2 = TextRenderingOptions.GetShaper();

        Assert.Same(shaper1, shaper2);
    }

    [Fact]
    public void UseHarfBuzz_Reset_ClearsState()
    {
        TextRenderingOptions.UseHarfBuzz = true;
        Assert.True(TextRenderingOptions.UseHarfBuzz);
        Assert.NotNull(TextShaper.CustomTypefaceResolver);

        TextRenderingOptions.Reset();

        // Reset restores UseHarfBuzz to true (the default) but clears the resolver
        Assert.True(TextRenderingOptions.UseHarfBuzz);
        Assert.Null(TextShaper.CustomTypefaceResolver);
    }

    [Fact]
    public void TextMeasurer_MeasureWidth_UsesHarfBuzz_WhenEnabled()
    {
        var measurer = new TextMeasurer();
        const string text = "Hello";
        const string family = "Arial";
        const float fontSize = 16f;

        // Measure without HarfBuzz
        TextRenderingOptions.UseHarfBuzz = false;
        float skiaWidth = measurer.MeasureWidth(text, family, fontSize, 400, false);

        // Enable HarfBuzz and measure again
        TextRenderingOptions.UseHarfBuzz = true;
        float hbWidth = measurer.MeasureWidth(text, family, fontSize, 400, false);

        // Both should produce reasonable, non-zero widths
        Assert.True(skiaWidth > 0);
        Assert.True(hbWidth > 0);

        // They should be reasonably close (within 15%)
        float tolerance = skiaWidth * 0.15f;
        Assert.InRange(hbWidth, skiaWidth - tolerance, skiaWidth + tolerance);
    }

    [Fact]
    public void TextLayout_Measure_UsesHarfBuzz_WhenEnabled()
    {
        var style = new Lumi.Core.ComputedStyle
        {
            FontFamily = "Arial",
            FontSize = 16f,
            FontWeight = 400,
            LineHeight = 1.2f
        };

        // Measure without HarfBuzz
        TextRenderingOptions.UseHarfBuzz = false;
        var (skiaW, skiaH) = TextLayout.Measure("Hello World", 500f, style);

        // Enable HarfBuzz
        TextRenderingOptions.UseHarfBuzz = true;
        var (hbW, hbH) = TextLayout.Measure("Hello World", 500f, style);

        // Both should produce reasonable dimensions
        Assert.True(skiaW > 0);
        Assert.True(hbW > 0);
        Assert.True(skiaH > 0);
        Assert.True(hbH > 0);

        // Widths should be reasonably close
        float tolerance = skiaW * 0.15f;
        Assert.InRange(hbW, skiaW - tolerance, skiaW + tolerance);
    }
}
