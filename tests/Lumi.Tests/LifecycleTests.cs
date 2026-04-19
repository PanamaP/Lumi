using Lumi.Core;
using Lumi.Tests.Helpers;

namespace Lumi.Tests;

[Collection("Lifecycle")]
public class LifecycleTests
{
    private const string TinyHtml = "<div id=\"root\"><span>hi</span></div>";
    private const string TinyCss = "div { background: red; padding: 4px; } span { color: blue; }";
    private const string IterationsEnvironmentVariable = "LUMI_LIFECYCLE_ITERATIONS";
    private const string WarmupEnvironmentVariable = "LUMI_LIFECYCLE_WARMUP";

    private static long MeasureMemoryDelta(Action scenario, int iterations = 1000, int warmup = 50)
    {
        iterations = GetConfiguredCount(IterationsEnvironmentVariable, iterations);
        warmup = GetConfiguredCount(WarmupEnvironmentVariable, warmup);

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

    private static int GetConfiguredCount(string environmentVariableName, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariableName);
        return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
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
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public void Element_AddChild_RemoveChild_ClearsParentAndRemovesFromChildren()
    {
        var parent = new BoxElement("div");
        long delta = MeasureMemoryDelta(() =>
        {
            var child = new BoxElement("span");
            parent.AddChild(child);
            parent.RemoveChild(child);
            Assert.Null(child.Parent);
        });

        Assert.Empty(parent.Children);
        Assert.True(delta < 1 * 1024 * 1024,
            $"Element add/remove leaked {delta:N0} bytes (>1 MB)");
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
        // Primary assertion is pointer reuse above; this delta is a smoke check
        // with generous headroom for JIT warm-up and GC variance across runners.
        Assert.True(delta < 8 * 1024 * 1024,
            $"Pipeline rerender leaked {delta:N0} bytes (>8 MB)");
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
            EventDispatcher.Dispatch(new RoutedMouseEvent("Click"), button!);
        }, iterations: 5000);

        Assert.True(delta < 1 * 1024 * 1024,
            $"Click dispatch leaked {delta:N0} bytes (>1 MB)");
    }
}
