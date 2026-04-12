using Lumi.Core;
using Lumi.Core.Components;
using Lumi.Tests.Helpers;
using SkiaSharp;

namespace Lumi.Tests.Integration;

[Collection("Integration")]
public class ComponentRegressionTests
{
    private const string HostHtml = "<div id='host' class='host'></div>";
    private const string HostCss = ".host { width: 400px; height: 400px; display: flex; flex-direction: column; }";

    private static void SimulateClick(Element target)
    {
        var e = new RoutedMouseEvent("click") { Button = MouseButton.Left };
        EventDispatcher.Dispatch(e, target);
    }

    /// <summary>
    /// Re-layout and repaint without running the style resolver, so that
    /// component styles set directly on ComputedStyle are preserved.
    /// </summary>
    private static void RelayoutAndPaint(HeadlessPipeline p)
    {
        p.Rerender(); // includes style resolution + layout + paint
    }

    [Fact]
    public void Button_RendersContent()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var btn = new LumiButton { Text = "Test" };
        host.AddChild(btn.Root);
        p.Rerender();

        Assert.True(p.HasContentInRegion(0, 0, 400, 100));
    }

    [Fact]
    public void Checkbox_RendersContent()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var cb = new LumiCheckbox { Label = "Accept terms" };
        host.AddChild(cb.Root);
        p.Rerender();

        Assert.True(p.HasContentInRegion(0, 0, 400, 100));
    }

    [Fact]
    public void Slider_RendersContent()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var slider = new LumiSlider { Min = 0, Max = 100, Value = 50 };
        host.AddChild(slider.Root);
        RelayoutAndPaint(p);

        Assert.True(p.HasContentInRegion(0, 0, 400, 50));
    }

    [Fact]
    public void Button_ClickAfterLayout_FiresOnClick()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        bool fired = false;
        var btn = new LumiButton { Text = "Click me" };
        btn.OnClick = () => fired = true;
        host.AddChild(btn.Root);
        p.Rerender();

        SimulateClick(btn.Root);

        Assert.True(fired);
    }

    [Fact]
    public void Checkbox_ToggleAfterRender_ChangesPixels()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var cb = new LumiCheckbox { Label = "Toggle" };
        host.AddChild(cb.Root);
        RelayoutAndPaint(p);

        // Sample pixel inside the checkbox box area
        // The checkbox box is 22x22 with border 2px; the indicator is 12x12 centered inside
        var checkBox = cb.Root.Children[0]; // The outer border box
        int sampleX = (int)(checkBox.LayoutBox.X + checkBox.LayoutBox.Width / 2);
        int sampleY = (int)(checkBox.LayoutBox.Y + checkBox.LayoutBox.Height / 2);

        var before = p.GetPixelAt(sampleX, sampleY);

        cb.IsChecked = true;
        RelayoutAndPaint(p);

        var after = p.GetPixelAt(sampleX, sampleY);

        Assert.True(before != after,
            $"Checkbox indicator pixels should change after toggling IsChecked (sampled at ({sampleX},{sampleY}), before={before}, after={after})");
    }

    [Fact]
    public void Slider_ValueChange_ChangesFillWidth()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var slider = new LumiSlider { Min = 0, Max = 1, Value = 0 };
        host.AddChild(slider.Root);
        RelayoutAndPaint(p);

        // fill = track's first child
        var fill = slider.Root.Children[0].Children[0];
        var widthAtZero = fill.LayoutBox.Width;

        slider.Value = 1;
        RelayoutAndPaint(p);

        var widthAtOne = fill.LayoutBox.Width;

        Assert.True(widthAtOne > widthAtZero,
            $"Fill width at Value=1 ({widthAtOne}) should be greater than at Value=0 ({widthAtZero})");
    }

    [Fact]
    public void Dialog_Visibility_TogglesRenderedPixels()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var dlg = new LumiDialog { Title = "Confirm", IsOpen = false };
        host.AddChild(dlg.Root);
        RelayoutAndPaint(p);

        bool hasContentWhenClosed = p.HasContentInRegion(50, 50, 300, 300);

        dlg.IsOpen = true;
        RelayoutAndPaint(p);

        bool hasContentWhenOpen = p.HasContentInRegion(50, 50, 300, 300);

        Assert.False(hasContentWhenClosed, "Closed dialog should not render content");
        Assert.True(hasContentWhenOpen, "Open dialog should render content");
    }

    [Fact]
    public void List_WithItems_RendersAllChildren()
    {
        using var p = HeadlessPipeline.Render(HostHtml, HostCss, 400, 400);
        var host = p.FindById("host")!;

        var list = new LumiList { Items = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"] };
        host.AddChild(list.Root);
        p.Rerender();

        Assert.Equal(5, list.Root.Children.Count);
        Assert.True(p.HasContentInRegion(0, 0, 400, 400));
    }
}
