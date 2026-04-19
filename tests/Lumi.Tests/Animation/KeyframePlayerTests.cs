using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests.Animation;

/// <summary>
/// Targets surviving mutants in KeyframePlayer: registry lookup, Play/Update lifecycle,
/// keyframe interpolation across iterations, direction (Reverse/Alternate), and the
/// internal TryParseNumeric / ApplyNumericProperty paths.
/// </summary>
public class KeyframePlayerTests
{
    private static KeyframeAnimation Fade(string name = "fade", float duration = 1f)
    {
        return new KeyframeAnimation
        {
            Name = name,
            Duration = duration,
            IterationCount = 1,
            Keyframes =
            [
                new Keyframe(0f, new Dictionary<string, string> { ["opacity"] = "0" }),
                new Keyframe(1f, new Dictionary<string, string> { ["opacity"] = "1" }),
            ]
        };
    }

    [Fact]
    public void Register_Get_RoundTrips()
    {
        var p = new KeyframePlayer();
        var anim = Fade();
        p.Register(anim);

        Assert.Same(anim, p.Get("fade"));
        Assert.Same(anim, p.Get("FADE")); // case-insensitive
        Assert.Null(p.Get("missing"));
    }

    [Fact]
    public void Register_OverwritesExistingByName()
    {
        var p = new KeyframePlayer();
        p.Register(Fade("dup", 1f));
        var second = Fade("dup", 2f);
        p.Register(second);
        Assert.Same(second, p.Get("dup"));
    }

    [Fact]
    public void Play_UnknownAnimation_DoesNotAddActive()
    {
        var p = new KeyframePlayer();
        var el = new BoxElement("div");
        p.Play(el, "unknown", 1f);
        Assert.Equal(0, p.ActiveCount);
    }

    [Fact]
    public void Play_RegisteredAnimation_BecomesActive()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");
        p.Play(el, "fade", 1f);
        Assert.Equal(1, p.ActiveCount);
    }

    [Fact]
    public void Update_AdvancesOpacityBetweenKeyframes()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 0f;

        p.Play(el, "fade", 1f);
        p.Update(0.5f);

        Assert.InRange(el.ComputedStyle.Opacity, 0.4f, 0.6f);
    }

    [Fact]
    public void Update_AtCompletion_AppliesFinalKeyframe_AndRemovesActive()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");

        p.Play(el, "fade", 1f);
        p.Update(1.5f);

        Assert.Equal(1f, el.ComputedStyle.Opacity, 2);
        Assert.Equal(0, p.ActiveCount);
    }

    [Fact]
    public void Update_ReverseDirection_StartsAtEndAndGoesToStart()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");

        p.Play(el, "fade", 1f, iterationCount: 1, direction: AnimationDirection.Reverse);
        p.Update(0.25f);

        // Reverse: t = 1 - 0.25 = 0.75 → opacity ≈ 0.75
        Assert.InRange(el.ComputedStyle.Opacity, 0.7f, 0.8f);
    }

    [Fact]
    public void Update_PlayDurationOverride_UsedInsteadOfAnimationDuration()
    {
        var p = new KeyframePlayer();
        p.Register(Fade("slow", 5f));
        var el = new BoxElement("div");

        p.Play(el, "slow", duration: 1f);
        p.Update(1.1f);
        // With override duration of 1, animation should be complete after 1.1s.
        Assert.Equal(0, p.ActiveCount);
        Assert.Equal(1f, el.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void Update_WithZeroOrNegativeOverride_FallsBackToAnimationDuration()
    {
        var p = new KeyframePlayer();
        p.Register(Fade("slow", 4f));
        var el = new BoxElement("div");

        p.Play(el, "slow", duration: 0f);
        p.Update(1f);
        // After 1s of a 4s anim, should still be active and partial.
        Assert.Equal(1, p.ActiveCount);
        Assert.InRange(el.ComputedStyle.Opacity, 0.2f, 0.3f);
    }

    [Fact]
    public void Update_InfiniteIteration_NeverRemoves()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");

        p.Play(el, "fade", 1f, iterationCount: -1);
        p.Update(10f);
        Assert.Equal(1, p.ActiveCount);
    }

    [Fact]
    public void Update_MultipleIterations_RestartsEachIteration()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");

        p.Play(el, "fade", 1f, iterationCount: 3);

        // After 1.5s: in second iteration, halfway through → opacity ≈ 0.5
        p.Update(1.5f);
        Assert.InRange(el.ComputedStyle.Opacity, 0.4f, 0.6f);

        // After total of 3.1s: completed all iterations → 1.0 and removed
        p.Update(1.6f);
        Assert.Equal(0, p.ActiveCount);
        Assert.Equal(1f, el.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void Update_AlternateDirection_SecondIteration_RunsBackward()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");

        // 2 iterations, 1s each. Alternate: it 0 forward, it 1 backward.
        p.Play(el, "fade", 1f, iterationCount: 2, direction: AnimationDirection.Alternate);
        // Halfway through second iteration → going from 1 back toward 0 → opacity ≈ 0.5
        p.Update(1.5f);
        Assert.InRange(el.ComputedStyle.Opacity, 0.4f, 0.6f);
    }

    [Fact]
    public void Update_AlternateReverseDirection_FirstIteration_RunsBackward()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");

        p.Play(el, "fade", 1f, iterationCount: 2, direction: AnimationDirection.AlternateReverse);
        // First iteration backward: at t=0.25, value = 1 - 0.25 = 0.75
        p.Update(0.25f);
        Assert.InRange(el.ComputedStyle.Opacity, 0.7f, 0.8f);
    }

    [Fact]
    public void Update_AppliesNumericValuesToWidthHeightAndPadding()
    {
        var anim = new KeyframeAnimation
        {
            Name = "grow",
            Duration = 1f,
            IterationCount = 1,
            Keyframes =
            [
                new Keyframe(0f, new Dictionary<string, string>
                {
                    ["width"] = "10",
                    ["height"] = "20",
                    ["padding-left"] = "0",
                }),
                new Keyframe(1f, new Dictionary<string, string>
                {
                    ["width"] = "100",
                    ["height"] = "200",
                    ["padding-left"] = "16",
                }),
            ]
        };

        var p = new KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        p.Play(el, "grow", 1f);
        p.Update(0.5f);

        Assert.Equal(55f, el.ComputedStyle.Width, 1);
        Assert.Equal(110f, el.ComputedStyle.Height, 1);
        Assert.Equal(8f, el.ComputedStyle.Padding.Left, 1);
    }

    [Fact]
    public void Update_AppliesValuesWithUnitsViaTryParseNumeric()
    {
        var anim = new KeyframeAnimation
        {
            Name = "wpx",
            Duration = 1f,
            Keyframes =
            [
                new Keyframe(0f, new Dictionary<string, string> { ["width"] = "0px" }),
                new Keyframe(1f, new Dictionary<string, string> { ["width"] = "100px" }),
            ]
        };

        var p = new KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        p.Play(el, "wpx", 1f);
        p.Update(0.4f);

        Assert.Equal(40f, el.ComputedStyle.Width, 1);
    }

    [Fact]
    public void Update_KeyframeWithSinglePropertyDefined_PropagatesToBoth()
    {
        // Property only present in 'to' should still apply to 'from' via the
        // fromStr ??= toStr fallback.
        var anim = new KeyframeAnimation
        {
            Name = "single",
            Duration = 1f,
            Keyframes =
            [
                new Keyframe(0f, new Dictionary<string, string>()), // no opacity
                new Keyframe(1f, new Dictionary<string, string> { ["opacity"] = "0.5" }),
            ]
        };

        var p = new KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        p.Play(el, "single", 1f);
        p.Update(0.5f);

        // Both ends resolve to "0.5" so the interpolated value stays at 0.5.
        Assert.Equal(0.5f, el.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void Update_NoKeyframes_DoesNothing()
    {
        var anim = new KeyframeAnimation { Name = "empty", Duration = 1f, Keyframes = [] };
        var p = new KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 0.42f;

        p.Play(el, "empty", 1f);
        p.Update(0.5f);

        Assert.Equal(0.42f, el.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void Update_MarksElementDirty()
    {
        var p = new KeyframePlayer();
        p.Register(Fade());
        var el = new BoxElement("div");
        p.Play(el, "fade", 1f);
        el.IsDirty = false;
        p.Update(0.1f);
        Assert.True(el.IsDirty);
    }
}
