using Lumi.Core;
using Lumi.Layout;
using Lumi.Styling;

namespace Lumi.Tests;

public class LayoutTests
{
    [Fact]
    public void StackingContext_ReturnsCorrectPaintOrder()
    {
        var root = new BoxElement("div");
        var child1 = new BoxElement("div");
        var child2 = new BoxElement("div");
        child2.ComputedStyle.ZIndex = 10;
        var child3 = new BoxElement("div");
        child3.ComputedStyle.ZIndex = -1;

        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var order = StackingContext.GetPaintOrder(root);

        // root first, then children sorted by z-index: child3 (-1), child1 (0), child2 (10)
        Assert.Equal(4, order.Count);
        Assert.Equal(root, order[0]);
        Assert.Equal(child3, order[1]);
        Assert.Equal(child1, order[2]);
        Assert.Equal(child2, order[3]);
    }

    [Fact]
    public void StackingContext_SkipsDisplayNone()
    {
        var root = new BoxElement("div");
        var visible = new BoxElement("div");
        var hidden = new BoxElement("div");
        hidden.ComputedStyle.Display = DisplayMode.None;

        root.AddChild(visible);
        root.AddChild(hidden);

        var order = StackingContext.GetPaintOrder(root);

        Assert.Equal(2, order.Count);
        Assert.DoesNotContain(hidden, order);
    }

    [Fact]
    public void YogaLayout_BasicFlexRow()
    {
        // Two children in a flex row, each taking 50%
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Flex;
        root.ComputedStyle.FlexDirection = FlexDirection.Row;

        var left = new BoxElement("div");
        left.ComputedStyle.FlexGrow = 1;
        root.AddChild(left);

        var right = new BoxElement("div");
        right.ComputedStyle.FlexGrow = 1;
        root.AddChild(right);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 800, 600);

        Assert.Equal(0, root.LayoutBox.X);
        Assert.Equal(0, root.LayoutBox.Y);
        Assert.Equal(800, root.LayoutBox.Width);
        Assert.Equal(600, root.LayoutBox.Height);

        Assert.Equal(0, left.LayoutBox.X);
        Assert.Equal(400, left.LayoutBox.Width);

        Assert.Equal(400, right.LayoutBox.X);
        Assert.Equal(400, right.LayoutBox.Width);
    }

    [Fact]
    public void YogaLayout_FlexColumn_WithFixedHeight()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Flex;
        root.ComputedStyle.FlexDirection = FlexDirection.Column;

        var header = new BoxElement("header");
        header.ComputedStyle.Height = 60;
        root.AddChild(header);

        var content = new BoxElement("main");
        content.ComputedStyle.FlexGrow = 1;
        root.AddChild(content);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 1024, 768);

        Assert.Equal(60, header.LayoutBox.Height);
        Assert.Equal(1024, header.LayoutBox.Width);

        Assert.Equal(60, content.LayoutBox.Y);
        Assert.Equal(708, content.LayoutBox.Height); // 768 - 60
    }

    [Fact]
    public void YogaLayout_Padding_AffectsChildPosition()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Padding = new EdgeValues(20);

        var child = new BoxElement("div");
        child.ComputedStyle.Width = 100;
        child.ComputedStyle.Height = 50;
        root.AddChild(child);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 400, 300);

        // Child should be offset by padding
        Assert.Equal(20, child.LayoutBox.X);
        Assert.Equal(20, child.LayoutBox.Y);
    }

    [Fact]
    public void YogaLayout_DisplayNone_ZeroSize()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Flex;

        var visible = new BoxElement("div");
        visible.ComputedStyle.Width = 100;
        visible.ComputedStyle.Height = 100;
        root.AddChild(visible);

        var hidden = new BoxElement("div");
        hidden.ComputedStyle.Display = DisplayMode.None;
        hidden.ComputedStyle.Width = 200;
        hidden.ComputedStyle.Height = 200;
        root.AddChild(hidden);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(root, 800, 600);

        Assert.Equal(100, visible.LayoutBox.Width);
        Assert.Equal(0, hidden.LayoutBox.Width);
    }

    [Fact]
    public void StyleResolver_IntegratesWithParser()
    {
        var html = HtmlTemplateParser.Parse(@"
            <div class=""container"">
                <div class=""header"">Header</div>
                <div class=""content"">Content</div>
            </div>
        ");

        var css = CssParser.Parse(@"
            .container { display: flex; flex-direction: column; }
            .header { height: 60px; background-color: #333333; }
            .content { flex-grow: 1; }
        ");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(html);

        var container = html.Children[0];
        Assert.Equal(DisplayMode.Flex, container.ComputedStyle.Display);
        Assert.Equal(FlexDirection.Column, container.ComputedStyle.FlexDirection);

        var header = container.Children[0];
        Assert.Equal(60, header.ComputedStyle.Height);

        var content = container.Children[1];
        Assert.Equal(1f, content.ComputedStyle.FlexGrow);
    }
}
