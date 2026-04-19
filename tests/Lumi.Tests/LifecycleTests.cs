using Lumi.Core;
using Lumi.Tests.Helpers;

namespace Lumi.Tests;

public class LifecycleTests
{
    private const string TinyHtml = "<div id=\"root\"><span>hi</span></div>";
    private const string TinyCss = "div { background: red; padding: 4px; } span { color: blue; }";

    private static long MeasureMemoryDelta(Action scenario, int iterations = 1000, int warmup = 50)
    {
        for (int i = 0; i < warmup; i++) scenario();   // JIT + reach steady state
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long before = GC.GetTotalMemory(forceFullCollection: true);
        for (int i = 0; i < iterations; i++) scenario();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long after = GC.GetTotalMemory(forceFullCollection: true);
        return after - before;
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public void HeadlessPipeline_Render_Dispose_NoLeak()
    {
        long delta = MeasureMemoryDelta(() =>
        {
            using var p = HeadlessPipeline.Render(TinyHtml, TinyCss, 200, 200);
        });

        Assert.True(delta < 5 * 1024 * 1024,
            $"Pipeline render/dispose leaked {delta:N0} bytes (>5 MB)");
        Console.WriteLine($"[Lifecycle] HeadlessPipeline_Render_Dispose delta={delta:N0} bytes");
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public void EventHandler_Subscribe_Unsubscribe_NoLeak()
    {
        var element = new BoxElement("button");
        RoutedEventHandler handler = (_, _) => { };

        long delta = MeasureMemoryDelta(() =>
        {
            element.On("Click", handler);
            element.Off("Click", handler);
        });

        Assert.True(delta < 1 * 1024 * 1024,
            $"Event subscribe/unsubscribe leaked {delta:N0} bytes (>1 MB)");
        Console.WriteLine($"[Lifecycle] EventHandler_Subscribe_Unsubscribe delta={delta:N0} bytes");
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public void Element_AddChild_RemoveChild_ClearsParentAndRemovesFromChildren()
    {
        var parent = new BoxElement("div");
        var removedChildren = new List<Element>();

        for (int i = 0; i < 1000; i++)
        {
            var child = new BoxElement("span");
            parent.AddChild(child);
            parent.RemoveChild(child);
            removedChildren.Add(child);
        }

        Assert.Empty(parent.Children);
        Assert.All(removedChildren, c => Assert.Null(c.Parent));
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public void Pipeline_Rerender_Bitmap_Reuses_Memory()
    {
        using var pipeline = HeadlessPipeline.Render(TinyHtml, TinyCss, 200, 200);
        IntPtr firstPtr = pipeline.Renderer.GetPixels();
        Assert.NotEqual(IntPtr.Zero, firstPtr);

        long delta = MeasureMemoryDelta(() =>
        {
            pipeline.Rerender();
        });

        IntPtr afterPtr = pipeline.Renderer.GetPixels();
        Assert.Equal(firstPtr, afterPtr);
        Assert.True(delta < 2 * 1024 * 1024,
            $"Pipeline rerender leaked {delta:N0} bytes (>2 MB)");
        Console.WriteLine($"[Lifecycle] Pipeline_Rerender_Bitmap_Reuses delta={delta:N0} bytes");
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public void Click_Dispatch_NoCacheGrowth()
    {
        using var pipeline = HeadlessPipeline.Render(
            "<div id=\"root\"><button id=\"btn\">click</button></div>",
            "button { padding: 4px; }",
            200, 200);

        var button = pipeline.FindById("btn");
        Assert.NotNull(button);
        button!.On("Click", (_, _) => { });

        long delta = MeasureMemoryDelta(() =>
        {
            EventDispatcher.Dispatch(new RoutedMouseEvent("Click"), button);
        }, iterations: 5000);

        Assert.True(delta < 1 * 1024 * 1024,
            $"Click dispatch leaked {delta:N0} bytes (>1 MB)");
        Console.WriteLine($"[Lifecycle] Click_Dispatch_NoCacheGrowth delta={delta:N0} bytes");
    }
}
