using Lumi.Core;

namespace Lumi.Tests;

public class FrameMetricsTests
{
    // --- Basic frame timing ---

    [Fact]
    public void AverageFps_IsZero_BeforeAnyFrames()
    {
        var metrics = new FrameMetrics();
        Assert.Equal(0, metrics.AverageFps);
    }

    [Fact]
    public void CurrentFps_IsZero_BeforeAnyFrames()
    {
        var metrics = new FrameMetrics();
        Assert.Equal(0, metrics.CurrentFps);
    }

    [Fact]
    public void BeginFrame_And_EndFrame_RecordsTiming()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        System.Threading.Thread.Sleep(5); // Small delay
        metrics.EndFrame();

        Assert.True(metrics.TotalFrameTimeMs > 0, "Total frame time should be positive");
    }

    [Fact]
    public void CurrentFps_Calculated_FromLastFrameTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        System.Threading.Thread.Sleep(10); // ~100 FPS
        metrics.EndFrame();

        Assert.True(metrics.CurrentFps > 0, "Current FPS should be positive");
        Assert.True(metrics.CurrentFps < 1000, "FPS should be reasonable");
    }

    [Fact]
    public void AverageFps_Calculated_OverMultipleFrames()
    {
        var metrics = new FrameMetrics();

        for (int i = 0; i < 5; i++)
        {
            metrics.BeginFrame();
            System.Threading.Thread.Sleep(5);
            metrics.EndFrame();
        }

        Assert.True(metrics.AverageFps > 0, "Average FPS should be positive after multiple frames");
    }

    // --- Stage timing ---

    [Fact]
    public void RecordPoll_RecordsStageTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        System.Threading.Thread.Sleep(2);
        metrics.RecordPoll();

        Assert.True(metrics.PollTimeMs > 0, "Poll time should be positive");
    }

    [Fact]
    public void RecordUpdate_RecordsStageTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        metrics.RecordUpdate();

        Assert.True(metrics.UpdateTimeMs >= 0, "Update time should be non-negative");
    }

    [Fact]
    public void RecordStyle_RecordsStageTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        metrics.RecordStyle();

        Assert.True(metrics.StyleTimeMs >= 0);
    }

    [Fact]
    public void RecordLayout_RecordsStageTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        metrics.RecordLayout();

        Assert.True(metrics.LayoutTimeMs >= 0);
    }

    [Fact]
    public void RecordPaint_RecordsStageTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        metrics.RecordPaint();

        Assert.True(metrics.PaintTimeMs >= 0);
    }

    [Fact]
    public void RecordPresent_RecordsStageTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        metrics.RecordPresent();

        Assert.True(metrics.PresentTimeMs >= 0);
    }

    [Fact]
    public void EndStage_ReturnsPositiveTime()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        metrics.BeginStage();
        System.Threading.Thread.Sleep(2);
        double elapsed = metrics.EndStage();

        Assert.True(elapsed > 0, "EndStage should return positive time");
    }

    // --- Rolling buffer ---

    [Fact]
    public void RollingBuffer_HandlesMoreThan120Frames()
    {
        var metrics = new FrameMetrics();

        // Record more than BufferSize (120) frames
        for (int i = 0; i < 150; i++)
        {
            metrics.BeginFrame();
            metrics.EndFrame();
        }

        // Should not throw and should still compute averages
        Assert.True(metrics.AverageFps >= 0);
    }

    // --- GetSummary ---

    [Fact]
    public void GetSummary_ContainsFpsInfo()
    {
        var metrics = new FrameMetrics();
        metrics.BeginFrame();
        System.Threading.Thread.Sleep(5);
        metrics.EndFrame();

        var summary = metrics.GetSummary();

        Assert.Contains("FPS:", summary);
        Assert.Contains("avg", summary);
        Assert.Contains("Frame:", summary);
        Assert.Contains("Paint:", summary);
        Assert.Contains("Layout:", summary);
        Assert.Contains("Style:", summary);
    }

    [Fact]
    public void GetSummary_BeforeAnyFrames_DoesNotThrow()
    {
        var metrics = new FrameMetrics();
        var summary = metrics.GetSummary();

        Assert.NotNull(summary);
        Assert.Contains("FPS:", summary);
    }

    // --- Full pipeline timing ---

    [Fact]
    public void FullPipeline_AllStagesRecorded()
    {
        var metrics = new FrameMetrics();

        metrics.BeginFrame();

        metrics.BeginStage();
        metrics.RecordPoll();

        metrics.BeginStage();
        metrics.RecordUpdate();

        metrics.BeginStage();
        metrics.RecordStyle();

        metrics.BeginStage();
        metrics.RecordLayout();

        metrics.BeginStage();
        metrics.RecordPaint();

        metrics.BeginStage();
        metrics.RecordPresent();

        metrics.EndFrame();

        Assert.True(metrics.TotalFrameTimeMs >= 0);
        Assert.True(metrics.CurrentFps > 0 || metrics.TotalFrameTimeMs == 0);
    }
}
