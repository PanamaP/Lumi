using Lumi.Tests.Helpers;

namespace Lumi.Tests.Integration;

[Collection("Integration")]
public class LayoutRegressionTests
{
    private static void AssertApprox(float expected, float actual, string message, float delta = 1f)
    {
        Assert.True(Math.Abs(actual - expected) < delta,
            $"{message}: expected {expected}, got {actual}");
    }

    [Fact]
    public void FlexRow_EqualGrow_DividesWidthEvenly()
    {
        const string html = """
            <div id="row">
                <div id="c1" class="grow"></div>
                <div id="c2" class="grow"></div>
                <div id="c3" class="grow"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; width: 600px; }
            .grow { flex-grow: 1; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css, 600, 400);

        var c1 = p.GetLayoutOf("c1");
        var c2 = p.GetLayoutOf("c2");
        var c3 = p.GetLayoutOf("c3");

        AssertApprox(200, c1.Width, "c1 width");
        AssertApprox(200, c2.Width, "c2 width");
        AssertApprox(200, c3.Width, "c3 width");
        AssertApprox(0, c1.X, "c1 X");
        AssertApprox(200, c2.X, "c2 X");
        AssertApprox(400, c3.X, "c3 X");
    }

    [Fact]
    public void FlexRow_FixedPlusGrow_GrowFillsRemaining()
    {
        const string html = """
            <div id="row">
                <div id="fixed"></div>
                <div id="grow"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; width: 500px; }
            #fixed { width: 100px; }
            #grow { flex-grow: 1; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css, 500, 400);

        var fixedBox = p.GetLayoutOf("fixed");
        var growBox = p.GetLayoutOf("grow");

        AssertApprox(100, fixedBox.Width, "fixed width");
        AssertApprox(400, growBox.Width, "grow width");
        AssertApprox(100, growBox.X, "grow X");
    }

    [Fact]
    public void FlexColumn_ChildrenStackVertically()
    {
        const string html = """
            <div id="col" style="display:flex; flex-direction:column;">
                <div id="a" style="height:100px;"></div>
                <div id="b" style="height:100px;"></div>
                <div id="c" style="height:100px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 400, 600);

        var a = p.GetLayoutOf("a");
        var b = p.GetLayoutOf("b");
        var c = p.GetLayoutOf("c");

        AssertApprox(100, a.Height, "a height");
        AssertApprox(100, b.Height, "b height");
        AssertApprox(100, c.Height, "c height");
        AssertApprox(0, a.Y, "a Y");
        AssertApprox(100, b.Y, "b Y");
        AssertApprox(200, c.Y, "c Y");
    }

    [Fact]
    public void FlexWrap_WrapsOverflowingChildren()
    {
        const string html = """
            <div id="wrap" style="display:flex; flex-direction:row; flex-wrap:wrap;">
                <div id="w1" style="width:200px; height:50px;"></div>
                <div id="w2" style="width:200px; height:50px;"></div>
                <div id="w3" style="width:200px; height:50px;"></div>
                <div id="w4" style="width:200px; height:50px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 600, 400);

        var w1 = p.GetLayoutOf("w1");
        var w2 = p.GetLayoutOf("w2");
        var w3 = p.GetLayoutOf("w3");
        var w4 = p.GetLayoutOf("w4");

        // First row: w1, w2, w3 (3 × 200 = 600 fits)
        AssertApprox(0, w1.Y, "w1 Y (first row)");
        AssertApprox(0, w2.Y, "w2 Y (first row)");
        AssertApprox(0, w3.Y, "w3 Y (first row)");

        // Second row: w4
        AssertApprox(50, w4.Y, "w4 Y (second row)");
        AssertApprox(0, w4.X, "w4 X (second row start)");
    }

    [Fact]
    public void JustifyContent_Center_CentersChild()
    {
        const string html = """
            <div id="parent">
                <div id="child"></div>
            </div>
            """;
        const string css = """
            #parent { display: flex; flex-direction: row; justify-content: center; width: 400px; }
            #child { width: 100px; height: 50px; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css, 400, 200);

        var child = p.GetLayoutOf("child");

        AssertApprox(150, child.X, "child X centered");
        AssertApprox(100, child.Width, "child width preserved");
    }

    [Fact]
    public void AlignItems_Center_CentersOnCrossAxis()
    {
        const string html = """
            <div id="parent">
                <div id="child"></div>
            </div>
            """;
        const string css = """
            #parent { display: flex; flex-direction: row; width: 400px; height: 200px; }
            #child { width: 50px; height: 50px; align-self: center; }
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, css, 400, 200);

        var child = p.GetLayoutOf("child");

        AssertApprox(75, child.Y, "child Y centered on cross axis");
        AssertApprox(50, child.Height, "child height preserved");
    }

    [Fact]
    public void Margin_OffsetsChildPosition()
    {
        const string html = """
            <div id="parent" style="display:flex;">
                <div id="child" style="width:60px; height:60px; margin:20px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 400, 300);

        var child = p.GetLayoutOf("child");

        AssertApprox(20, child.X, "child X offset by margin");
        AssertApprox(20, child.Y, "child Y offset by margin");
    }

    [Fact]
    public void Padding_OffsetsFirstChild()
    {
        const string html = """
            <div id="parent" style="display:flex; padding:15px;">
                <div id="child" style="width:50px; height:50px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 400, 300);

        var child = p.GetLayoutOf("child");

        AssertApprox(15, child.X, "child X offset by parent padding");
        AssertApprox(15, child.Y, "child Y offset by parent padding");
    }

    [Fact]
    public void NestedLayout_GrandchildAbsolutePosition()
    {
        const string html = """
            <div id="outer" style="display:flex; padding:10px;">
                <div id="inner" style="display:flex; padding:5px; margin:10px;">
                    <div id="leaf" style="width:20px; height:20px;"></div>
                </div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 400, 300);

        var outer = p.GetLayoutOf("outer");
        var inner = p.GetLayoutOf("inner");
        var leaf = p.GetLayoutOf("leaf");

        // inner starts at outer padding (10) + inner margin (10) = 20
        AssertApprox(20, inner.X, "inner X");
        AssertApprox(20, inner.Y, "inner Y");

        // leaf starts at inner.X (20) + inner padding (5) = 25
        AssertApprox(25, leaf.X, "leaf absolute X");
        AssertApprox(25, leaf.Y, "leaf absolute Y");
    }

    [Fact]
    public void MinWidth_EnforcedWhenContainerSmaller()
    {
        const string html = """
            <div id="parent" style="display:flex; width:50px;">
                <div id="child" style="min-width:100px; height:30px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 800, 600);

        var child = p.GetLayoutOf("child");

        Assert.True(child.Width >= 100,
            $"min-width not enforced: expected >= 100, got {child.Width}");
    }

    [Fact]
    public void DisplayNone_ProducesZeroSize()
    {
        const string html = """
            <div id="parent" style="display:flex;">
                <div id="visible" style="width:100px; height:100px;"></div>
                <div id="hidden" style="display:none; width:200px; height:200px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 800, 600);

        var hidden = p.GetLayoutOf("hidden");

        AssertApprox(0, hidden.Width, "hidden width");
        AssertApprox(0, hidden.Height, "hidden height");
    }

    [Fact]
    public void PositionAbsolute_OffsetsFromParent()
    {
        const string html = """
            <div id="parent" style="position:relative; width:300px; height:300px;">
                <div id="abs" style="position:absolute; top:10px; left:10px; width:50px; height:50px;"></div>
            </div>
            """;

        using var p = HeadlessPipeline.StyleAndLayout(html, "", 800, 600);

        var abs = p.GetLayoutOf("abs");

        AssertApprox(10, abs.X, "absolute left");
        AssertApprox(10, abs.Y, "absolute top");
        AssertApprox(50, abs.Width, "absolute width");
        AssertApprox(50, abs.Height, "absolute height");
    }
}
