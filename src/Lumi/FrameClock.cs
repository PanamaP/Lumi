using System.Diagnostics;

namespace Lumi;

/// <summary>
/// High-resolution frame clock for consistent frame pacing.
/// Tracks delta time and provides precise frame-budget-aware waiting.
/// </summary>
public sealed class FrameClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _frameStartTicks;
    private long _previousFrameStartTicks;
    private int _targetRefreshRate;
    private double _targetFrameTimeMs;

    /// <summary>
    /// Time elapsed since the previous frame, in seconds.
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    /// Time elapsed during the current frame's work (between BeginFrame and now), in milliseconds.
    /// </summary>
    public double ElapsedFrameTimeMs => (_stopwatch.ElapsedTicks - _frameStartTicks) * 1000.0 / Stopwatch.Frequency;

    /// <summary>
    /// The target refresh rate in Hz.
    /// </summary>
    public int TargetRefreshRate
    {
        get => _targetRefreshRate;
        set
        {
            _targetRefreshRate = value;
            _targetFrameTimeMs = value > 0 ? 1000.0 / value : 0;
        }
    }

    /// <summary>
    /// Target frame time in milliseconds (derived from TargetRefreshRate).
    /// </summary>
    public double TargetFrameTimeMs => _targetFrameTimeMs;

    public FrameClock(int targetRefreshRate = 60)
    {
        TargetRefreshRate = targetRefreshRate;
        _frameStartTicks = _stopwatch.ElapsedTicks;
        _previousFrameStartTicks = _frameStartTicks;
    }

    /// <summary>
    /// Call at the start of each frame. Updates DeltaTime.
    /// </summary>
    public void BeginFrame()
    {
        _previousFrameStartTicks = _frameStartTicks;
        _frameStartTicks = _stopwatch.ElapsedTicks;
        DeltaTime = (_frameStartTicks - _previousFrameStartTicks) / (double)Stopwatch.Frequency;

        // Clamp delta to avoid spiral-of-death after long stalls (e.g. breakpoints, window drag)
        if (DeltaTime > 0.1)
            DeltaTime = 0.1;
    }

    /// <summary>
    /// Call at the end of each frame. Sleeps to hit the target frame time,
    /// using Thread.Sleep for bulk waiting and SpinWait for sub-ms precision.
    /// </summary>
    public void WaitForNextFrame()
    {
        if (_targetFrameTimeMs <= 0)
            return;

        double elapsedMs = ElapsedFrameTimeMs;
        double remainingMs = _targetFrameTimeMs - elapsedMs;

        if (remainingMs <= 0)
            return;

        // Sleep for bulk of the remaining time (leave ~0.5ms for spin-wait precision)
        if (remainingMs > 1.0)
        {
            Thread.Sleep((int)(remainingMs - 0.5));
        }

        // Spin-wait for the final sub-millisecond precision
        double targetTicks = _frameStartTicks + (_targetFrameTimeMs / 1000.0 * Stopwatch.Frequency);
        var spinner = new SpinWait();
        while (_stopwatch.ElapsedTicks < targetTicks)
        {
            spinner.SpinOnce();
        }
    }

    /// <summary>
    /// Lightweight idle wait — used when no work was done this frame (clean state).
    /// Sleeps without spin-waiting to minimize CPU usage.
    /// </summary>
    public void IdleWait()
    {
        double elapsedMs = ElapsedFrameTimeMs;
        double remainingMs = _targetFrameTimeMs - elapsedMs;

        if (remainingMs > 1.0)
            Thread.Sleep(Math.Max(1, (int)remainingMs));
    }
}
