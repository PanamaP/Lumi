using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests;

public class AnimationTests
{
    // --- Easing ---

    [Fact]
    public void Easing_Linear_ReturnsInput()
    {
        Assert.Equal(0f, Easing.Linear(0f));
        Assert.Equal(0.5f, Easing.Linear(0.5f));
        Assert.Equal(1f, Easing.Linear(1f));
    }

    [Fact]
    public void Easing_EaseInCubic_BoundaryValues()
    {
        Assert.Equal(0f, Easing.EaseInCubic(0f));
        Assert.Equal(1f, Easing.EaseInCubic(1f));
    }

    [Fact]
    public void Easing_EaseOutCubic_BoundaryValues()
    {
        Assert.Equal(0f, Easing.EaseOutCubic(0f));
        Assert.Equal(1f, Easing.EaseOutCubic(1f), 4);
    }

    [Fact]
    public void Easing_EaseInCubic_MiddleValue()
    {
        float result = Easing.EaseInCubic(0.5f);
        Assert.Equal(0.125f, result, 4); // 0.5^3 = 0.125
    }

    [Fact]
    public void Easing_EaseOutCubic_MiddleValue()
    {
        float result = Easing.EaseOutCubic(0.5f);
        Assert.Equal(0.875f, result, 4); // 1 - (1-0.5)^3 = 0.875
    }

    // --- Tween ---

    [Fact]
    public void Tween_InterpolatesCorrectly_AtBoundaries()
    {
        var tween = new Tween(10f, 20f, 1f);
        // At t=0, value should be From
        Assert.Equal(10f, tween.Value, 2);
    }

    [Fact]
    public void Tween_InterpolatesCorrectly_AtMidpoint()
    {
        float captured = 0;
        var tween = new Tween(0f, 100f, 1f, Easing.Linear);
        tween.OnUpdate = v => captured = v;

        var engine = new TweenEngine();
        engine.Add(tween);
        engine.Update(0.5f);

        Assert.Equal(50f, captured, 1);
    }

    [Fact]
    public void Tween_InterpolatesCorrectly_AtEnd()
    {
        float captured = 0;
        var tween = new Tween(0f, 100f, 1f, Easing.Linear);
        tween.OnUpdate = v => captured = v;

        var engine = new TweenEngine();
        engine.Add(tween);
        engine.Update(1f);

        Assert.Equal(100f, captured, 1);
    }

    [Fact]
    public void Tween_CompletesAfterDurationElapsed()
    {
        var tween = new Tween(0f, 1f, 0.5f);
        Assert.False(tween.IsComplete);

        var engine = new TweenEngine();
        engine.Add(tween);
        engine.Update(0.6f);

        Assert.True(tween.IsComplete);
    }

    [Fact]
    public void Tween_FiresOnCompleteCallback()
    {
        bool completed = false;
        var tween = new Tween(0f, 1f, 0.5f);
        tween.OnComplete = () => completed = true;

        var engine = new TweenEngine();
        engine.Add(tween);
        engine.Update(1f); // Advance past duration

        Assert.True(completed, "OnComplete should have been called");
    }

    // --- TweenEngine ---

    [Fact]
    public void TweenEngine_Update_TicksAllActiveTweens()
    {
        var engine = new TweenEngine();
        float val1 = 0, val2 = 0;

        var t1 = new Tween(0f, 100f, 1f, Easing.Linear) { OnUpdate = v => val1 = v };
        var t2 = new Tween(0f, 200f, 1f, Easing.Linear) { OnUpdate = v => val2 = v };
        engine.Add(t1);
        engine.Add(t2);

        engine.Update(0.5f);

        Assert.True(val1 > 0, "Tween 1 should have updated");
        Assert.True(val2 > 0, "Tween 2 should have updated");
    }

    [Fact]
    public void TweenEngine_Update_RemovesCompletedTweens()
    {
        var engine = new TweenEngine();
        var tween = new Tween(0f, 1f, 0.5f);
        engine.Add(tween);

        engine.Update(0.1f); // Start it
        Assert.Equal(1, engine.ActiveCount);

        engine.Update(1f); // Complete it
        Assert.Equal(0, engine.ActiveCount);
    }

    [Fact]
    public void TweenEngine_Clear_RemovesAllTweens()
    {
        var engine = new TweenEngine();
        engine.Add(new Tween(0f, 1f, 5f));
        engine.Add(new Tween(0f, 1f, 5f));

        engine.Update(0.01f); // Move from pending to active
        engine.Clear();

        Assert.Equal(0, engine.ActiveCount);
    }

    // --- KeyframeAnimation ---

    [Fact]
    public void KeyframeAnimation_StoresKeyframesCorrectly()
    {
        var anim = new KeyframeAnimation
        {
            Name = "fadeIn",
            Duration = 1f,
            IterationCount = 1,
            Keyframes =
            [
                new Keyframe(0f, new Dictionary<string, string> { ["opacity"] = "0" }),
                new Keyframe(100f, new Dictionary<string, string> { ["opacity"] = "1" }),
            ]
        };

        Assert.Equal("fadeIn", anim.Name);
        Assert.Equal(2, anim.Keyframes.Count);
        Assert.Equal(0f, anim.Keyframes[0].Percent);
        Assert.Equal(100f, anim.Keyframes[1].Percent);
        Assert.Equal("0", anim.Keyframes[0].Properties["opacity"]);
        Assert.Equal("1", anim.Keyframes[1].Properties["opacity"]);
    }

    // --- AnimationBuilder ---

    [Fact]
    public void AnimationBuilder_FluentApi_CreatesValidTween()
    {
        var engine = new TweenEngine();
        var element = new BoxElement("div");
        bool completed = false;

        element.Animate(engine)
            .Property("opacity", 0f, 1f)
            .Duration(0.5f)
            .Easing(Easing.Linear)
            .OnComplete(() => completed = true)
            .Start();

        // Engine should have a tween now
        engine.Update(0.01f); // Activate pending tweens
        Assert.True(engine.ActiveCount > 0, "Engine should have active tweens after Start()");

        engine.Update(1f); // Complete
        Assert.True(completed, "OnComplete should fire after animation completes");
    }
}
