using Lumi.Core;
using Lumi.Input;

namespace Lumi.Tests;

public class HitTestTests
{
    [Fact]
    public void HitTest_ReturnsElement_WhenPointInside()
    {
        var element = new BoxElement("div");
        element.LayoutBox = new LayoutBox(10, 10, 100, 100);

        var result = HitTester.HitTest(element, 50, 50);

        Assert.Equal(element, result);
    }

    [Fact]
    public void HitTest_ReturnsNull_WhenPointOutside()
    {
        var element = new BoxElement("div");
        element.LayoutBox = new LayoutBox(10, 10, 100, 100);

        var result = HitTester.HitTest(element, 200, 200);

        Assert.Null(result);
    }

    [Fact]
    public void HitTest_ReturnsDeepestChild()
    {
        var parent = new BoxElement("div");
        parent.LayoutBox = new LayoutBox(0, 0, 200, 200);

        var child = new BoxElement("div");
        child.LayoutBox = new LayoutBox(10, 10, 50, 50);
        parent.AddChild(child);

        var result = HitTester.HitTest(parent, 30, 30);

        Assert.Equal(child, result);
    }

    [Fact]
    public void HitTest_SkipsHiddenElements()
    {
        var parent = new BoxElement("div");
        parent.LayoutBox = new LayoutBox(0, 0, 200, 200);

        var child = new BoxElement("div");
        child.LayoutBox = new LayoutBox(0, 0, 200, 200);
        child.ComputedStyle.Display = DisplayMode.None;
        parent.AddChild(child);

        var result = HitTester.HitTest(parent, 50, 50);

        Assert.Equal(parent, result);
    }

    [Fact]
    public void HitTest_SkipsPointerEventsNone()
    {
        var parent = new BoxElement("div");
        parent.LayoutBox = new LayoutBox(0, 0, 200, 200);

        var child = new BoxElement("div");
        child.LayoutBox = new LayoutBox(0, 0, 200, 200);
        child.ComputedStyle.PointerEvents = false;
        parent.AddChild(child);

        var result = HitTester.HitTest(parent, 50, 50);

        Assert.Equal(parent, result);
    }

    [Fact]
    public void HitTest_TopmostChildWins()
    {
        var parent = new BoxElement("div");
        parent.LayoutBox = new LayoutBox(0, 0, 200, 200);

        var childA = new BoxElement("div") { Id = "a" };
        childA.LayoutBox = new LayoutBox(0, 0, 100, 100);
        parent.AddChild(childA);

        var childB = new BoxElement("div") { Id = "b" };
        childB.LayoutBox = new LayoutBox(0, 0, 100, 100);
        parent.AddChild(childB);

        // childB is painted after childA, so it's "on top"
        var result = HitTester.HitTest(parent, 50, 50);

        Assert.Equal(childB, result);
    }

    [Fact]
    public void HitTest_ScrolledChild_HitsAtVisualPosition()
    {
        // Scroll container at (0,100) with height 300, ScrollTop=50
        var scrollContainer = new BoxElement("div");
        scrollContainer.LayoutBox = new LayoutBox(0, 100, 400, 300);
        scrollContainer.ComputedStyle.Overflow = Overflow.Scroll;
        scrollContainer.ScrollTop = 50;
        scrollContainer.ScrollHeight = 600;

        // Button at layout Y=150 (50px from container top)
        // With ScrollTop=50, it visually appears at Y=100 (container top)
        var button = new BoxElement("button") { Id = "btn" };
        button.LayoutBox = new LayoutBox(10, 150, 80, 30);
        scrollContainer.AddChild(button);

        // Click at the VISUAL position (Y=105, within scroll container bounds)
        var result = HitTester.HitTest(scrollContainer, 15, 105);

        Assert.NotNull(result);
        Assert.Equal("btn", result!.Id);
    }

    [Fact]
    public void HitTest_ScrolledChild_MissesAtOriginalLayoutPosition()
    {
        // Scroll container at (0,100) with height 300, ScrollTop=200
        var scrollContainer = new BoxElement("div");
        scrollContainer.LayoutBox = new LayoutBox(0, 100, 400, 300);
        scrollContainer.ComputedStyle.Overflow = Overflow.Scroll;
        scrollContainer.ScrollTop = 200;
        scrollContainer.ScrollHeight = 800;

        // Button at layout Y=150 — with ScrollTop=200, it's scrolled out of view (above container)
        var button = new BoxElement("button") { Id = "btn" };
        button.LayoutBox = new LayoutBox(10, 150, 80, 30);
        scrollContainer.AddChild(button);

        // Click at the original layout position (Y=155) — should NOT hit the button
        // because it's been scrolled out of view
        var result = HitTester.HitTest(scrollContainer, 15, 155);

        // Should hit the scroll container itself, not the button
        Assert.Equal(scrollContainer, result);
    }

    [Fact]
    public void HitTest_ClipsChildrenOutsideScrollBounds()
    {
        // Scroll container at (0,100) with height 300
        var scrollContainer = new BoxElement("div");
        scrollContainer.LayoutBox = new LayoutBox(0, 100, 400, 300);
        scrollContainer.ComputedStyle.Overflow = Overflow.Scroll;
        scrollContainer.ScrollTop = 0;
        scrollContainer.ScrollHeight = 600;

        var child = new BoxElement("div") { Id = "child" };
        child.LayoutBox = new LayoutBox(10, 110, 80, 30);
        scrollContainer.AddChild(child);

        // Click outside the scroll container's bounds (Y=50, above container at Y=100)
        var result = HitTester.HitTest(scrollContainer, 15, 50);

        // Should NOT hit the child even though the child's LayoutBox might match after adjustment
        Assert.Null(result);
    }

    [Fact]
    public void HitTest_NestedScrollContainers()
    {
        // Outer scroll container
        var outer = new BoxElement("div") { Id = "outer" };
        outer.LayoutBox = new LayoutBox(0, 0, 400, 400);
        outer.ComputedStyle.Overflow = Overflow.Scroll;
        outer.ScrollTop = 30;
        outer.ScrollHeight = 800;

        // Inner scroll container at layout Y=100 (visually at Y=70 due to outer scroll)
        var inner = new BoxElement("div") { Id = "inner" };
        inner.LayoutBox = new LayoutBox(10, 100, 380, 200);
        inner.ComputedStyle.Overflow = Overflow.Scroll;
        inner.ScrollTop = 20;
        inner.ScrollHeight = 500;

        outer.AddChild(inner);

        // Button inside inner container at layout Y=150
        var button = new BoxElement("button") { Id = "btn" };
        button.LayoutBox = new LayoutBox(20, 150, 80, 30);
        inner.AddChild(button);

        // Visual position: outer scrolls by 30, inner scrolls by 20
        // Button layout Y=150, inner layout Y=100
        // Button relative to inner: 150-100=50, with inner scroll -20 → visual 30 from inner top
        // Inner visual Y: 100 - 30 (outer scroll) = 70
        // Button visual Y: 70 + 30 = 100
        var result = HitTester.HitTest(outer, 25, 100);

        Assert.NotNull(result);
        Assert.Equal("btn", result!.Id);
    }

    [Fact]
    public void HitTest_NoScrollOffset_WorksAsNormal()
    {
        // Scroll container with ScrollTop=0 behaves normally
        var scrollContainer = new BoxElement("div");
        scrollContainer.LayoutBox = new LayoutBox(0, 0, 400, 300);
        scrollContainer.ComputedStyle.Overflow = Overflow.Scroll;
        scrollContainer.ScrollTop = 0;
        scrollContainer.ScrollHeight = 300;

        var child = new BoxElement("div") { Id = "child" };
        child.LayoutBox = new LayoutBox(10, 10, 80, 30);
        scrollContainer.AddChild(child);

        var result = HitTester.HitTest(scrollContainer, 15, 15);

        Assert.NotNull(result);
        Assert.Equal("child", result!.Id);
    }
}
