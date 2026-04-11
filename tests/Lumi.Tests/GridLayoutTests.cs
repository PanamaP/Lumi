using Lumi.Core;
using Lumi.Layout;
using Lumi.Styling;

namespace Lumi.Tests;

public class GridLayoutTests
{
    [Fact]
    public void ThreeColumnGrid_FrUnits_DividesEvenly()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Grid;
        root.ComputedStyle.GridTemplateColumns = "1fr 1fr 1fr";

        var c1 = new BoxElement("div");
        var c2 = new BoxElement("div");
        var c3 = new BoxElement("div");
        root.AddChild(c1);
        root.AddChild(c2);
        root.AddChild(c3);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 900, 600);

        Assert.Equal(0, c1.LayoutBox.X);
        Assert.Equal(300, c1.LayoutBox.Width, 1f);
        Assert.Equal(300, c2.LayoutBox.X, 1f);
        Assert.Equal(300, c2.LayoutBox.Width, 1f);
        Assert.Equal(600, c3.LayoutBox.X, 1f);
        Assert.Equal(300, c3.LayoutBox.Width, 1f);
    }

    [Fact]
    public void FixedAndFrMixedColumns()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Grid;
        root.ComputedStyle.GridTemplateColumns = "100px 1fr 2fr";

        var c1 = new BoxElement("div");
        var c2 = new BoxElement("div");
        var c3 = new BoxElement("div");
        root.AddChild(c1);
        root.AddChild(c2);
        root.AddChild(c3);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 400, 300);

        // 400 - 100px fixed = 300 remaining, split 1fr+2fr = 100+200
        Assert.Equal(100, c1.LayoutBox.Width, 1f);
        Assert.Equal(100, c2.LayoutBox.Width, 1f);
        Assert.Equal(200, c3.LayoutBox.Width, 1f);

        Assert.Equal(0, c1.LayoutBox.X, 1f);
        Assert.Equal(100, c2.LayoutBox.X, 1f);
        Assert.Equal(200, c3.LayoutBox.X, 1f);
    }

    [Fact]
    public void AutoRowsGeneration_WhenChildrenExceedExplicitGrid()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Grid;
        root.ComputedStyle.GridTemplateColumns = "1fr 1fr";
        // Only 1 row explicitly defined, but 4 children → 2 rows needed
        root.ComputedStyle.GridTemplateRows = "100px";

        for (int i = 0; i < 4; i++)
            root.AddChild(new BoxElement("div"));

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 600, 400);

        // Row 0: explicitly 100px
        Assert.Equal(0, root.Children[0].LayoutBox.Y);
        Assert.Equal(100, root.Children[0].LayoutBox.Height, 1f);
        Assert.Equal(0, root.Children[1].LayoutBox.Y);
        Assert.Equal(100, root.Children[1].LayoutBox.Height, 1f);

        // Row 1: auto-generated, children at y = 100
        Assert.Equal(100, root.Children[2].LayoutBox.Y, 1f);
        Assert.Equal(100, root.Children[3].LayoutBox.Y, 1f);
    }

    [Fact]
    public void GridGap_AppliesSpacingBetweenCells()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Grid;
        root.ComputedStyle.GridTemplateColumns = "1fr 1fr";
        root.ComputedStyle.GridGap = 20;

        var c1 = new BoxElement("div");
        var c2 = new BoxElement("div");
        var c3 = new BoxElement("div");
        var c4 = new BoxElement("div");
        root.AddChild(c1);
        root.AddChild(c2);
        root.AddChild(c3);
        root.AddChild(c4);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 420, 300);

        // 420 - 20 gap = 400, split 2 fr → 200 each
        Assert.Equal(0, c1.LayoutBox.X);
        Assert.Equal(200, c1.LayoutBox.Width, 1f);
        Assert.Equal(220, c2.LayoutBox.X, 1f); // 200 + 20 gap
        Assert.Equal(200, c2.LayoutBox.Width, 1f);

        // Row 1 should be offset by row height + gap
        float row0Height = c1.LayoutBox.Height;
        Assert.Equal(row0Height + 20, c3.LayoutBox.Y, 1f);
    }

    [Fact]
    public void RepeatSyntax_ExpandsCorrectly()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Grid;
        root.ComputedStyle.GridTemplateColumns = "repeat(3, 1fr)";

        var c1 = new BoxElement("div");
        var c2 = new BoxElement("div");
        var c3 = new BoxElement("div");
        root.AddChild(c1);
        root.AddChild(c2);
        root.AddChild(c3);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 600, 400);

        Assert.Equal(200, c1.LayoutBox.Width, 1f);
        Assert.Equal(200, c2.LayoutBox.Width, 1f);
        Assert.Equal(200, c3.LayoutBox.Width, 1f);

        Assert.Equal(0, c1.LayoutBox.X, 1f);
        Assert.Equal(200, c2.LayoutBox.X, 1f);
        Assert.Equal(400, c3.LayoutBox.X, 1f);
    }

    [Fact]
    public void NestedGridInsideFlexContainer()
    {
        var flexRoot = new BoxElement("div");
        flexRoot.ComputedStyle.Display = DisplayMode.Flex;
        flexRoot.ComputedStyle.FlexDirection = FlexDirection.Row;

        // Left side: flex child taking half
        var leftPanel = new BoxElement("div");
        leftPanel.ComputedStyle.FlexGrow = 1;
        flexRoot.AddChild(leftPanel);

        // Right side: grid child taking half
        var gridPanel = new BoxElement("div");
        gridPanel.ComputedStyle.Display = DisplayMode.Grid;
        gridPanel.ComputedStyle.FlexGrow = 1;
        gridPanel.ComputedStyle.GridTemplateColumns = "1fr 1fr";

        var g1 = new BoxElement("div");
        var g2 = new BoxElement("div");
        gridPanel.AddChild(g1);
        gridPanel.AddChild(g2);
        flexRoot.AddChild(gridPanel);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(flexRoot, 800, 400);

        // Each flex child gets 400px
        Assert.Equal(400, leftPanel.LayoutBox.Width, 1f);
        Assert.Equal(400, gridPanel.LayoutBox.Width, 1f);

        // Grid children split the 400px grid panel evenly
        Assert.Equal(200, g1.LayoutBox.Width, 1f);
        Assert.Equal(200, g2.LayoutBox.Width, 1f);
    }

    [Fact]
    public void PropertyApplier_GridProperties_AreParsed()
    {
        var style = new ComputedStyle();

        PropertyApplier.Apply(style, "display", "grid");
        Assert.Equal(DisplayMode.Grid, style.Display);

        PropertyApplier.Apply(style, "grid-template-columns", "1fr 2fr 100px");
        Assert.Equal("1fr 2fr 100px", style.GridTemplateColumns);

        PropertyApplier.Apply(style, "grid-template-rows", "repeat(2, 50px)");
        Assert.Equal("repeat(2, 50px)", style.GridTemplateRows);

        PropertyApplier.Apply(style, "grid-gap", "10px");
        Assert.Equal(10, style.GridGap);
    }

    [Fact]
    public void Gap_FallsBackToGenericGap_WhenGridGapNotSet()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Grid;
        root.ComputedStyle.GridTemplateColumns = "1fr 1fr";
        root.ComputedStyle.Gap = 10;

        var c1 = new BoxElement("div");
        var c2 = new BoxElement("div");
        root.AddChild(c1);
        root.AddChild(c2);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 210, 100);

        // 210 - 10 gap = 200, split 2 fr → 100 each
        Assert.Equal(100, c1.LayoutBox.Width, 1f);
        Assert.Equal(110, c2.LayoutBox.X, 1f); // 100 + 10 gap
    }
}
