using FsCheck;
using FsCheck.Xunit;
using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests.Properties;

/// <summary>
/// Property-based invariants for the animation subsystem.
/// </summary>
public class AnimationProperties
{
    /// <summary>
    /// Easing functions must satisfy f(0) = 0 and f(1) = 1 for every input value.
    /// </summary>
    [Fact]
    public void Easing_AllNamed_HitBoundaries()
    {
        var fns = new (string name, Func<float, float> fn)[]
        {
            ("linear", Easing.Linear),
            ("in", Easing.EaseInCubic),
            ("out", Easing.EaseOutCubic),
            ("inOut", Easing.EaseInOutCubic),
            ("inQuad", Easing.EaseInQuad),
            ("outQuad", Easing.EaseOutQuad),
        };
        foreach (var (name, fn) in fns)
        {
            Assert.Equal(0f, fn(0f), 4);
            Assert.Equal(1f, fn(1f), 4);
        }
    }

    /// <summary>
    /// For all t ∈ [0, 1], every easing function must produce a value in [0, 1].
    /// (FsCheck NormalFloat narrows to a finite-magnitude float; we then clamp it
    /// into [0, 1] so we only assert the property's actual domain.)
    /// </summary>
    [Property(MaxTest = 100)]
    public void Easing_AllNamed_StayInUnitInterval(NormalFloat tArb)
    {
        float t = (float)Math.Clamp((double)tArb.Get % 1.0 + (tArb.Get < 0 ? 1.0 : 0.0), 0.0, 1.0);

        Func<float, float>[] fns =
        [
            Easing.Linear, Easing.EaseInCubic, Easing.EaseOutCubic,
            Easing.EaseInOutCubic, Easing.EaseInQuad, Easing.EaseOutQuad,
        ];
        foreach (var fn in fns)
        {
            float v = fn(t);
            Assert.True(v >= -0.001f && v <= 1.001f, $"easing returned {v} for t={t}");
        }
    }

    /// <summary>
    /// Easing functions must be monotonically non-decreasing on [0, 1].
    /// (Cubic / quadratic ease curves have no local extrema in this range.)
    /// </summary>
    [Property(MaxTest = 50)]
    public void Easing_AllNamed_AreMonotonicNonDecreasing(byte rawA, byte rawB)
    {
        float a = rawA / 255f;
        float b = rawB / 255f;
        if (a > b) (a, b) = (b, a);

        Func<float, float>[] fns =
        [
            Easing.Linear, Easing.EaseInCubic, Easing.EaseOutCubic,
            Easing.EaseInOutCubic, Easing.EaseInQuad, Easing.EaseOutQuad,
        ];
        foreach (var fn in fns)
        {
            float fa = fn(a);
            float fb = fn(b);
            Assert.True(fb + 1e-4f >= fa, $"easing not monotonic: f({a})={fa}, f({b})={fb}");
        }
    }

    /// <summary>
    /// FromName resolves the canonical CSS timing-function names to the documented
    /// implementations, and falls back to ease-in-out for unknowns.
    /// </summary>
    [Theory]
    [InlineData("linear")]
    [InlineData("ease-in")]
    [InlineData("ease-out")]
    [InlineData("ease-in-out")]
    [InlineData("ease")]
    [InlineData("nonsense")]
    public void Easing_FromName_AlwaysReturnsACallableFunction(string name)
    {
        var fn = Easing.FromName(name);
        Assert.NotNull(fn);
        // 0 and 1 must round-trip through whatever was returned.
        Assert.Equal(0f, fn(0f), 3);
        Assert.Equal(1f, fn(1f), 3);
    }

    [Fact]
    public void Easing_FromName_LinearReturnsLinearImplementation()
    {
        var fn = Easing.FromName("linear");
        Assert.Equal(0.5f, fn(0.5f), 4);
        Assert.Equal(0.25f, fn(0.25f), 4);
    }

    [Fact]
    public void Easing_FromName_EaseInReturnsCubicImplementation()
    {
        var fn = Easing.FromName("ease-in");
        Assert.Equal(0.125f, fn(0.5f), 4); // 0.5^3
    }

    [Fact]
    public void Easing_FromName_IsCaseInsensitive()
    {
        var fn = Easing.FromName("EASE-IN");
        Assert.Equal(0.125f, fn(0.5f), 4);
    }

    /// <summary>
    /// A linear tween over [from, to] driven for full duration must end at `to`,
    /// regardless of the chosen endpoints.
    /// </summary>
    [Property(MaxTest = 50)]
    public void Tween_Linear_ConvergesToTarget(int rawFrom, int rawTo)
    {
        float from = (rawFrom % 1000);
        float to = (rawTo % 1000);

        float captured = float.NaN;
        var t = new Tween(from, to, 0.5f, Easing.Linear) { OnUpdate = v => captured = v };
        var engine = new TweenEngine();
        engine.Add(t);
        engine.Update(1f); // past duration

        Assert.True(t.IsComplete);
        Assert.Equal(to, captured, 1);
    }

    /// <summary>
    /// Tween midpoint with linear easing must equal the average of from/to.
    /// </summary>
    [Property(MaxTest = 50)]
    public void Tween_Linear_MidpointIsAverage(int rawFrom, int rawTo)
    {
        float from = (rawFrom % 1000);
        float to = (rawTo % 1000);

        float captured = float.NaN;
        var t = new Tween(from, to, 1f, Easing.Linear) { OnUpdate = v => captured = v };
        var engine = new TweenEngine();
        engine.Add(t);
        engine.Update(0.5f);

        Assert.Equal((from + to) / 2f, captured, 1);
    }
}
