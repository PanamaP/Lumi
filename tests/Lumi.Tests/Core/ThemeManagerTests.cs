using Lumi.Core;

namespace Lumi.Tests.Core;

/// <summary>
/// Targets surviving mutants in ThemeManager: Mode toggles ApplyTo behaviour,
/// theme variables are stripped from inline style and re-emitted as ThemeVariables,
/// SetSystemPreference only fires when in System mode, ThemeChanged event semantics.
/// </summary>
public class ThemeManagerTests
{
    [Fact]
    public void Default_Mode_IsSystem()
    {
        var tm = new ThemeManager();
        Assert.Equal(ThemeMode.System, tm.Mode);
    }

    [Fact]
    public void SetMode_Light_ResolvesToNotDark()
    {
        var tm = new ThemeManager();
        tm.Mode = ThemeMode.Light;
        Assert.False(tm.IsDarkMode);
        Assert.Same(ThemeManager.LightVariables, tm.CurrentVariables);
    }

    [Fact]
    public void SetMode_Dark_ResolvesToDark()
    {
        var tm = new ThemeManager();
        tm.Mode = ThemeMode.Dark;
        Assert.True(tm.IsDarkMode);
        Assert.Same(ThemeManager.DarkVariables, tm.CurrentVariables);
    }

    [Fact]
    public void Mode_SettingSameValue_SkipsRecalc()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        int callCount = 0;
        tm.ThemeChanged += _ => callCount++;
        tm.Mode = ThemeMode.Light;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ThemeChanged_FiresWithNewIsDarkValue_OnModeSwitch()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        bool? captured = null;
        tm.ThemeChanged += v => captured = v;
        tm.Mode = ThemeMode.Dark;
        Assert.True(captured);

        captured = null;
        tm.Mode = ThemeMode.Light;
        Assert.False(captured);
    }

    [Fact]
    public void Toggle_FlipsBetweenLightAndDark()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        tm.Toggle();
        Assert.Equal(ThemeMode.Dark, tm.Mode);
        tm.Toggle();
        Assert.Equal(ThemeMode.Light, tm.Mode);
    }

    [Fact]
    public void SetSystemPreference_OnlyAffectsResolvedDark_WhenInSystemMode()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        int events = 0;
        tm.ThemeChanged += _ => events++;
        tm.SetSystemPreference(true);
        Assert.False(tm.IsDarkMode);
        Assert.Equal(0, events);

        tm.Mode = ThemeMode.System; // now system follows preference
        // After Mode change to System, isDarkMode should be true (matching preference set above).
        Assert.True(tm.IsDarkMode);
    }

    [Fact]
    public void SetSystemPreference_SameValue_NoOp()
    {
        var tm = new ThemeManager();
        tm.SetSystemPreference(false);
        int events = 0;
        tm.ThemeChanged += _ => events++;
        tm.SetSystemPreference(false);
        Assert.Equal(0, events);
    }

    [Fact]
    public void ApplyTo_WritesThemeVariablesDictionary()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        var root = new BoxElement("div");
        tm.ApplyTo(root);
        Assert.NotNull(root.ThemeVariables);
        Assert.Equal(ThemeManager.LightVariables["--accent"], root.ThemeVariables["--accent"]);
    }

    [Fact]
    public void ApplyTo_StripsPreviouslyInjectedThemeVarsFromInlineStyle()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        var root = new BoxElement("div")
        {
            // pretend an old version stored vars inline alongside a user style
            InlineStyle = "color: red; --accent: #000000; padding: 4px"
        };
        tm.ApplyTo(root);
        Assert.NotNull(root.InlineStyle);
        Assert.DoesNotContain("--accent", root.InlineStyle!);
        Assert.Contains("color: red", root.InlineStyle);
        Assert.Contains("padding: 4px", root.InlineStyle);
    }

    [Fact]
    public void ApplyTo_AllThemeVariables_LeavesNullStyleWhenNothingElseRemains()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        var root = new BoxElement("div") { InlineStyle = "--accent: #000; --bg-primary: #fff" };
        tm.ApplyTo(root);
        Assert.True(string.IsNullOrEmpty(root.InlineStyle));
    }

    [Fact]
    public void ModeSwitch_AfterApplyTo_ReinvokesApplyAndUpdatesThemeVars()
    {
        var tm = new ThemeManager { Mode = ThemeMode.Light };
        var root = new BoxElement("div");
        tm.ApplyTo(root);
        Assert.Equal("#ffffff", root.ThemeVariables!["--bg-primary"]);

        tm.Mode = ThemeMode.Dark;
        Assert.Equal("#0f172a", root.ThemeVariables!["--bg-primary"]);
    }
}
