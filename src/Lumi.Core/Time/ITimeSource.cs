namespace Lumi.Core.Time;

/// <summary>
/// Abstraction for wall-clock reads, allowing deterministic time control in tests.
/// </summary>
public interface ITimeSource
{
    /// <summary>Monotonic time in seconds since this source was created.</summary>
    double NowSeconds { get; }

    /// <summary>Equivalent of <see cref="Environment.TickCount64"/> in milliseconds.</summary>
    long TickCount64 { get; }
}

/// <summary>
/// Default <see cref="ITimeSource"/> backed by a started <see cref="System.Diagnostics.Stopwatch"/>.
/// </summary>
public sealed class StopwatchTimeSource : ITimeSource
{
    private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
    /// <inheritdoc />
    public double NowSeconds => _sw.Elapsed.TotalSeconds;
    /// <inheritdoc />
    public long TickCount64 => _sw.ElapsedMilliseconds;
}

/// <summary>
/// Test-controlled <see cref="ITimeSource"/>; advances only when <see cref="Advance"/> is called.
/// </summary>
public sealed class ManualTimeSource : ITimeSource
{
    private double _now;
    private long _tick;

    public double NowSeconds => _now;
    public long TickCount64 => _tick;

    public void Advance(double seconds)
    {
        if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        _now += seconds;
        _tick += (long)(seconds * 1000.0);
    }
}

/// <summary>
/// Process-wide ambient <see cref="ITimeSource"/>. Defaults to a <see cref="StopwatchTimeSource"/>;
/// tests may swap in a <see cref="ManualTimeSource"/>.
/// </summary>
public static class TimeSource
{
    public static ITimeSource Default { get; set; } = new StopwatchTimeSource();
}
