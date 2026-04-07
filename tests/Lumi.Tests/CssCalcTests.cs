using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssCalcTests
{
    private static ComputedStyle Apply(string property, string value, float vpWidth = 1920, float vpHeight = 1080)
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.SetViewportContext(vpWidth, vpHeight);
        PropertyApplier.Apply(style, property, value);
        return style;
    }

    // ── Viewport units ──────────────────────────────────────────────

    [Fact]
    public void Vh_100_EqualsViewportHeight()
    {
        var style = Apply("height", "100vh", vpHeight: 1080);
        Assert.Equal(1080, style.Height);
    }

    [Fact]
    public void Vw_50_EqualsHalfViewportWidth()
    {
        var style = Apply("width", "50vw", vpWidth: 1920);
        Assert.Equal(960, style.Width);
    }

    [Fact]
    public void Vmin_UsesSmaller()
    {
        var style = Apply("width", "10vmin", vpWidth: 1920, vpHeight: 1080);
        Assert.Equal(108, style.Width); // 10% of 1080
    }

    [Fact]
    public void Vmax_UsesLarger()
    {
        var style = Apply("width", "10vmax", vpWidth: 1920, vpHeight: 1080);
        Assert.Equal(192, style.Width); // 10% of 1920
    }

    [Fact]
    public void Vh_FractionalValue()
    {
        var style = Apply("height", "33.33vh", vpHeight: 900);
        Assert.Equal(299.97f, style.Height, 0.01f);
    }

    // ── calc() expressions ──────────────────────────────────────────

    [Fact]
    public void Calc_SimpleAddition()
    {
        var style = Apply("width", "calc(100px + 50px)");
        Assert.Equal(150, style.Width);
    }

    [Fact]
    public void Calc_SimpleSubtraction()
    {
        var style = Apply("width", "calc(200px - 30px)");
        Assert.Equal(170, style.Width);
    }

    [Fact]
    public void Calc_Multiplication()
    {
        var style = Apply("width", "calc(10px * 5)");
        Assert.Equal(50, style.Width);
    }

    [Fact]
    public void Calc_Division()
    {
        var style = Apply("width", "calc(100px / 4)");
        Assert.Equal(25, style.Width);
    }

    [Fact]
    public void Calc_MixedUnits_PxAndVh()
    {
        var style = Apply("height", "calc(50vh + 20px)", vpHeight: 1080);
        Assert.Equal(560, style.Height); // 540 + 20
    }

    [Fact]
    public void Calc_MixedUnits_PxAndVw()
    {
        var style = Apply("width", "calc(100vw - 40px)", vpWidth: 1920);
        Assert.Equal(1880, style.Width);
    }

    [Fact]
    public void Calc_EmUnits()
    {
        var style = Apply("width", "calc(10em + 20px)");
        Assert.Equal(180, style.Width); // 10*16 + 20
    }

    [Fact]
    public void Calc_RemUnits()
    {
        var style = Apply("width", "calc(5rem + 10px)");
        Assert.Equal(90, style.Width); // 5*16 + 10
    }

    [Fact]
    public void Calc_Nested_Parens()
    {
        var style = Apply("width", "calc((100px + 50px) * 2)");
        Assert.Equal(300, style.Width);
    }

    [Fact]
    public void Calc_OperatorPrecedence()
    {
        // * has higher precedence than +
        var style = Apply("width", "calc(10px + 5px * 3)");
        Assert.Equal(25, style.Width);
    }

    [Fact]
    public void Calc_InvalidExpression_ReturnsZero()
    {
        var style = Apply("width", "calc(abc)");
        Assert.Equal(0, style.Width); // calc fallback is 0 for invalid
    }

    // ── Internal evaluator ──────────────────────────────────────────

    [Fact]
    public void CalcExpression_PureNumbers()
    {
        var result = CalcExpression.Evaluate("10 + 20", 16, 1920, 1080, 0);
        Assert.Equal(30, result);
    }

    [Fact]
    public void CalcExpression_VhVw()
    {
        var result = CalcExpression.Evaluate("50vh + 50vw", 16, 1920, 1080, 0);
        Assert.Equal(1500, result); // 540 + 960
    }
}
