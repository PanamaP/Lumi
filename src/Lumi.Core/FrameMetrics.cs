using System.Diagnostics;

namespace Lumi.Core;

/// <summary>
/// Collects per-frame timing data for profiling the render pipeline.
/// Maintains a rolling buffer of the last N frames for percentile analysis.
/// </summary>
public sealed class FrameMetrics
{
    private const int BufferSize = 120;

    private readonly double[] _frameTimes = new double[BufferSize];
    private readonly Stopwatch _stopwatch = new();
    private int _frameIndex;
    private int _frameCount;

    // Per-stage timing (current frame)
    private long _stageStart;

    public double PollTimeMs { get; private set; }
    public double UpdateTimeMs { get; private set; }
    public double StyleTimeMs { get; private set; }
    public double LayoutTimeMs { get; private set; }
    public double PaintTimeMs { get; private set; }
    public double PresentTimeMs { get; private set; }
    public double TotalFrameTimeMs { get; private set; }

    /// <summary>
    /// Rolling average FPS over the last <see cref="BufferSize"/> frames.
    /// </summary>
    public double AverageFps
    {
        get
        {
            if (_frameCount == 0) return 0;
            int count = Math.Min(_frameCount, BufferSize);
            double sum = 0;
            for (int i = 0; i < count; i++)
                sum += _frameTimes[i];
            double avgMs = sum / count;
            return avgMs > 0 ? 1000.0 / avgMs : 0;
        }
    }

    /// <summary>
    /// Current instantaneous FPS based on last frame time.
    /// </summary>
    public double CurrentFps => TotalFrameTimeMs > 0 ? 1000.0 / TotalFrameTimeMs : 0;

    public void BeginFrame()
    {
        _stopwatch.Restart();
    }

    public void BeginStage()
    {
        _stageStart = _stopwatch.ElapsedTicks;
    }

    public double EndStage()
    {
        return (_stopwatch.ElapsedTicks - _stageStart) * 1000.0 / Stopwatch.Frequency;
    }

    public void RecordPoll() => PollTimeMs = EndStage();
    public void RecordUpdate() => UpdateTimeMs = EndStage();
    public void RecordStyle() => StyleTimeMs = EndStage();
    public void RecordLayout() => LayoutTimeMs = EndStage();
    public void RecordPaint() => PaintTimeMs = EndStage();
    public void RecordPresent() => PresentTimeMs = EndStage();

    public void EndFrame()
    {
        TotalFrameTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
        _frameTimes[_frameIndex % BufferSize] = TotalFrameTimeMs;
        _frameIndex = (_frameIndex + 1) % BufferSize;
        _frameCount++;
    }

    /// <summary>
    /// Returns a formatted summary string for debug overlay.
    /// </summary>
    public string GetSummary()
    {
        return $"FPS: {CurrentFps:F0} (avg {AverageFps:F0}) | " +
               $"Frame: {TotalFrameTimeMs:F1}ms | " +
               $"Paint: {PaintTimeMs:F1}ms | " +
               $"Layout: {LayoutTimeMs:F1}ms | " +
               $"Style: {StyleTimeMs:F1}ms";
    }
}
