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

    /// <inheritdoc />
    public double NowSeconds => _now;
    /// <inheritdoc />
    public long TickCount64 => _tick;

    /// <summary>Advance the manual clock by <paramref name="seconds"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="seconds"/> is negative.</exception>
    public void Advance(double seconds)
    {
        if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        _now += seconds;
        // Derive _tick from _now to keep TickCount64 consistent with NowSeconds and avoid
        // per-step truncation drift (e.g. 1/60s steps would otherwise lose ~0.666ms each).
        _tick = (long)Math.Round(_now * 1000.0);
    }
}

/// <summary>
/// Process-wide ambient <see cref="ITimeSource"/>. Defaults to a <see cref="StopwatchTimeSource"/>;
/// tests may swap in a <see cref="ManualTimeSource"/>.
/// </summary>
public static class TimeSource
{
    private static ITimeSource _default = new StopwatchTimeSource();

    /// <summary>
    /// Gets or sets the ambient process-wide <see cref="ITimeSource"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static ITimeSource Default
    {
        get => _default;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _default = value;
        }
    }
}
