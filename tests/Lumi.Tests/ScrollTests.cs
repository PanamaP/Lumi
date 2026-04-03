using Lumi.Core;

namespace Lumi.Tests;

public class ScrollTests
{
    private static BoxElement CreateScrollableElement()
    {
        var el = new BoxElement("div");
        el.LayoutBox = new LayoutBox(0, 0, 200, 300);
        el.ScrollWidth = 500;
        el.ScrollHeight = 800;
        return el;
    }

    [Fact]
    public void ScrollTo_SetsScrollTopAndScrollLeft()
    {
        var el = CreateScrollableElement();
        el.ScrollTo(50, 100);

        Assert.Equal(50, el.ScrollLeft);
        Assert.Equal(100, el.ScrollTop);
    }

    [Fact]
    public void ScrollBy_AddsToCurrentScrollPosition()
    {
        var el = CreateScrollableElement();
        el.ScrollTo(10, 20);
        el.ScrollBy(5, 10);

        Assert.Equal(15, el.ScrollLeft);
        Assert.Equal(30, el.ScrollTop);
    }

    [Fact]
    public void ScrollTo_ClampsToZeroMinimum()
    {
        var el = CreateScrollableElement();
        el.ScrollTo(-100, -200);

        Assert.Equal(0, el.ScrollLeft);
        Assert.Equal(0, el.ScrollTop);
    }

    [Fact]
    public void ScrollTo_ClampsToMax()
    {
        var el = CreateScrollableElement();
        // Max scroll: ScrollWidth - Width = 500 - 200 = 300
        // Max scroll: ScrollHeight - Height = 800 - 300 = 500
        el.ScrollTo(9999, 9999);

        Assert.Equal(300, el.ScrollLeft);
        Assert.Equal(500, el.ScrollTop);
    }

    [Fact]
    public void ScrollBy_WithNegativeDelta_ScrollsUp()
    {
        var el = CreateScrollableElement();
        el.ScrollTo(100, 200);
        el.ScrollBy(-30, -50);

        Assert.Equal(70, el.ScrollLeft);
        Assert.Equal(150, el.ScrollTop);
    }
}
