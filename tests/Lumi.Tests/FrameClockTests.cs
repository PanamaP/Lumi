using Lumi.Core;

namespace Lumi.Tests;

public class FrameClockTests
{
    [Fact]
    public void Constructor_DefaultRefreshRate_Is60()
    {
        var clock = new FrameClock();
        Assert.Equal(60, clock.TargetRefreshRate);
    }

    [Fact]
    public void Constructor_CustomRefreshRate()
    {
        var clock = new FrameClock(144);
        Assert.Equal(144, clock.TargetRefreshRate);
    }

    [Fact]
    public void TargetFrameTimeMs_CalculatedFromRefreshRate()
    {
        var clock = new FrameClock(60);
        Assert.Equal(1000.0 / 60, clock.TargetFrameTimeMs, 2);

        clock.TargetRefreshRate = 120;
        Assert.Equal(1000.0 / 120, clock.TargetFrameTimeMs, 2);
    }

    [Fact]
    public void TargetFrameTimeMs_ZeroRefreshRate_ReturnsZero()
    {
        var clock = new FrameClock(0);
        Assert.Equal(0, clock.TargetFrameTimeMs);
    }

    [Fact]
    public void DeltaTime_InitiallyZero()
    {
        var clock = new FrameClock();
        // Before first BeginFrame, DeltaTime is 0
        Assert.Equal(0, clock.DeltaTime);
    }

    [Fact]
    public void BeginFrame_UpdatesDeltaTime()
    {
        var clock = new FrameClock();

        // First begin frame sets the starting point
        clock.BeginFrame();

        // Wait a small amount and call again
        System.Threading.Thread.Sleep(20);
        clock.BeginFrame();

        Assert.True(clock.DeltaTime > 0, "DeltaTime should be positive after second BeginFrame");
    }

    [Fact]
    public void BeginFrame_DeltaTime_ClampedToMaximum()
    {
        var clock = new FrameClock();
        clock.BeginFrame();

        // Simulate a long stall (> 100ms)
        System.Threading.Thread.Sleep(150);
        clock.BeginFrame();

        // DeltaTime should be clamped to 0.1 seconds
        Assert.True(clock.DeltaTime <= 0.1, $"DeltaTime should be clamped to 0.1, got {clock.DeltaTime}");
    }

    [Fact]
    public void ElapsedFrameTimeMs_IsNonNegative()
    {
        var clock = new FrameClock();
        clock.BeginFrame();

        Assert.True(clock.ElapsedFrameTimeMs >= 0, "ElapsedFrameTimeMs should not be negative");
    }

    [Fact]
    public void TargetRefreshRate_SetUpdatesFrameTime()
    {
        var clock = new FrameClock(60);
        Assert.Equal(60, clock.TargetRefreshRate);

        clock.TargetRefreshRate = 30;
        Assert.Equal(30, clock.TargetRefreshRate);
        Assert.Equal(1000.0 / 30, clock.TargetFrameTimeMs, 2);
    }

    [Fact]
    public void BeginFrame_ConsecutiveCalls_ProducesSmallDelta()
    {
        var clock = new FrameClock();
        clock.BeginFrame();
        clock.BeginFrame(); // Immediately after

        // Should be very small (less than a couple of ms)
        Assert.True(clock.DeltaTime < 0.01, $"Consecutive calls should produce small delta, got {clock.DeltaTime}");
    }
}
