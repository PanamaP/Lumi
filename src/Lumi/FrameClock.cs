using Lumi.Core.Time;

namespace Lumi;

/// <summary>
/// High-resolution frame clock for tracking frame timing.
/// Provides DeltaTime for animations and frame-time metrics.
/// Frame pacing is handled by VSync (SwapBuffers) and event-driven idle (SDL_WaitEvent).
/// </summary>
public sealed class FrameClock
{
    private readonly ITimeSource _timeSource;
    private double _frameStartSeconds;
    private double _previousFrameStartSeconds;
    private int _targetRefreshRate;
    private double _targetFrameTimeMs;

    /// <summary>
    /// Time elapsed since the previous frame, in seconds.
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    /// Time elapsed during the current frame's work (between BeginFrame and now), in milliseconds.
    /// </summary>
    public double ElapsedFrameTimeMs => (_timeSource.NowSeconds - _frameStartSeconds) * 1000.0;

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

#pragma warning disable RS0027 // Existing shipped API (with optional parameter); keep as-is. The 2-arg overload below adds the ITimeSource injection point.
    public FrameClock(int targetRefreshRate = 60)
        : this(targetRefreshRate, TimeSource.Default)
    {
    }
#pragma warning restore RS0027

    public FrameClock(int targetRefreshRate, ITimeSource timeSource)
    {
        _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        TargetRefreshRate = targetRefreshRate;
        _frameStartSeconds = _timeSource.NowSeconds;
        _previousFrameStartSeconds = _frameStartSeconds;
    }

    /// <summary>
    /// Call at the start of each frame. Updates DeltaTime.
    /// </summary>
    public void BeginFrame()
    {
        _previousFrameStartSeconds = _frameStartSeconds;
        _frameStartSeconds = _timeSource.NowSeconds;
        DeltaTime = _frameStartSeconds - _previousFrameStartSeconds;

        // Clamp delta to avoid spiral-of-death after long stalls (e.g. breakpoints, idle wait)
        if (DeltaTime > 0.1)
            DeltaTime = 0.1;
    }
}
