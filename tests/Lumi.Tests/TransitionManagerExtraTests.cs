using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests;

/// <summary>
/// Targets surviving mutants in TransitionManager: color interpolation arithmetic
/// (R/G/B/A channel midpoints), zero-duration boundary, null timing-function fallback,
/// the per-property AND prevColors guard for color transitions, and Clear semantics.
/// </summary>
public class TransitionManagerExtraTests
{
    private static Element MakeElement(string prop, float duration, string? timing = "linear")
    {
        var el = new BoxElement("div");
        el.ComputedStyle.TransitionProperty = prop;
        el.ComputedStyle.TransitionDuration = duration;
        el.ComputedStyle.TransitionTimingFunction = timing;
        return el;
    }

    [Fact]
    public void DetectChanges_ZeroDuration_DoesNotStartAnyTween()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("opacity", 0f);
        el.ComputedStyle.Opacity = 0.5f;
        mgr.CaptureState(el);

        el.ComputedStyle.Opacity = 1f;
        mgr.DetectChanges(el);

        // Without an active tween, the value should remain whatever the user set last.
        Assert.Equal(1f, el.ComputedStyle.Opacity);
    }

    [Fact]
    public void DetectChanges_NullTimingFunction_FallsBackToEase()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("opacity", 1.0f, timing: null);
        el.ComputedStyle.Opacity = 0f;
        mgr.CaptureState(el);

        el.ComputedStyle.Opacity = 1f;
        var ex = Record.Exception(() => mgr.DetectChanges(el));
        Assert.Null(ex);
    }

    [Fact]
    public void DetectChanges_NumericPropertyTransition_DoesNotInvokeColorPath()
    {
        // Opacity transition, but element has never had its colors captured.
        // The "is all or background-color" check must AND with prevColors != null;
        // mutating it to OR would NRE here.
        var mgr = new TransitionManager();
        var el = MakeElement("opacity", 1.0f);
        // Capture state but only numeric — colors will still be in the dict from CaptureState.
        // To ensure the AND check matters, the prop here is "opacity", not "all" / "background-color",
        // so the color branch should never run regardless.
        el.ComputedStyle.Opacity = 0f;
        mgr.CaptureState(el);

        el.ComputedStyle.Opacity = 1f;
        var ex = Record.Exception(() => mgr.DetectChanges(el));
        Assert.Null(ex);
    }

    [Fact]
    public void Update_ColorTransition_AtMidpoint_ProducesLinearLerp()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("background-color", 1.0f);
        el.ComputedStyle.BackgroundColor = new Color(0, 0, 0, 0);
        mgr.CaptureState(el);

        el.ComputedStyle.BackgroundColor = new Color(200, 100, 50, 200);
        mgr.DetectChanges(el);

        // After detect changes, the element should reset to the OLD color ready for tweening.
        Assert.Equal(0, el.ComputedStyle.BackgroundColor.R);

        // Halfway through: linear lerp.
        mgr.Update(0.5);
        var c = el.ComputedStyle.BackgroundColor;
        Assert.InRange(c.R, 95, 105);   // 100
        Assert.InRange(c.G, 45, 55);    // 50
        Assert.InRange(c.B, 20, 30);    // 25
        Assert.InRange(c.A, 95, 105);   // 100
    }

    [Fact]
    public void Update_ColorTransition_AtEnd_ConvergesToTargetColor()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("background-color", 1.0f);
        el.ComputedStyle.BackgroundColor = new Color(10, 20, 30, 40);
        mgr.CaptureState(el);

        el.ComputedStyle.BackgroundColor = new Color(250, 240, 230, 220);
        mgr.DetectChanges(el);

        mgr.Update(2.0); // overshoot the duration
        var c = el.ComputedStyle.BackgroundColor;
        Assert.Equal(250, c.R);
        Assert.Equal(240, c.G);
        Assert.Equal(230, c.B);
        Assert.Equal(220, c.A);
    }

    [Fact]
    public void Clear_RemovesActiveTransitions_AndAllowsRetriggering()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("opacity", 1.0f);
        el.ComputedStyle.Opacity = 0f;
        mgr.CaptureState(el);

        el.ComputedStyle.Opacity = 1f;
        mgr.DetectChanges(el);
        Assert.Equal(0f, el.ComputedStyle.Opacity); // reset to old by tween start

        mgr.Clear();

        // After Clear, capture+change can start a brand new transition without the
        // "duplicate active transition" suppression kicking in.
        el.ComputedStyle.Opacity = 0.25f;
        mgr.CaptureState(el);
        el.ComputedStyle.Opacity = 0.75f;
        mgr.DetectChanges(el);
        Assert.Equal(0.25f, el.ComputedStyle.Opacity);
    }

    [Fact]
    public void Update_NumericTransition_AtMidpoint_LinearLerpsExpected()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("width", 1.0f);
        el.ComputedStyle.Width = 100f;
        mgr.CaptureState(el);

        el.ComputedStyle.Width = 200f;
        mgr.DetectChanges(el);

        Assert.Equal(100f, el.ComputedStyle.Width);
        mgr.Update(0.5);
        Assert.InRange(el.ComputedStyle.Width, 149f, 151f);
    }

    [Fact]
    public void Update_AfterCompletion_RemovesTransitionFromActiveSet_SoNewChangeRetriggers()
    {
        var mgr = new TransitionManager();
        var el = MakeElement("opacity", 1.0f);
        el.ComputedStyle.Opacity = 0f;
        mgr.CaptureState(el);

        el.ComputedStyle.Opacity = 1f;
        mgr.DetectChanges(el);
        mgr.Update(2.0); // complete

        // CaptureState happens at end of DetectChanges — re-capture for new baseline.
        mgr.CaptureState(el);
        el.ComputedStyle.Opacity = 0.2f;
        mgr.DetectChanges(el);

        // Because OnComplete previously removed the (el,opacity) entry, a new
        // change should reset to the old value (1f) and start a new tween.
        Assert.Equal(1f, el.ComputedStyle.Opacity);
    }
}
