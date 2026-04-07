using Lumi.Core;
using Lumi.Core.Animation;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssKeyframeIntegrationTests
{
    // ── @keyframes registration via StyleResolver ────────────────────

    [Fact]
    public void StyleResolver_RegistersKeyframes_FromStyleSheet()
    {
        var css = @"
            @keyframes fadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
        ";
        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(css));

        var anim = resolver.KeyframePlayer.Get("fadeIn");
        Assert.NotNull(anim);
        Assert.Equal("fadeIn", anim.Name);
        Assert.Equal(2, anim.Keyframes.Count);
        Assert.Equal(0f, anim.Keyframes[0].Percent);
        Assert.Equal(1f, anim.Keyframes[1].Percent);
    }

    [Fact]
    public void StyleResolver_RegistersMultipleKeyframes()
    {
        var css = @"
            @keyframes spin { from { opacity: 1; } to { opacity: 0; } }
            @keyframes grow { 0% { width: 10px; } 100% { width: 100px; } }
        ";
        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(css));

        Assert.NotNull(resolver.KeyframePlayer.Get("spin"));
        Assert.NotNull(resolver.KeyframePlayer.Get("grow"));
    }

    // ── Auto-play via animation-name in CSS ─────────────────────────

    [Fact]
    public void AutoPlay_TriggersWhenAnimationNameSet()
    {
        var css = @"
            @keyframes fadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
            .animated { animation-name: fadeIn; animation-duration: 1s; }
        ";
        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(css));

        var el = new BoxElement("div");
        el.Classes.Add("animated");

        PropertyApplier.SetFontSizeContext(16);
        resolver.ResolveStyles(el);

        Assert.Equal("fadeIn", el.ComputedStyle.AnimationName);
        Assert.Equal(1, resolver.KeyframePlayer.ActiveCount);
    }

    [Fact]
    public void AutoPlay_DoesNotDuplicateOnReResolve()
    {
        var css = @"
            @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
            .animated { animation-name: fadeIn; animation-duration: 0.5s; }
        ";
        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(css));

        var el = new BoxElement("div");
        el.Classes.Add("animated");

        PropertyApplier.SetFontSizeContext(16);
        resolver.ResolveStyles(el);
        resolver.ResolveStyles(el); // resolve again

        Assert.Equal(1, resolver.KeyframePlayer.ActiveCount);
    }

    [Fact]
    public void AutoPlay_NoAnimationWhenNameNotRegistered()
    {
        var css = ".test { animation-name: nonexistent; animation-duration: 1s; }";
        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(css));

        var el = new BoxElement("div");
        el.Classes.Add("test");

        PropertyApplier.SetFontSizeContext(16);
        resolver.ResolveStyles(el);

        Assert.Equal(0, resolver.KeyframePlayer.ActiveCount);
    }

    // ── KeyframePlayer updates ──────────────────────────────────────

    [Fact]
    public void KeyframePlayer_InterpolatesOpacity()
    {
        var player = new KeyframePlayer();
        var anim = new KeyframeAnimation
        {
            Name = "fade",
            Duration = 1,
            Keyframes =
            [
                new(0f, new() { ["opacity"] = "0" }),
                new(1f, new() { ["opacity"] = "1" })
            ]
        };
        player.Register(anim);

        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 1f;

        player.Play(el, "fade", 1f);
        player.Update(0.5f); // 50% through

        Assert.Equal(0.5f, el.ComputedStyle.Opacity, 0.05f);
    }

    [Fact]
    public void KeyframePlayer_CompletesAnimation()
    {
        var player = new KeyframePlayer();
        var anim = new KeyframeAnimation
        {
            Name = "fade",
            Duration = 0.5f,
            Keyframes =
            [
                new(0f, new() { ["opacity"] = "0" }),
                new(1f, new() { ["opacity"] = "1" })
            ]
        };
        player.Register(anim);

        var el = new BoxElement("div");
        player.Play(el, "fade", 0.5f);

        player.Update(1.0f); // past end
        Assert.Equal(0, player.ActiveCount);
        Assert.Equal(1f, el.ComputedStyle.Opacity, 0.01f);
    }

    [Fact]
    public void KeyframePlayer_InfiniteLoop()
    {
        var player = new KeyframePlayer();
        var anim = new KeyframeAnimation
        {
            Name = "pulse",
            Duration = 1f,
            Keyframes =
            [
                new(0f, new() { ["opacity"] = "0" }),
                new(1f, new() { ["opacity"] = "1" })
            ]
        };
        player.Register(anim);

        var el = new BoxElement("div");
        player.Play(el, "pulse", 1f, iterationCount: -1);

        // After 10 seconds, still active (infinite)
        player.Update(10f);
        Assert.Equal(1, player.ActiveCount);
    }

    [Fact]
    public void KeyframePlayer_MultipleKeyframeStops()
    {
        var player = new KeyframePlayer();
        var anim = new KeyframeAnimation
        {
            Name = "three",
            Duration = 1f,
            Keyframes =
            [
                new(0f, new() { ["opacity"] = "0" }),
                new(0.5f, new() { ["opacity"] = "0.8" }),
                new(1f, new() { ["opacity"] = "0.2" })
            ]
        };
        player.Register(anim);

        var el = new BoxElement("div");
        player.Play(el, "three", 1f);

        // At 25% — between 0% and 50% keyframes
        player.Update(0.25f);
        Assert.InRange(el.ComputedStyle.Opacity, 0.3f, 0.5f);
    }

    // ── Animation shorthand parsing ─────────────────────────────────

    [Fact]
    public void AnimationShorthand_NameAndDuration()
    {
        var style = ApplyProp("animation", "fadeIn 0.3s");
        Assert.Equal("fadeIn", style.AnimationName);
        Assert.Equal(0.3f, style.AnimationDuration, 0.01f);
    }

    [Fact]
    public void AnimationShorthand_Full()
    {
        var style = ApplyProp("animation", "spin 1s ease-in-out infinite alternate");
        Assert.Equal("spin", style.AnimationName);
        Assert.Equal(1f, style.AnimationDuration, 0.01f);
        Assert.Equal("ease-in-out", style.AnimationTimingFunction);
        Assert.Equal(-1, style.AnimationIterationCount);
        Assert.Equal(AnimationDirection.Alternate, style.AnimationDirection);
    }

    [Fact]
    public void AnimationShorthand_None()
    {
        var style = ApplyProp("animation", "none");
        Assert.Null(style.AnimationName);
    }

    [Fact]
    public void AnimationShorthand_WithDelay()
    {
        var style = ApplyProp("animation", "fadeIn 1s 0.5s");
        Assert.Equal("fadeIn", style.AnimationName);
        Assert.Equal(1f, style.AnimationDuration, 0.01f);
        Assert.Equal(0.5f, style.AnimationDelay, 0.01f);
    }

    [Fact]
    public void AnimationShorthand_WithFillMode()
    {
        var style = ApplyProp("animation", "slide 2s forwards");
        Assert.Equal("slide", style.AnimationName);
        Assert.Equal(2f, style.AnimationDuration, 0.01f);
        Assert.Equal(AnimationFillMode.Forwards, style.AnimationFillMode);
    }

    // ── Animation direction enum ────────────────────────────────────

    [Theory]
    [InlineData("normal", AnimationDirection.Normal)]
    [InlineData("reverse", AnimationDirection.Reverse)]
    [InlineData("alternate", AnimationDirection.Alternate)]
    [InlineData("alternate-reverse", AnimationDirection.AlternateReverse)]
    public void AnimationDirection_ParsesCorrectly(string value, AnimationDirection expected)
    {
        var style = ApplyProp("animation-direction", value);
        Assert.Equal(expected, style.AnimationDirection);
    }

    [Theory]
    [InlineData("none", AnimationFillMode.None)]
    [InlineData("forwards", AnimationFillMode.Forwards)]
    [InlineData("backwards", AnimationFillMode.Backwards)]
    [InlineData("both", AnimationFillMode.Both)]
    public void AnimationFillMode_ParsesCorrectly(string value, AnimationFillMode expected)
    {
        var style = ApplyProp("animation-fill-mode", value);
        Assert.Equal(expected, style.AnimationFillMode);
    }

    [Fact]
    public void AnimationDelay_Parses()
    {
        var style = ApplyProp("animation-delay", "500ms");
        Assert.Equal(0.5f, style.AnimationDelay, 0.01f);
    }

    [Fact]
    public void AnimationTimingFunction_Parses()
    {
        var style = ApplyProp("animation-timing-function", "ease-in-out");
        Assert.Equal("ease-in-out", style.AnimationTimingFunction);
    }

    // ── Helper ──────────────────────────────────────────────────────

    private static ComputedStyle ApplyProp(string property, string value)
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.Apply(style, property, value);
        return style;
    }
}
