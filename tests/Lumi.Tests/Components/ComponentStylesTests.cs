using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets ComponentStyles surviving mutants: ToRgba formatting, AppendStyle behaviour,
/// SetVisible toggling, GetAbsoluteBounds scroll math, and the per-variant button/style helpers.
/// </summary>
public class ComponentStylesTests
{
    // ── ToRgba ─────────────────────────────────────────────────────────

    [Fact]
    public void ToRgba_FullyOpaqueColor_FormatsAsOnePointZero()
    {
        var s = ComponentStyles.ToRgba(new Color(10, 20, 30, 255));
        Assert.Equal("rgba(10,20,30,1.00)", s);
    }

    [Fact]
    public void ToRgba_ZeroAlpha_FormatsAsZero()
    {
        var s = ComponentStyles.ToRgba(new Color(0, 0, 0, 0));
        Assert.Equal("rgba(0,0,0,0.00)", s);
    }

    [Fact]
    public void ToRgba_HalfAlpha_RoundsToTwoDecimals()
    {
        // 128/255 = 0.5019607...; format "F2" → "0.50"
        var s = ComponentStyles.ToRgba(new Color(255, 128, 64, 128));
        Assert.Equal("rgba(255,128,64,0.50)", s);
    }

    [Fact]
    public void ToRgba_MidValueComponentsPreserved()
    {
        var s = ComponentStyles.ToRgba(new Color(1, 2, 3, 255));
        // Each component must appear exactly — kills mutations that swap R/G/B
        Assert.Contains("rgba(1,2,3,", s);
    }

    // ── Color.FromHex round-trip ──────────────────────────────────────

    [Theory]
    [InlineData("000000", 0, 0, 0, 255)]
    [InlineData("FFFFFF", 255, 255, 255, 255)]
    [InlineData("FF0000", 255, 0, 0, 255)]
    [InlineData("00FF00", 0, 255, 0, 255)]
    [InlineData("0000FF", 0, 0, 255, 255)]
    [InlineData("123456", 0x12, 0x34, 0x56, 255)]
    public void FromHex_SixDigit_ParsesComponents(string hex, int r, int g, int b, int a)
    {
        var c = Color.FromHex(hex);
        Assert.Equal(r, c.R);
        Assert.Equal(g, c.G);
        Assert.Equal(b, c.B);
        Assert.Equal(a, c.A);
    }

    [Fact]
    public void FromHex_AcceptsLeadingHash()
    {
        var c = Color.FromHex("#1E293B");
        Assert.Equal(0x1E, c.R);
        Assert.Equal(0x29, c.G);
        Assert.Equal(0x3B, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void FromHex_ThreeDigit_ExpandsByMul17()
    {
        var c = Color.FromHex("ABC");
        Assert.Equal(0xAA, c.R); // 0xA * 17 = 0xAA
        Assert.Equal(0xBB, c.G);
        Assert.Equal(0xCC, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void FromHex_FourDigit_ExpandsAlpha()
    {
        var c = Color.FromHex("F00A");
        Assert.Equal(0xFF, c.R);
        Assert.Equal(0x00, c.G);
        Assert.Equal(0x00, c.B);
        Assert.Equal(0xAA, c.A);
    }

    [Fact]
    public void FromHex_EightDigit_IncludesAlpha()
    {
        var c = Color.FromHex("11223380");
        Assert.Equal(0x11, c.R);
        Assert.Equal(0x22, c.G);
        Assert.Equal(0x33, c.B);
        Assert.Equal(0x80, c.A);
    }

    [Fact]
    public void FromHex_InvalidLength_ReturnsBlack()
    {
        var c = Color.FromHex("12345"); // 5 chars
        Assert.Equal(Color.Black, c);
    }

    [Fact]
    public void FromHex_RoundTripWithToRgba_StableForOpaqueColors()
    {
        // round-trip through FromHex → ToRgba and check components are preserved.
        var c = Color.FromHex("FFAA33");
        Assert.Equal("rgba(255,170,51,1.00)", ComponentStyles.ToRgba(c));
    }

    // ── AppendStyle ───────────────────────────────────────────────────

    [Fact]
    public void AppendStyle_OnEmptyInline_SetsValueDirectly()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "";
        ComponentStyles.AppendStyle(el, "color: red");
        Assert.Equal("color: red", el.InlineStyle);
    }

    [Fact]
    public void AppendStyle_OnNullInline_SetsValueDirectly()
    {
        var el = new BoxElement("div");
        el.InlineStyle = null;
        ComponentStyles.AppendStyle(el, "color: red");
        Assert.Equal("color: red", el.InlineStyle);
    }

    [Fact]
    public void AppendStyle_AppendsWithSemicolonSeparator()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "padding: 4px";
        ComponentStyles.AppendStyle(el, "color: red");
        Assert.Equal("padding: 4px; color: red", el.InlineStyle);
    }

    [Fact]
    public void AppendStyle_TrimsTrailingSemicolonAndSpace()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "padding: 4px; ";
        ComponentStyles.AppendStyle(el, "color: red");
        Assert.Equal("padding: 4px; color: red", el.InlineStyle);
    }

    // ── ApplyButton variants ──────────────────────────────────────────

    [Fact]
    public void ApplyButton_Primary_UsesAccentBackground()
    {
        var el = new BoxElement("button");
        ComponentStyles.ApplyButton(el, ButtonVariant.Primary);
        Assert.Contains("--accent", el.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), el.InlineStyle);
    }

    [Fact]
    public void ApplyButton_Danger_UsesDangerColor()
    {
        var el = new BoxElement("button");
        ComponentStyles.ApplyButton(el, ButtonVariant.Danger);
        Assert.Contains("--error", el.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Danger), el.InlineStyle);
    }

    [Fact]
    public void ApplyButton_Secondary_UsesSurfaceColor()
    {
        var el = new BoxElement("button");
        ComponentStyles.ApplyButton(el, ButtonVariant.Secondary);
        Assert.Contains("--bg-tertiary", el.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Surface), el.InlineStyle);
    }

    [Fact]
    public void ApplyDisabledButton_UsesDisabledTone()
    {
        var el = new BoxElement("button");
        ComponentStyles.ApplyDisabledButton(el);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Disabled), el.InlineStyle);
        Assert.Contains("opacity: 0.6", el.InlineStyle);
        Assert.Contains("cursor: default", el.InlineStyle);
    }

    [Fact]
    public void ApplyContainer_Row_SetsFlexDirectionRow()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyContainer(el, FlexDirection.Row);
        Assert.Contains("flex-direction: row", el.InlineStyle);
    }

    [Fact]
    public void ApplyContainer_DefaultColumn_SetsFlexDirectionColumn()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyContainer(el);
        Assert.Contains("flex-direction: column", el.InlineStyle);
    }

    [Fact]
    public void ApplyToggleTrack_OnState_UsesAccentColor()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyToggleTrack(el, true);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), el.InlineStyle);
    }

    [Fact]
    public void ApplyToggleTrack_OffState_UsesBorderColor()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyToggleTrack(el, false);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Border), el.InlineStyle);
        Assert.DoesNotContain(ComponentStyles.ToRgba(ComponentStyles.Accent), el.InlineStyle);
    }

    [Fact]
    public void ApplyProgressTrack_HasFullWidthAndHeight8()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyProgressTrack(el);
        Assert.Contains("width: 100%", el.InlineStyle);
        Assert.Contains("height: 8px", el.InlineStyle);
    }

    [Fact]
    public void ApplyTabHeader_HasBottomBorderOnly()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyTabHeader(el);
        Assert.Contains("border-width: 0px 0px 1px 0px", el.InlineStyle);
    }

    [Fact]
    public void ApplyListRow_HasBottomBorderAndCursorPointer()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyListRow(el);
        Assert.Contains("cursor: pointer", el.InlineStyle);
        Assert.Contains("border-width: 0px 0px 1px 0px", el.InlineStyle);
    }

    [Fact]
    public void ApplyTooltip_UsesHighZIndexAndPointerEventsNone()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyTooltip(el);
        Assert.Contains("z-index: 10000", el.InlineStyle);
        Assert.Contains("pointer-events: none", el.InlineStyle);
    }

    [Fact]
    public void ApplyDialogPanel_HasMinDimensions()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyDialogPanel(el);
        Assert.Contains("min-width: 300px", el.InlineStyle);
        Assert.Contains("min-height: 150px", el.InlineStyle);
    }

    [Fact]
    public void ApplyOverlay_PositionsFixedAndDarkens()
    {
        var el = new BoxElement("div");
        ComponentStyles.ApplyOverlay(el);
        Assert.Contains("position: fixed", el.InlineStyle);
        Assert.Contains("z-index: 1000", el.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Overlay), el.InlineStyle);
    }

    // ── SetVisible ────────────────────────────────────────────────────

    [Fact]
    public void SetVisible_False_AppendsDisplayNone()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "color: red";
        ComponentStyles.SetVisible(el, false);
        Assert.Contains("display: none", el.InlineStyle);
        Assert.Contains("color: red", el.InlineStyle);
    }

    [Fact]
    public void SetVisible_True_RemovesDisplayNone()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "color: red";
        ComponentStyles.SetVisible(el, false);
        Assert.Contains("display: none", el.InlineStyle);

        ComponentStyles.SetVisible(el, true);
        Assert.DoesNotContain("display: none", el.InlineStyle);
        Assert.Contains("color: red", el.InlineStyle);
    }

    [Fact]
    public void SetVisible_FalseTwice_DoesNotDuplicateDisplayNone()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "color: red";
        ComponentStyles.SetVisible(el, false);
        ComponentStyles.SetVisible(el, false);
        // Substring should appear at most once
        var idx = el.InlineStyle!.IndexOf("display: none", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        Assert.Equal(-1, el.InlineStyle.IndexOf("display: none", idx + 1, StringComparison.Ordinal));
    }

    [Fact]
    public void SetVisible_FalseFromEmpty_SetsExactString()
    {
        var el = new BoxElement("div");
        el.InlineStyle = "";
        ComponentStyles.SetVisible(el, false);
        Assert.Equal("display: none", el.InlineStyle);
    }
}
