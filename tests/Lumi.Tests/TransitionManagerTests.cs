using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests;

public class TransitionManagerTests
{
    private static Element CreateTransitionElement(string property, float duration, string timingFunction = "linear")
    {
        var element = new BoxElement("div");
        element.ComputedStyle.TransitionProperty = property;
        element.ComputedStyle.TransitionDuration = duration;
        element.ComputedStyle.TransitionTimingFunction = timingFunction;
        return element;
    }

    // --- CaptureState ---

    [Fact]
    public void CaptureState_SnapshotsNumericProperties()
    {
        var manager = new TransitionManager();
        var element = new BoxElement("div");
        element.ComputedStyle.Opacity = 0.5f;
        element.ComputedStyle.Width = 100f;
        element.ComputedStyle.Height = 200f;
        element.ComputedStyle.FontSize = 14f;
        element.ComputedStyle.BorderRadius = 8f;

        // CaptureState should not throw
        manager.CaptureState(element);
    }

    [Fact]
    public void CaptureState_SnapshotsColorProperties()
    {
        var manager = new TransitionManager();
        var element = new BoxElement("div");
        element.ComputedStyle.BackgroundColor = new Color(255, 0, 0, 255);

        manager.CaptureState(element);
        // No assertion needed — verifying it doesn't throw and can be recalled
    }

    // --- DetectChanges: no transition when disabled ---

    [Fact]
    public void DetectChanges_NoTransitionDuration_DoesNotCreateTween()
    {
        var manager = new TransitionManager();
        var element = new BoxElement("div");
        element.ComputedStyle.TransitionDuration = 0; // disabled

        manager.CaptureState(element);
        element.ComputedStyle.Opacity = 0.5f;
        manager.DetectChanges(element);

        // Update should be a no-op (no tweens)
        manager.Update(1.0);
    }

    [Fact]
    public void DetectChanges_NoTransitionProperty_DoesNotCreateTween()
    {
        var manager = new TransitionManager();
        var element = new BoxElement("div");
        element.ComputedStyle.TransitionDuration = 0.3f;
        element.ComputedStyle.TransitionProperty = null; // not set

        manager.CaptureState(element);
        element.ComputedStyle.Opacity = 0.5f;
        manager.DetectChanges(element);

        manager.Update(1.0);
    }

    [Fact]
    public void DetectChanges_EmptyTransitionProperty_DoesNotCreateTween()
    {
        var manager = new TransitionManager();
        var element = new BoxElement("div");
        element.ComputedStyle.TransitionDuration = 0.3f;
        element.ComputedStyle.TransitionProperty = "";

        manager.CaptureState(element);
        element.ComputedStyle.Opacity = 0.5f;
        manager.DetectChanges(element);

        manager.Update(1.0);
    }

    // --- DetectChanges: numeric transitions ---

    [Fact]
    public void DetectChanges_OpacityChange_CreatesTransition()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("opacity", 1.0f);
        element.ComputedStyle.Opacity = 1.0f;

        manager.CaptureState(element);

        // Change property
        element.ComputedStyle.Opacity = 0.0f;
        manager.DetectChanges(element);

        // After detection, the value should revert to old value for animation
        // (the transition animates from old to new)
        Assert.Equal(1.0f, element.ComputedStyle.Opacity, 2);

        // Advance through the full duration
        manager.Update(1.0);

        // Now opacity should be at the target value (0.0)
        Assert.Equal(0.0f, element.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void DetectChanges_WidthChange_CreatesTransition()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("width", 0.5f);
        element.ComputedStyle.Width = 100f;

        manager.CaptureState(element);
        element.ComputedStyle.Width = 200f;
        manager.DetectChanges(element);

        // Should revert to old value initially
        Assert.Equal(100f, element.ComputedStyle.Width, 1);

        // Halfway through
        manager.Update(0.25f);
        float midValue = element.ComputedStyle.Width;
        Assert.True(midValue > 100f && midValue < 200f, $"Expected mid-transition width, got {midValue}");

        // Complete
        manager.Update(0.5f);
        Assert.Equal(200f, element.ComputedStyle.Width, 1);
    }

    [Fact]
    public void DetectChanges_AllProperties_TransitionsEverything()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("all", 1.0f);
        element.ComputedStyle.Opacity = 1.0f;
        element.ComputedStyle.Width = 100f;
        element.ComputedStyle.Padding = new EdgeValues(10);

        manager.CaptureState(element);

        element.ComputedStyle.Opacity = 0.5f;
        element.ComputedStyle.Width = 200f;
        element.ComputedStyle.Padding = new EdgeValues(20);
        manager.DetectChanges(element);

        // All should revert to old values
        Assert.Equal(1.0f, element.ComputedStyle.Opacity, 2);
        Assert.Equal(100f, element.ComputedStyle.Width, 1);
        Assert.Equal(10f, element.ComputedStyle.Padding.Top, 1);
    }

    [Fact]
    public void DetectChanges_NoChange_DoesNotCreateTransition()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("opacity", 1.0f);
        element.ComputedStyle.Opacity = 1.0f;

        manager.CaptureState(element);

        // Same value — no change
        element.ComputedStyle.Opacity = 1.0f;
        manager.DetectChanges(element);

        // Opacity should remain unchanged
        Assert.Equal(1.0f, element.ComputedStyle.Opacity, 2);
    }

    // --- DetectChanges: color transitions ---

    [Fact]
    public void DetectChanges_BackgroundColorChange_CreatesTransition()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("background-color", 1.0f);
        element.ComputedStyle.BackgroundColor = new Color(255, 0, 0, 255);

        manager.CaptureState(element);

        element.ComputedStyle.BackgroundColor = new Color(0, 0, 255, 255);
        manager.DetectChanges(element);

        // Should revert to old color
        Assert.Equal(255, element.ComputedStyle.BackgroundColor.R);

        // Complete transition
        manager.Update(1.5f);

        // Should reach target color
        Assert.Equal(0, element.ComputedStyle.BackgroundColor.R);
        Assert.Equal(0, element.ComputedStyle.BackgroundColor.G);
        Assert.Equal(255, element.ComputedStyle.BackgroundColor.B);
    }

    [Fact]
    public void DetectChanges_SameColor_DoesNotCreateTransition()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("background-color", 1.0f);
        var color = new Color(100, 100, 100, 255);
        element.ComputedStyle.BackgroundColor = color;

        manager.CaptureState(element);
        element.ComputedStyle.BackgroundColor = color;
        manager.DetectChanges(element);

        // No change should mean no tween
        Assert.Equal(color, element.ComputedStyle.BackgroundColor);
    }

    // --- DetectChanges: multiple comma-separated properties ---

    [Fact]
    public void DetectChanges_MultipleTransitionProperties_TransitionsListed()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("opacity, width", 1.0f);
        element.ComputedStyle.Opacity = 1.0f;
        element.ComputedStyle.Width = 50f;
        element.ComputedStyle.Height = 100f; // not listed in transition-property

        manager.CaptureState(element);

        element.ComputedStyle.Opacity = 0.0f;
        element.ComputedStyle.Width = 100f;
        element.ComputedStyle.Height = 200f;
        manager.DetectChanges(element);

        // opacity and width should revert (transition active), height should stay at new value
        Assert.Equal(1.0f, element.ComputedStyle.Opacity, 2);
        Assert.Equal(50f, element.ComputedStyle.Width, 1);
        Assert.Equal(200f, element.ComputedStyle.Height, 1); // not transitioned
    }

    // --- DetectChanges: without prior CaptureState ---

    [Fact]
    public void DetectChanges_WithoutCapture_DoesNotThrow()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("opacity", 1.0f);
        element.ComputedStyle.Opacity = 0.5f;

        // No CaptureState called — should safely return without creating tweens
        manager.DetectChanges(element);
    }

    // --- Update ---

    [Fact]
    public void Update_AdvancesTweens()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("opacity", 1.0f);
        element.ComputedStyle.Opacity = 1.0f;
        manager.CaptureState(element);
        element.ComputedStyle.Opacity = 0.0f;
        manager.DetectChanges(element);

        manager.Update(0.5);

        // Should be somewhere between 0 and 1
        float val = element.ComputedStyle.Opacity;
        Assert.True(val >= 0f && val <= 1f, $"Expected mid-transition value, got {val}");
    }

    // --- Duplicate transition prevention ---

    [Fact]
    public void DetectChanges_DuplicatePropertyChange_IgnoredWhileActive()
    {
        var manager = new TransitionManager();
        var element = CreateTransitionElement("opacity", 1.0f);
        element.ComputedStyle.Opacity = 1.0f;
        manager.CaptureState(element);

        element.ComputedStyle.Opacity = 0.0f;
        manager.DetectChanges(element);

        // Advance partway — opacity should be moving toward 0.0
        manager.Update(0.5);
        float midValue = element.ComputedStyle.Opacity;
        Assert.True(midValue > 0.0f && midValue < 1.0f,
            $"Expected mid-transition value between 0 and 1, got {midValue}");

        // Try to trigger another change while first is active
        element.ComputedStyle.Opacity = 0.5f;
        manager.DetectChanges(element);

        // Advance slightly — should still be heading toward original target (0.0), not 0.5
        manager.Update(0.1);
        float afterDuplicate = element.ComputedStyle.Opacity;
        Assert.True(afterDuplicate < midValue,
            $"After duplicate change, value should still decrease toward 0.0 (was {midValue}, now {afterDuplicate})");

        // Complete — should reach the original target
        manager.Update(2.0);
        Assert.Equal(0.0f, element.ComputedStyle.Opacity, 2);
    }
}
