using System.Diagnostics;

namespace Lumi;

/// <summary>
/// High-resolution frame clock for tracking frame timing.
/// Provides DeltaTime for animations and frame-time metrics.
/// Frame pacing is handled by VSync (SwapBuffers) and event-driven idle (SDL_WaitEvent).
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

        // Clamp delta to avoid spiral-of-death after long stalls (e.g. breakpoints, idle wait)
        if (DeltaTime > 0.1)
            DeltaTime = 0.1;
    }
}
