using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssColorTests
{
    private static Color Parse(string value) => PropertyApplier.ParseColor(value);

    // ── HSL colors ──────────────────────────────────────────────────

    [Fact]
    public void Hsl_Red()
    {
        var c = Parse("hsl(0, 100%, 50%)");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void Hsl_Green()
    {
        var c = Parse("hsl(120, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Hsl_Blue()
    {
        var c = Parse("hsl(240, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(255, c.B);
    }

    [Fact]
    public void Hsl_White()
    {
        var c = Parse("hsl(0, 0%, 100%)");
        Assert.Equal(255, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(255, c.B);
    }

    [Fact]
    public void Hsl_Black()
    {
        var c = Parse("hsl(0, 0%, 0%)");
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Hsl_MidGray()
    {
        var c = Parse("hsl(0, 0%, 50%)");
        Assert.Equal(128, c.R);
        Assert.Equal(128, c.G);
        Assert.Equal(128, c.B);
    }

    [Fact]
    public void Hsla_WithAlpha()
    {
        var c = Parse("hsla(0, 100%, 50%, 0.5)");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(127, c.A);
    }

    [Fact]
    public void Hsl_DegreeSymbol()
    {
        var c = Parse("hsl(120°, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Hsl_Cyan_180()
    {
        var c = Parse("hsl(180, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(255, c.B);
    }

    // ── Named colors ────────────────────────────────────────────────

    [Theory]
    [InlineData("red", 255, 0, 0)]
    [InlineData("green", 0, 128, 0)]
    [InlineData("blue", 0, 0, 255)]
    [InlineData("white", 255, 255, 255)]
    [InlineData("black", 0, 0, 0)]
    [InlineData("cyan", 0, 255, 255)]
    [InlineData("magenta", 255, 0, 255)]
    [InlineData("yellow", 255, 255, 0)]
    [InlineData("orange", 255, 165, 0)]
    [InlineData("purple", 128, 0, 128)]
    public void NamedColor_BasicColors(string name, byte r, byte g, byte b)
    {
        var c = Parse(name);
        Assert.Equal(r, c.R);
        Assert.Equal(g, c.G);
        Assert.Equal(b, c.B);
        Assert.Equal(255, c.A);
    }

    [Theory]
    [InlineData("cornflowerblue", 100, 149, 237)]
    [InlineData("rebeccapurple", 102, 51, 153)]
    [InlineData("tomato", 255, 99, 71)]
    [InlineData("coral", 255, 127, 80)]
    [InlineData("deepskyblue", 0, 191, 255)]
    [InlineData("hotpink", 255, 105, 180)]
    [InlineData("limegreen", 50, 205, 50)]
    [InlineData("slategray", 112, 128, 144)]
    [InlineData("steelblue", 70, 130, 180)]
    [InlineData("wheat", 245, 222, 179)]
    public void NamedColor_ExtendedColors(string name, byte r, byte g, byte b)
    {
        var c = Parse(name);
        Assert.Equal(r, c.R);
        Assert.Equal(g, c.G);
        Assert.Equal(b, c.B);
    }

    [Fact]
    public void NamedColor_Transparent()
    {
        var c = Parse("transparent");
        Assert.Equal(0, c.A);
    }

    [Fact]
    public void NamedColor_CaseInsensitive()
    {
        var c1 = Parse("CornflowerBlue");
        var c2 = Parse("cornflowerblue");
        Assert.Equal(c1, c2);
    }

    // ── Hex variants ────────────────────────────────────────────────

    [Fact]
    public void Hex_ThreeDigit()
    {
        var c = Parse("#F00");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void Hex_FourDigit_RGBA()
    {
        var c = Parse("#F008");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(136, c.A); // 0x8 * 17 = 136
    }

    [Fact]
    public void Hex_SixDigit()
    {
        var c = Parse("#336699");
        Assert.Equal(0x33, c.R);
        Assert.Equal(0x66, c.G);
        Assert.Equal(0x99, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void Hex_EightDigit_RRGGBBAA()
    {
        var c = Parse("#FF000080");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(128, c.A);
    }

    // ── RGB/RGBA ────────────────────────────────────────────────────

    [Fact]
    public void Rgb_Standard()
    {
        var c = Parse("rgb(100, 200, 50)");
        Assert.Equal(100, c.R);
        Assert.Equal(200, c.G);
        Assert.Equal(50, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void Rgba_WithAlpha()
    {
        var c = Parse("rgba(255, 128, 0, 0.5)");
        Assert.Equal(255, c.R);
        Assert.Equal(128, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(127, c.A);
    }
}
