using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssShorthandTests
{
    private static ComputedStyle ApplyProperty(string property, string value)
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, property, value);
        return style;
    }

    // ── border shorthand ─────────────────────────────────────────────

    [Fact]
    public void Border_WidthStyleColor()
    {
        var style = ApplyProperty("border", "2px solid red");
        Assert.Equal(2, style.BorderWidth.Top);
        Assert.Equal(2, style.BorderWidth.Right);
        Assert.Equal(2, style.BorderWidth.Bottom);
        Assert.Equal(2, style.BorderWidth.Left);
        Assert.Equal(BorderStyle.Solid, style.BorderStyle);
        Assert.Equal(new Color(255, 0, 0, 255), style.BorderColor);
    }

    [Fact]
    public void Border_WidthAndStyle()
    {
        var style = ApplyProperty("border", "1px dashed");
        Assert.Equal(1, style.BorderWidth.Top);
        Assert.Equal(BorderStyle.Dashed, style.BorderStyle);
    }

    [Fact]
    public void Border_None()
    {
        // First set a border, then clear it
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, "border", "2px solid red");
        PropertyApplier.Apply(style, "border", "none");
        Assert.Equal(0, style.BorderWidth.Top);
        Assert.Equal(BorderStyle.None, style.BorderStyle);
    }

    [Fact]
    public void Border_StyleWidthColor_AnyOrder()
    {
        // CSS allows any order for border shorthand components
        var style = ApplyProperty("border", "solid 3px #00FF00");
        Assert.Equal(3, style.BorderWidth.Top);
        Assert.Equal(BorderStyle.Solid, style.BorderStyle);
        Assert.Equal(new Color(0, 255, 0, 255), style.BorderColor);
    }

    [Fact]
    public void Border_WithRgbaColor()
    {
        var style = ApplyProperty("border", "1px solid rgba(255, 0, 0, 0.5)");
        Assert.Equal(1, style.BorderWidth.Top);
        Assert.Equal(BorderStyle.Solid, style.BorderStyle);
        Assert.Equal(new Color(255, 0, 0, 127), style.BorderColor);
    }

    // ── flex shorthand ───────────────────────────────────────────────

    [Fact]
    public void Flex_SingleNumber()
    {
        var style = ApplyProperty("flex", "1");
        Assert.Equal(1, style.FlexGrow);
        Assert.Equal(1, style.FlexShrink);
        Assert.Equal(0, style.FlexBasis);
    }

    [Fact]
    public void Flex_TwoNumbers()
    {
        var style = ApplyProperty("flex", "2 3");
        Assert.Equal(2, style.FlexGrow);
        Assert.Equal(3, style.FlexShrink);
        Assert.Equal(0, style.FlexBasis);
    }

    [Fact]
    public void Flex_ThreeValues()
    {
        var style = ApplyProperty("flex", "1 0 100px");
        Assert.Equal(1, style.FlexGrow);
        Assert.Equal(0, style.FlexShrink);
        Assert.Equal(100, style.FlexBasis);
    }

    [Fact]
    public void Flex_None()
    {
        var style = ApplyProperty("flex", "none");
        Assert.Equal(0, style.FlexGrow);
        Assert.Equal(0, style.FlexShrink);
        Assert.True(float.IsNaN(style.FlexBasis)); // auto
    }

    [Fact]
    public void Flex_Auto()
    {
        var style = ApplyProperty("flex", "auto");
        Assert.Equal(1, style.FlexGrow);
        Assert.Equal(1, style.FlexShrink);
        Assert.True(float.IsNaN(style.FlexBasis)); // auto
    }

    [Fact]
    public void Flex_Initial()
    {
        var style = ApplyProperty("flex", "initial");
        Assert.Equal(0, style.FlexGrow);
        Assert.Equal(1, style.FlexShrink);
        Assert.True(float.IsNaN(style.FlexBasis)); // auto
    }

    [Fact]
    public void Flex_GrowAndBasis()
    {
        var style = ApplyProperty("flex", "1 50%");
        Assert.Equal(1, style.FlexGrow);
        Assert.Equal(1, style.FlexShrink); // default
        Assert.Equal(-50, style.FlexBasis); // negative = percentage
    }

    // ── background shorthand ────────────────────────────────────────

    [Fact]
    public void Background_Color()
    {
        var style = ApplyProperty("background", "#336699");
        Assert.Equal(new Color(0x33, 0x66, 0x99, 255), style.BackgroundColor);
    }

    [Fact]
    public void Background_NamedColor()
    {
        var style = ApplyProperty("background", "red");
        Assert.Equal(new Color(255, 0, 0, 255), style.BackgroundColor);
    }

    [Fact]
    public void Background_None()
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, "background", "red");
        PropertyApplier.Apply(style, "background", "none");
        Assert.Equal(Color.Transparent, style.BackgroundColor);
        Assert.Null(style.BackgroundImage);
    }

    [Fact]
    public void Background_WithUrl()
    {
        var style = ApplyProperty("background", "url('bg.png')");
        Assert.Equal("bg.png", style.BackgroundImage);
    }

    // ── font shorthand ──────────────────────────────────────────────

    [Fact]
    public void Font_SizeAndFamily()
    {
        var style = ApplyProperty("font", "16px Arial");
        Assert.Equal(16, style.FontSize);
        Assert.Equal("Arial", style.FontFamily);
    }

    [Fact]
    public void Font_StyleWeightSizeFamily()
    {
        var style = ApplyProperty("font", "italic bold 20px Helvetica");
        Assert.Equal(FontStyle.Italic, style.FontStyle);
        Assert.Equal(700, style.FontWeight);
        Assert.Equal(20, style.FontSize);
        Assert.Equal("Helvetica", style.FontFamily);
    }

    [Fact]
    public void Font_SizeWithLineHeight()
    {
        var style = ApplyProperty("font", "14px/1.5 Segoe UI");
        Assert.Equal(14, style.FontSize);
        Assert.Equal(1.5f, style.LineHeight);
        Assert.Equal("Segoe UI", style.FontFamily);
    }

    [Fact]
    public void Font_WeightSizeFamily()
    {
        var style = ApplyProperty("font", "300 12px Consolas");
        Assert.Equal(300, style.FontWeight);
        Assert.Equal(12, style.FontSize);
        Assert.Equal("Consolas", style.FontFamily);
    }

    [Fact]
    public void Font_NumericWeight()
    {
        var style = ApplyProperty("font", "600 18px/1.4 Inter");
        Assert.Equal(600, style.FontWeight);
        Assert.Equal(18, style.FontSize);
        Assert.Equal(1.4f, style.LineHeight);
        Assert.Equal("Inter", style.FontFamily);
    }
}
