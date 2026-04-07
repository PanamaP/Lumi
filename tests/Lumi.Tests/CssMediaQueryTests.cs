using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssMediaQueryTests
{
    // ── @media parsing ──────────────────────────────────────────────

    [Fact]
    public void MediaQuery_ParsesMinWidth()
    {
        var sheet = CssParser.Parse(@"
            @media (min-width: 768px) {
                .container { width: 750px; }
            }
        ");

        Assert.Single(sheet.MediaRules);
        Assert.Equal(768, sheet.MediaRules[0].Condition.MinWidth);
        Assert.Single(sheet.MediaRules[0].Rules);
        Assert.Equal(".container", sheet.MediaRules[0].Rules[0].SelectorText);
    }

    [Fact]
    public void MediaQuery_ParsesMaxWidth()
    {
        var sheet = CssParser.Parse(@"
            @media (max-width: 600px) {
                .sidebar { display: none; }
            }
        ");

        Assert.Single(sheet.MediaRules);
        Assert.Equal(600, sheet.MediaRules[0].Condition.MaxWidth);
    }

    [Fact]
    public void MediaQuery_ParsesMinAndMaxWidth()
    {
        var sheet = CssParser.Parse(@"
            @media (min-width: 768px) and (max-width: 1024px) {
                .content { padding: 20px; }
            }
        ");

        Assert.Single(sheet.MediaRules);
        var condition = sheet.MediaRules[0].Condition;
        Assert.Equal(768, condition.MinWidth);
        Assert.Equal(1024, condition.MaxWidth);
    }

    [Fact]
    public void MediaQuery_ParsesOrientation()
    {
        var sheet = CssParser.Parse(@"
            @media (orientation: portrait) {
                .layout { flex-direction: column; }
            }
        ");

        Assert.Single(sheet.MediaRules);
        Assert.Equal("portrait", sheet.MediaRules[0].Condition.Orientation);
    }

    [Fact]
    public void MediaQuery_ScreenAndCondition()
    {
        var sheet = CssParser.Parse(@"
            @media screen and (min-width: 1200px) {
                .wide { max-width: 1140px; }
            }
        ");

        Assert.Single(sheet.MediaRules);
        Assert.Equal(1200, sheet.MediaRules[0].Condition.MinWidth);
    }

    [Fact]
    public void MediaQuery_MultipleRulesInside()
    {
        var sheet = CssParser.Parse(@"
            @media (min-width: 768px) {
                .header { height: 80px; }
                .footer { height: 60px; }
            }
        ");

        Assert.Single(sheet.MediaRules);
        Assert.Equal(2, sheet.MediaRules[0].Rules.Count);
    }

    [Fact]
    public void MediaQuery_MixedWithRegularRules()
    {
        var sheet = CssParser.Parse(@"
            .always { color: red; }
            @media (min-width: 768px) {
                .responsive { color: blue; }
            }
            .also-always { color: green; }
        ");

        Assert.Equal(2, sheet.Rules.Count); // .always and .also-always
        Assert.Single(sheet.MediaRules);
    }

    // ── Condition evaluation ────────────────────────────────────────

    [Fact]
    public void MediaCondition_MinWidth_Matches()
    {
        var condition = new MediaCondition { MinWidth = 768 };
        Assert.True(condition.Evaluate(1024, 768));
        Assert.True(condition.Evaluate(768, 768)); // exact match
        Assert.False(condition.Evaluate(500, 768));
    }

    [Fact]
    public void MediaCondition_MaxWidth_Matches()
    {
        var condition = new MediaCondition { MaxWidth = 600 };
        Assert.True(condition.Evaluate(400, 768));
        Assert.True(condition.Evaluate(600, 768)); // exact match
        Assert.False(condition.Evaluate(800, 768));
    }

    [Fact]
    public void MediaCondition_MinMaxWidth_Matches()
    {
        var condition = new MediaCondition { MinWidth = 768, MaxWidth = 1024 };
        Assert.True(condition.Evaluate(900, 600));
        Assert.False(condition.Evaluate(500, 600));
        Assert.False(condition.Evaluate(1200, 600));
    }

    [Fact]
    public void MediaCondition_Portrait()
    {
        var condition = new MediaCondition { Orientation = "portrait" };
        Assert.True(condition.Evaluate(600, 800)); // h > w
        Assert.True(condition.Evaluate(600, 600)); // h == w
        Assert.False(condition.Evaluate(800, 600)); // w > h
    }

    [Fact]
    public void MediaCondition_Landscape()
    {
        var condition = new MediaCondition { Orientation = "landscape" };
        Assert.True(condition.Evaluate(800, 600)); // w > h
        Assert.False(condition.Evaluate(600, 800)); // h > w
    }

    // ── Integration: StyleResolver with @media ──────────────────────

    [Fact]
    public void StyleResolver_AppliesMediaRulesWhenConditionMet()
    {
        var sheet = CssParser.Parse(@"
            .box { width: 100px; }
            @media (min-width: 768px) {
                .box { width: 200px; }
            }
        ");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        root.Classes.Add("box");

        // Wide viewport: media rule applies
        resolver.SetViewport(1024, 768);
        resolver.ResolveStyles(root);
        Assert.Equal(200, root.ComputedStyle.Width);

        // Narrow viewport: media rule does not apply
        resolver.SetViewport(500, 768);
        resolver.ResolveStyles(root);
        Assert.Equal(100, root.ComputedStyle.Width);
    }

    [Fact]
    public void StyleResolver_DoesNotApplyMediaRulesWhenConditionNotMet()
    {
        var sheet = CssParser.Parse(@"
            .box { color: red; }
            @media (max-width: 600px) {
                .box { color: blue; }
            }
        ");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        root.Classes.Add("box");

        // Wide viewport: media rule should not apply
        resolver.SetViewport(1024, 768);
        resolver.ResolveStyles(root);
        Assert.Equal(new Color(255, 0, 0, 255), root.ComputedStyle.Color);
    }

    // ── @keyframes parsing ──────────────────────────────────────────

    [Fact]
    public void Keyframes_ParsesBasicAnimation()
    {
        var sheet = CssParser.Parse(@"
            @keyframes fadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
        ");

        Assert.True(sheet.Keyframes.ContainsKey("fadeIn"));
        var kf = sheet.Keyframes["fadeIn"];
        Assert.Equal(2, kf.Frames.Count);
        Assert.Equal(0f, kf.Frames[0].Percent);
        Assert.Equal(1f, kf.Frames[1].Percent);
    }

    [Fact]
    public void Keyframes_ParsesPercentageStops()
    {
        var sheet = CssParser.Parse(@"
            @keyframes pulse {
                0% { opacity: 1; }
                50% { opacity: 0.5; }
                100% { opacity: 1; }
            }
        ");

        Assert.True(sheet.Keyframes.ContainsKey("pulse"));
        var kf = sheet.Keyframes["pulse"];
        Assert.Equal(3, kf.Frames.Count);
        Assert.Equal(0f, kf.Frames[0].Percent);
        Assert.Equal(0.5f, kf.Frames[1].Percent);
        Assert.Equal(1f, kf.Frames[2].Percent);
    }

    [Fact]
    public void Keyframes_ParsesMultipleProperties()
    {
        var sheet = CssParser.Parse(@"
            @keyframes slide {
                from { opacity: 0; width: 0px; }
                to { opacity: 1; width: 100px; }
            }
        ");

        var kf = sheet.Keyframes["slide"];
        Assert.Equal(2, kf.Frames[0].Declarations.Count);
        Assert.Equal("opacity", kf.Frames[0].Declarations[0].Property);
        Assert.Equal("width", kf.Frames[0].Declarations[1].Property);
    }

    [Fact]
    public void Keyframes_CoexistsWithRegularRules()
    {
        var sheet = CssParser.Parse(@"
            .box { width: 100px; }
            @keyframes spin { from { opacity: 0; } to { opacity: 1; } }
            .other { height: 50px; }
        ");

        Assert.Equal(2, sheet.Rules.Count);
        Assert.Single(sheet.Keyframes);
    }
}
