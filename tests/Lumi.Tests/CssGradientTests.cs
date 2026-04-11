using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssGradientTests
{
    private static ComputedStyle ApplyProperty(string property, string value)
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, property, value);
        return style;
    }

    // ── Linear gradient parsing ──────────────────────────────────────

    [Fact]
    public void LinearGradient_TwoStops_DefaultAngle()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(red, blue)");
        Assert.NotNull(g);
        Assert.Equal(GradientType.Linear, g.Type);
        Assert.Equal(180, g.Angle);
        Assert.Equal(2, g.Stops.Count);
        Assert.Equal(new Color(255, 0, 0, 255), g.Stops[0].Color);
        Assert.Equal(0f, g.Stops[0].Position);
        Assert.Equal(new Color(0, 0, 255, 255), g.Stops[1].Color);
        Assert.Equal(1f, g.Stops[1].Position);
    }

    [Fact]
    public void LinearGradient_ToRight()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(to right, #ff0000, #0000ff)");
        Assert.NotNull(g);
        Assert.Equal(90, g.Angle);
        Assert.Equal(2, g.Stops.Count);
        Assert.Equal(new Color(255, 0, 0, 255), g.Stops[0].Color);
        Assert.Equal(new Color(0, 0, 255, 255), g.Stops[1].Color);
    }

    [Fact]
    public void LinearGradient_ExplicitAngle()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(45deg, red 0%, blue 100%)");
        Assert.NotNull(g);
        Assert.Equal(45, g.Angle);
        Assert.Equal(2, g.Stops.Count);
        Assert.Equal(0f, g.Stops[0].Position);
        Assert.Equal(1f, g.Stops[1].Position);
    }

    [Fact]
    public void LinearGradient_ThreeStops_MiddlePosition()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(red, yellow 50%, blue)");
        Assert.NotNull(g);
        Assert.Equal(3, g.Stops.Count);
        Assert.Equal(new Color(255, 0, 0, 255), g.Stops[0].Color);
        Assert.Equal(0f, g.Stops[0].Position);
        Assert.Equal(new Color(255, 255, 0, 255), g.Stops[1].Color);
        Assert.Equal(0.5f, g.Stops[1].Position);
        Assert.Equal(new Color(0, 0, 255, 255), g.Stops[2].Color);
        Assert.Equal(1f, g.Stops[2].Position);
    }

    // ── Direction keywords ───────────────────────────────────────────

    [Theory]
    [InlineData("to top", 0)]
    [InlineData("to right", 90)]
    [InlineData("to bottom", 180)]
    [InlineData("to left", 270)]
    [InlineData("to top right", 45)]
    [InlineData("to top left", 315)]
    [InlineData("to bottom right", 135)]
    [InlineData("to bottom left", 225)]
    public void LinearGradient_DirectionKeywords(string direction, float expectedAngle)
    {
        var g = PropertyApplier.ParseGradient($"linear-gradient({direction}, red, blue)");
        Assert.NotNull(g);
        Assert.Equal(expectedAngle, g.Angle);
    }

    // ── Radial gradient parsing ──────────────────────────────────────

    [Fact]
    public void RadialGradient_TwoStops()
    {
        var g = PropertyApplier.ParseGradient("radial-gradient(red, blue)");
        Assert.NotNull(g);
        Assert.Equal(GradientType.Radial, g.Type);
        Assert.Equal(2, g.Stops.Count);
        Assert.Equal(new Color(255, 0, 0, 255), g.Stops[0].Color);
        Assert.Equal(0f, g.Stops[0].Position);
        Assert.Equal(new Color(0, 0, 255, 255), g.Stops[1].Color);
        Assert.Equal(1f, g.Stops[1].Position);
    }

    [Fact]
    public void RadialGradient_CircleKeyword()
    {
        var g = PropertyApplier.ParseGradient("radial-gradient(circle, red, blue)");
        Assert.NotNull(g);
        Assert.Equal(GradientType.Radial, g.Type);
        Assert.Equal(2, g.Stops.Count);
    }

    // ── Hex color stops ──────────────────────────────────────────────

    [Fact]
    public void LinearGradient_HexColors()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(#ff0000, #00ff00, #0000ff)");
        Assert.NotNull(g);
        Assert.Equal(3, g.Stops.Count);
        Assert.Equal(new Color(255, 0, 0, 255), g.Stops[0].Color);
        Assert.Equal(new Color(0, 255, 0, 255), g.Stops[1].Color);
        Assert.Equal(new Color(0, 0, 255, 255), g.Stops[2].Color);
        Assert.Equal(0f, g.Stops[0].Position, 0.001f);
        Assert.Equal(0.5f, g.Stops[1].Position, 0.001f);
        Assert.Equal(1f, g.Stops[2].Position, 0.001f);
    }

    // ── Integration: gradient applied via CSS property ────────────────

    [Fact]
    public void BackgroundImage_LinearGradient_SetsGradient()
    {
        var style = ApplyProperty("background-image", "linear-gradient(to right, red, blue)");
        Assert.NotNull(style.BackgroundGradient);
        Assert.Equal(GradientType.Linear, style.BackgroundGradient.Type);
        Assert.Equal(90, style.BackgroundGradient.Angle);
    }

    [Fact]
    public void Background_LinearGradient_SetsGradient()
    {
        var style = ApplyProperty("background", "linear-gradient(45deg, #ff0000, #0000ff)");
        Assert.NotNull(style.BackgroundGradient);
        Assert.Equal(45, style.BackgroundGradient.Angle);
        Assert.Equal(2, style.BackgroundGradient.Stops.Count);
    }

    [Fact]
    public void Background_RadialGradient_SetsGradient()
    {
        var style = ApplyProperty("background", "radial-gradient(red, blue)");
        Assert.NotNull(style.BackgroundGradient);
        Assert.Equal(GradientType.Radial, style.BackgroundGradient.Type);
    }

    [Fact]
    public void Background_None_ClearsGradient()
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, "background", "linear-gradient(red, blue)");
        Assert.NotNull(style.BackgroundGradient);

        PropertyApplier.Apply(style, "background", "none");
        Assert.Null(style.BackgroundGradient);
    }

    [Fact]
    public void Reset_ClearsGradient()
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, "background", "linear-gradient(red, blue)");
        Assert.NotNull(style.BackgroundGradient);

        style.Reset();
        Assert.Null(style.BackgroundGradient);
    }

    // ── RGB color stops ──────────────────────────────────────────────

    [Fact]
    public void LinearGradient_RgbColorStops()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(rgb(255, 0, 0), rgb(0, 0, 255))");
        Assert.NotNull(g);
        Assert.Equal(2, g.Stops.Count);
        Assert.Equal(new Color(255, 0, 0, 255), g.Stops[0].Color);
        Assert.Equal(new Color(0, 0, 255, 255), g.Stops[1].Color);
    }

    [Fact]
    public void LinearGradient_ToBottomRight()
    {
        var g = PropertyApplier.ParseGradient("linear-gradient(to bottom right, red, blue)");
        Assert.NotNull(g);
        Assert.Equal(135, g.Angle);
    }

    [Fact]
    public void ParseGradient_InvalidInput_ReturnsNull()
    {
        Assert.Null(PropertyApplier.ParseGradient("red"));
        Assert.Null(PropertyApplier.ParseGradient("url(image.png)"));
    }
}
