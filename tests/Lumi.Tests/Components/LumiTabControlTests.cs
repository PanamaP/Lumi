using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiTabControl: SelectedIndex bounds, header/text
/// styling per tab, content visibility, and the auto-select-first-tab heuristic.
/// </summary>
public class LumiTabControlTests
{
    [Fact]
    public void Default_NoTabs_SelectedIndexIsNegativeOne()
    {
        var tc = new LumiTabControl();
        Assert.Equal(-1, tc.SelectedIndex);
    }

    [Fact]
    public void AddTab_FirstTab_AutoSelected()
    {
        var tc = new LumiTabControl();
        tc.AddTab("First", new BoxElement("div"));
        Assert.Equal(0, tc.SelectedIndex);
    }

    [Fact]
    public void AddTab_SubsequentTabs_DoNotChangeSelection()
    {
        var tc = new LumiTabControl();
        tc.AddTab("a", new BoxElement("div"));
        tc.AddTab("b", new BoxElement("div"));
        tc.AddTab("c", new BoxElement("div"));
        Assert.Equal(0, tc.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_NegativeOrOutOfRange_Rejected()
    {
        var tc = new LumiTabControl();
        tc.AddTab("a", new BoxElement("div"));
        tc.AddTab("b", new BoxElement("div"));

        tc.SelectedIndex = -1;
        Assert.Equal(0, tc.SelectedIndex);
        tc.SelectedIndex = 5;
        Assert.Equal(0, tc.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_ChangesContentVisibility()
    {
        var tc = new LumiTabControl();
        var c1 = new BoxElement("div");
        var c2 = new BoxElement("div");
        tc.AddTab("a", c1);
        tc.AddTab("b", c2);

        // initial: a visible, b hidden
        Assert.DoesNotContain("display: none", c1.InlineStyle);
        Assert.Contains("display: none", c2.InlineStyle);

        tc.SelectedIndex = 1;
        Assert.Contains("display: none", c1.InlineStyle);
        Assert.DoesNotContain("display: none", c2.InlineStyle);
    }

    [Fact]
    public void Selected_HeaderUsesAccentBorderAndTextColor()
    {
        var tc = new LumiTabControl();
        tc.AddTab("a", new BoxElement("div"));
        tc.AddTab("b", new BoxElement("div"));

        // After selecting tab 1
        tc.SelectedIndex = 1;
        var headerRow = tc.Root.Children[0];
        var selectedHeader = headerRow.Children[1];
        var selectedText = selectedHeader.Children[0];

        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), selectedHeader.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.TextColor), selectedText.InlineStyle);

        var unselectedHeader = headerRow.Children[0];
        var unselectedText = unselectedHeader.Children[0];
        Assert.Contains("transparent", unselectedHeader.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Subtle), unselectedText.InlineStyle);
    }

    [Fact]
    public void Click_HeaderSwitchesSelectionAndFiresCallback()
    {
        var tc = new LumiTabControl();
        tc.AddTab("a", new BoxElement("div"));
        tc.AddTab("b", new BoxElement("div"));
        tc.AddTab("c", new BoxElement("div"));

        int? notified = null;
        tc.OnTabChanged = i => notified = i;

        var headerRow = tc.Root.Children[0];
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, headerRow.Children[2]);

        Assert.Equal(2, tc.SelectedIndex);
        Assert.Equal(2, notified);
    }

    [Fact]
    public void Dispose_DoesNotThrow_AndRemovesHeaderHandlers()
    {
        var tc = new LumiTabControl();
        tc.AddTab("a", new BoxElement("div"));
        tc.AddTab("b", new BoxElement("div"));

        int callCount = 0;
        tc.OnTabChanged = _ => callCount++;
        tc.Dispose();

        var headerRow = tc.Root.Children[0];
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, headerRow.Children[1]);
        Assert.Equal(0, callCount);
    }
}
