using Lumi.Core;
using Lumi.Styling;
using Xunit;

namespace Lumi.Tests;

public class ThemeTests
{
    // ── Mode defaults ──────────────────────────────────────────────

    [Fact]
    public void ThemeManager_DefaultsToSystemMode()
    {
        var tm = new ThemeManager();
        Assert.Equal(ThemeMode.System, tm.Mode);
    }

    [Fact]
    public void SetTheme_Light_IsDarkModeFalse()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);
        Assert.False(tm.IsDarkMode);
    }

    [Fact]
    public void SetTheme_Dark_IsDarkModeTrue()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Dark);
        Assert.True(tm.IsDarkMode);
    }

    [Fact]
    public void SystemMode_FollowsSystemPreference()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.System);

        tm.SetSystemPreference(true);
        Assert.True(tm.IsDarkMode);

        tm.SetSystemPreference(false);
        Assert.False(tm.IsDarkMode);
    }

    // ── Toggle ─────────────────────────────────────────────────────

    [Fact]
    public void Toggle_SwitchesBetweenLightAndDark()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);
        Assert.False(tm.IsDarkMode);

        tm.Toggle();
        Assert.True(tm.IsDarkMode);
        Assert.Equal(ThemeMode.Dark, tm.Mode);

        tm.Toggle();
        Assert.False(tm.IsDarkMode);
        Assert.Equal(ThemeMode.Light, tm.Mode);
    }

    [Fact]
    public void Toggle_FromSystemMode_SwitchesToOpposite()
    {
        var tm = new ThemeManager();
        // System defaults to light (systemDarkMode = false)
        Assert.False(tm.IsDarkMode);

        tm.Toggle(); // opposite of resolved false → go dark
        Assert.True(tm.IsDarkMode);
        Assert.Equal(ThemeMode.Dark, tm.Mode);
    }

    // ── ThemeChanged event ─────────────────────────────────────────

    [Fact]
    public void ThemeChanged_FiresOnModeChange()
    {
        var tm = new ThemeManager();
        bool? received = null;
        tm.ThemeChanged += dark => received = dark;

        tm.SetTheme(ThemeMode.Dark);
        Assert.True(received);

        received = null;
        tm.SetTheme(ThemeMode.Light);
        Assert.False(received);
    }

    [Fact]
    public void ThemeChanged_DoesNotFireWhenResolved_StaysSame()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);

        bool fired = false;
        tm.ThemeChanged += _ => fired = true;

        // System with systemDarkMode=false → still light → no event
        tm.SetTheme(ThemeMode.System);
        Assert.False(fired);
    }

    // ── ApplyTo sets CSS variables ─────────────────────────────────

    [Fact]
    public void ApplyTo_SetsCssVariablesOnRoot()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);
        var root = new BoxElement("body");

        tm.ApplyTo(root);

        Assert.NotNull(root.InlineStyle);
        Assert.Contains("--bg-primary: #ffffff", root.InlineStyle);
        Assert.Contains("--text-primary: #0f172a", root.InlineStyle);
        Assert.Contains("--accent: #3b82f6", root.InlineStyle);
    }

    [Fact]
    public void ApplyTo_DarkTheme_SetsDarkVariables()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Dark);
        var root = new BoxElement("body");

        tm.ApplyTo(root);

        Assert.NotNull(root.InlineStyle);
        Assert.Contains("--bg-primary: #0f172a", root.InlineStyle);
        Assert.Contains("--text-primary: #f8fafc", root.InlineStyle);
        Assert.Contains("--accent-hover: #60a5fa", root.InlineStyle);
    }

    [Fact]
    public void LightTheme_HasCorrectVariableValues()
    {
        var vars = ThemeManager.LightVariables;

        Assert.Equal("#ffffff", vars["--bg-primary"]);
        Assert.Equal("#f1f5f9", vars["--bg-secondary"]);
        Assert.Equal("#e2e8f0", vars["--bg-tertiary"]);
        Assert.Equal("#0f172a", vars["--text-primary"]);
        Assert.Equal("#475569", vars["--text-secondary"]);
        Assert.Equal("#94a3b8", vars["--text-muted"]);
        Assert.Equal("#cbd5e1", vars["--border-color"]);
        Assert.Equal("#3b82f6", vars["--accent"]);
        Assert.Equal("#2563eb", vars["--accent-hover"]);
        Assert.Equal("#ef4444", vars["--error"]);
        Assert.Equal("#22c55e", vars["--success"]);
        Assert.Equal("#f59e0b", vars["--warning"]);
    }

    [Fact]
    public void DarkTheme_HasCorrectVariableValues()
    {
        var vars = ThemeManager.DarkVariables;

        Assert.Equal("#0f172a", vars["--bg-primary"]);
        Assert.Equal("#1e293b", vars["--bg-secondary"]);
        Assert.Equal("#334155", vars["--bg-tertiary"]);
        Assert.Equal("#f8fafc", vars["--text-primary"]);
        Assert.Equal("#94a3b8", vars["--text-secondary"]);
        Assert.Equal("#64748b", vars["--text-muted"]);
        Assert.Equal("#475569", vars["--border-color"]);
        Assert.Equal("#3b82f6", vars["--accent"]);
        Assert.Equal("#60a5fa", vars["--accent-hover"]);
        Assert.Equal("#f87171", vars["--error"]);
        Assert.Equal("#4ade80", vars["--success"]);
        Assert.Equal("#fbbf24", vars["--warning"]);
    }

    // ── ApplyTo preserves user inline styles ───────────────────────

    [Fact]
    public void ApplyTo_PreservesExistingUserStyles()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);
        var root = new BoxElement("body") { InlineStyle = "padding: 10px" };

        tm.ApplyTo(root);

        Assert.Contains("padding: 10px", root.InlineStyle);
        Assert.Contains("--bg-primary: #ffffff", root.InlineStyle);
    }

    [Fact]
    public void ApplyTo_ReplacesPreviousThemeVars()
    {
        var tm = new ThemeManager();
        var root = new BoxElement("body");

        tm.SetTheme(ThemeMode.Light);
        tm.ApplyTo(root);
        Assert.Contains("--bg-primary: #ffffff", root.InlineStyle);

        tm.SetTheme(ThemeMode.Dark);
        tm.ApplyTo(root);
        Assert.Contains("--bg-primary: #0f172a", root.InlineStyle);
        Assert.DoesNotContain("#ffffff", root.InlineStyle);
    }

    // ── Style resolution integration ───────────────────────────────

    [Fact]
    public void ApplyTo_VariablesResolvedInComputedStyle()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);
        var root = new BoxElement("body");
        tm.ApplyTo(root);

        // Run style resolver to process the inline style
        var resolver = new StyleResolver();
        resolver.ResolveStyles(root);

        Assert.True(root.ComputedStyle.HasCustomProperties);
        Assert.Equal("#ffffff", root.ComputedStyle.CustomProperties["--bg-primary"]);
        Assert.Equal("#0f172a", root.ComputedStyle.CustomProperties["--text-primary"]);
    }

    [Fact]
    public void ThemeVariables_InheritToChildren()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Dark);

        var root = new BoxElement("body");
        var child = new BoxElement("div");
        root.AddChild(child);

        tm.ApplyTo(root);

        var resolver = new StyleResolver();
        resolver.ResolveStyles(root);

        Assert.True(child.ComputedStyle.HasCustomProperties);
        Assert.Equal("#0f172a", child.ComputedStyle.CustomProperties["--bg-primary"]);
        Assert.Equal("#f8fafc", child.ComputedStyle.CustomProperties["--text-primary"]);
    }

    [Fact]
    public void ThemeVariables_ResolveInChildStylesheetRules()
    {
        var tm = new ThemeManager();
        tm.SetTheme(ThemeMode.Light);

        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("themed");
        root.AddChild(child);

        tm.ApplyTo(root);

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(".themed { background-color: var(--bg-primary); }"));
        resolver.ResolveStyles(root);

        // The var(--bg-primary) should resolve to #ffffff → Color(255,255,255,255)
        Assert.Equal(new Color(255, 255, 255, 255), child.ComputedStyle.BackgroundColor);
    }

    // ── CurrentVariables ───────────────────────────────────────────

    [Fact]
    public void CurrentVariables_ReturnsCorrectPalette()
    {
        var tm = new ThemeManager();

        tm.SetTheme(ThemeMode.Light);
        Assert.Equal("#ffffff", tm.CurrentVariables["--bg-primary"]);

        tm.SetTheme(ThemeMode.Dark);
        Assert.Equal("#0f172a", tm.CurrentVariables["--bg-primary"]);
    }
}
