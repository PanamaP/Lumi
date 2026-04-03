using Lumi.Core;
using Lumi.Tests.Helpers;

namespace Lumi.Tests.Integration;

[Collection("Integration")]
public class StyleRegressionTests
{
    [Fact]
    public void BackgroundColor_AppliedViaSelector()
    {
        const string html = """<div id="box" class="box">Hello</div>""";
        const string css = ".box { background-color: #FF0000; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("box")!.ComputedStyle;

        Assert.Equal(255, style.BackgroundColor.R);
        Assert.Equal(0, style.BackgroundColor.G);
        Assert.Equal(0, style.BackgroundColor.B);
        Assert.Equal(255, style.BackgroundColor.A);
    }

    [Fact]
    public void MultipleProperties_AppliedOnSameElement()
    {
        const string html = """<div id="box" class="styled">Hello</div>""";
        const string css = ".styled { color: #FF0000; background-color: #0000FF; font-size: 20px; opacity: 0.5; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("box")!.ComputedStyle;

        Assert.Equal(255, style.Color.R);
        Assert.Equal(0, style.Color.G);
        Assert.Equal(0, style.Color.B);
        Assert.Equal(0, style.BackgroundColor.R);
        Assert.Equal(0, style.BackgroundColor.G);
        Assert.Equal(255, style.BackgroundColor.B);
        Assert.Equal(20, style.FontSize);
        Assert.Equal(0.5f, style.Opacity);
    }

    [Fact]
    public void MultipleRules_CombineOnSameElement()
    {
        const string html = """<div id="box" class="a b">Hello</div>""";
        const string css = ".a { color: #FF0000; } .b { background-color: #00FF00; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("box")!.ComputedStyle;

        Assert.Equal(255, style.Color.R);
        Assert.Equal(0, style.Color.G);
        Assert.Equal(0, style.Color.B);
        Assert.Equal(0, style.BackgroundColor.R);
        Assert.Equal(255, style.BackgroundColor.G);
        Assert.Equal(0, style.BackgroundColor.B);
    }

    [Fact]
    public void ColorInheritance_ChildInheritsFromParent()
    {
        const string html = """<div id="parent" class="parent"><div id="child">Hello</div></div>""";
        const string css = ".parent { color: #0000FF; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("child")!.ComputedStyle;

        Assert.Equal(0, style.Color.R);
        Assert.Equal(0, style.Color.G);
        Assert.Equal(255, style.Color.B);
    }

    [Fact]
    public void FontInheritance_ChildInheritsFontSizeAndFamily()
    {
        const string html = """<div class="parent"><div id="child">Hello</div></div>""";
        const string css = ".parent { font-size: 24px; font-family: Arial; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("child")!.ComputedStyle;

        Assert.Equal(24, style.FontSize);
        Assert.Equal("Arial", style.FontFamily);
    }

    [Fact]
    public void Specificity_IdBeatsClass()
    {
        const string html = """<div class="box" id="box">Hello</div>""";
        const string css = ".box { color: #FF0000; } #box { color: #0000FF; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("box")!.ComputedStyle;

        Assert.Equal(0, style.Color.R);
        Assert.Equal(0, style.Color.G);
        Assert.Equal(255, style.Color.B);
    }

    [Fact]
    public void Specificity_ClassBeatsTag()
    {
        const string html = """<div class="box" id="target">Hello</div>""";
        const string css = "div { color: #FF0000; } .box { color: #00FF00; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("target")!.ComputedStyle;

        Assert.Equal(0, style.Color.R);
        Assert.Equal(255, style.Color.G);
        Assert.Equal(0, style.Color.B);
    }

    [Fact]
    public void Cascade_LaterStylesheetOverridesEarlier()
    {
        const string html = """<div class="box" id="target">Hello</div>""";
        const string css = ".box { color: #FF0000; } .box { color: #00FF00; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("target")!.ComputedStyle;

        Assert.Equal(0, style.Color.R);
        Assert.Equal(255, style.Color.G);
        Assert.Equal(0, style.Color.B);
    }

    [Fact]
    public void InlineStyle_OverridesStylesheet()
    {
        const string html = """<div class="box" id="target" style="color: #0000FF;">Hello</div>""";
        const string css = ".box { color: #FF0000; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var style = p.FindById("target")!.ComputedStyle;

        Assert.Equal(0, style.Color.R);
        Assert.Equal(0, style.Color.G);
        Assert.Equal(255, style.Color.B);
    }

    [Fact]
    public void BoxShadow_ParsesAllProperties()
    {
        const string html = """<div id="target">Hello</div>""";
        const string css = "#target { box-shadow: 2px 4px 8px 0px rgba(0,0,0,0.5); }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);
        var shadow = p.FindById("target")!.ComputedStyle.BoxShadow;

        Assert.Equal(2, shadow.OffsetX);
        Assert.Equal(4, shadow.OffsetY);
        Assert.Equal(8, shadow.BlurRadius);
        Assert.Equal(0, shadow.SpreadRadius);
        Assert.False(shadow.Inset);
        Assert.Equal(0, shadow.Color.R);
        Assert.Equal(0, shadow.Color.G);
        Assert.Equal(0, shadow.Color.B);
        Assert.True(shadow.Color.A > 100 && shadow.Color.A < 140); // ~128 for 0.5 alpha
    }
}
