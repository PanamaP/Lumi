using Lumi.Core;
using Lumi.Tests.Helpers;
using SkiaSharp;

namespace Lumi.Tests.Integration;

/// <summary>
/// Full pipeline integration tests: HTML → CSS → Style → Layout → Render.
/// </summary>
[Collection("Integration")]
public class PipelineTests
{
    [Fact]
    public void SimpleDivWithBackground_RendersCorrectSizeAndColor()
    {
        const string html = """<div id="box" class="red"></div>""";
        const string css = ".red { width: 100px; height: 100px; background-color: red; }";

        using var p = HeadlessPipeline.Render(html, css, 800, 600);

        var layout = p.GetLayoutOf("box");
        Assert.Equal(100, layout.Width);
        Assert.Equal(100, layout.Height);
        Assert.True(p.PixelMatches(50, 50, new SKColor(255, 0, 0)),
            "Center pixel of 100×100 red box should be red");
    }

    [Fact]
    public void NestedFlexRow_ChildrenShareWidthEqually()
    {
        const string html = """
            <div id="row" class="row">
                <div id="c1" class="child"></div>
                <div id="c2" class="child"></div>
                <div id="c3" class="child"></div>
            </div>
            """;
        const string css = """
            .row { display: flex; flex-direction: row; width: 300px; height: 100px; }
            .child { flex-grow: 1; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var c1 = p.GetLayoutOf("c1");
        var c2 = p.GetLayoutOf("c2");
        var c3 = p.GetLayoutOf("c3");

        Assert.InRange(c1.Width, 99f, 101f);
        Assert.InRange(c2.Width, 99f, 101f);
        Assert.InRange(c3.Width, 99f, 101f);
    }

    [Fact]
    public void FlexColumnWithHeader_ContentFillsRemainingSpace()
    {
        const string html = """
            <div id="col" class="col">
                <div id="header" class="header"></div>
                <div id="content" class="content"></div>
            </div>
            """;
        const string css = """
            .col { display: flex; flex-direction: column; width: 400px; height: 600px; }
            .header { height: 60px; }
            .content { flex-grow: 1; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var header = p.GetLayoutOf("header");
        var content = p.GetLayoutOf("content");

        Assert.Equal(60, header.Height);
        Assert.InRange(content.Height, 539f, 541f);
    }

    [Fact]
    public void StyleInheritance_ChildInheritsParentColor()
    {
        const string html = """
            <div id="parent">
                <div id="child"></div>
            </div>
            """;
        const string css = "#parent { color: red; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var child = p.FindById("child");
        Assert.NotNull(child);
        Assert.Equal(255, child.ComputedStyle.Color.R);
        Assert.Equal(0, child.ComputedStyle.Color.G);
        Assert.Equal(0, child.ComputedStyle.Color.B);
    }

    [Fact]
    public void CssSpecificity_IdSelectorBeatsClassSelector()
    {
        const string html = """<div id="el" class="blue"></div>""";
        const string css = """
            .blue { background-color: blue; }
            #el { background-color: green; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var el = p.FindById("el");
        Assert.NotNull(el);
        // ID selector (#el → green) should override class (.blue → blue)
        Assert.Equal(0, el.ComputedStyle.BackgroundColor.R);
        Assert.True(el.ComputedStyle.BackgroundColor.G > 0,
            "Green channel should be non-zero (green won over blue)");
        Assert.Equal(0, el.ComputedStyle.BackgroundColor.B);
    }

    [Fact]
    public void InlineStyleOverride_WinsOverStylesheet()
    {
        const string html = """<div id="el" class="blue" style="background-color: green;"></div>""";
        const string css = ".blue { background-color: blue; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var el = p.FindById("el");
        Assert.NotNull(el);
        // Inline style (green) should beat stylesheet (blue)
        Assert.Equal(0, el.ComputedStyle.BackgroundColor.R);
        Assert.True(el.ComputedStyle.BackgroundColor.G > 0,
            "Green channel should be non-zero (inline green won over stylesheet blue)");
        Assert.Equal(0, el.ComputedStyle.BackgroundColor.B);
    }

    [Fact]
    public void MarginAndPadding_ChildOffsetCorrectly()
    {
        const string html = """
            <div id="outer" class="outer">
                <div id="inner" class="inner"></div>
            </div>
            """;
        const string css = """
            .outer { margin: 20px; padding: 10px; width: 200px; height: 200px; }
            .inner { width: 50px; height: 50px; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var outer = p.GetLayoutOf("outer");
        var inner = p.GetLayoutOf("inner");

        // Outer offset by its margin
        Assert.Equal(20, outer.X);
        Assert.Equal(20, outer.Y);
        // Inner offset by outer's margin + padding
        Assert.Equal(30, inner.X);
        Assert.Equal(30, inner.Y);
    }

    [Fact]
    public void TextAutoSizing_TextElementGetsNonZeroMeasuredWidth()
    {
        const string html = """
            <div id="container" class="container">
                <span id="txt">Hello World</span>
            </div>
            """;
        const string css = ".container { display: flex; width: 800px; height: 100px; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var txt = p.GetLayoutOf("txt");
        Assert.True(txt.Width > 0, "Text element should have non-zero measured width");
        Assert.True(txt.Height > 0, "Text element should have non-zero measured height");
    }

    [Fact]
    public void DisplayNone_ElementGetsZeroLayoutBox()
    {
        const string html = """<div id="hidden" class="hidden"></div>""";
        const string css = ".hidden { display: none; width: 200px; height: 200px; }";

        using var p = HeadlessPipeline.StyleAndLayout(html, css);

        var layout = p.GetLayoutOf("hidden");
        Assert.Equal(0, layout.Width);
        Assert.Equal(0, layout.Height);
    }

    [Fact]
    public void RerenderAfterChange_UpdatesRenderedPixels()
    {
        const string html = """<div id="box" class="box"></div>""";
        const string css = ".box { width: 100px; height: 100px; background-color: red; }";

        using var p = HeadlessPipeline.Render(html, css, 800, 600);

        // Initially red
        Assert.True(p.PixelMatches(50, 50, new SKColor(255, 0, 0)),
            "Should initially be red");

        // Change to blue via inline style and rerender
        var box = p.FindById("box");
        Assert.NotNull(box);
        box.InlineStyle = "width: 100px; height: 100px; background-color: blue;";
        p.Rerender();

        // Now should be blue
        Assert.True(p.PixelMatches(50, 50, new SKColor(0, 0, 255)),
            "After rerender should be blue");
    }
}
