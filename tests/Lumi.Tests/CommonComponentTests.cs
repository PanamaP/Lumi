using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests;

public class CommonComponentTests
{
    [Fact]
    public void RadioGroup_SelectingOptionUpdatesSelectedIndex()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        Assert.Equal(0, rg.SelectedIndex);
        SimulateClick(rg.Root.Children[1]);
        Assert.Equal(1, rg.SelectedIndex);
    }

    [Fact]
    public void RadioGroup_SelectionFiresCallback()
    {
        var rg = new LumiRadioGroup(["X", "Y"]);
        int? received = null;
        rg.OnSelectionChanged = idx => received = idx;
        SimulateClick(rg.Root.Children[1]);
        Assert.Equal(1, received);
    }

    [Fact]
    public void RadioGroup_OnlyOneOptionSelected()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        SimulateClick(rg.Root.Children[1]);
        Assert.Equal(1, rg.SelectedIndex);
        SimulateClick(rg.Root.Children[2]);
        Assert.Equal(2, rg.SelectedIndex);
    }

    [Fact]
    public void RadioGroup_SetSelectedIndexUpdatesState()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        rg.SelectedIndex = 2;
        Assert.Equal(2, rg.SelectedIndex);
    }

    [Fact]
    public void Toggle_ClickTogglesIsOn()
    {
        var toggle = new LumiToggle();
        Assert.False(toggle.IsOn);
        SimulateClick(toggle.Root);
        Assert.True(toggle.IsOn);
    }

    [Fact]
    public void Toggle_DoubleClickReturnsFalse()
    {
        var toggle = new LumiToggle();
        SimulateClick(toggle.Root);
        SimulateClick(toggle.Root);
        Assert.False(toggle.IsOn);
    }

    [Fact]
    public void Toggle_OnToggleCallbackFiresWithCorrectValue()
    {
        var toggle = new LumiToggle();
        bool? received = null;
        toggle.OnToggle = v => received = v;
        SimulateClick(toggle.Root);
        Assert.True(received);
    }

    [Fact]
    public void Toggle_LabelCanBeSet()
    {
        var toggle = new LumiToggle { Label = "Dark Mode" };
        Assert.Equal("Dark Mode", toggle.Label);
    }

    [Fact]
    public void ProgressBar_ValueClampsBetweenZeroAndOne()
    {
        var pb = new LumiProgressBar();
        pb.Value = 1.5f;
        Assert.Equal(1.0f, pb.Value);
        pb.Value = -0.5f;
        Assert.Equal(0.0f, pb.Value);
    }

    [Fact]
    public void ProgressBar_DefaultValueIsZero()
    {
        var pb = new LumiProgressBar();
        Assert.Equal(0.0f, pb.Value);
    }

    [Fact]
    public void ProgressBar_IndeterminateModeCanBeSet()
    {
        var pb = new LumiProgressBar { IsIndeterminate = true };
        Assert.True(pb.IsIndeterminate);
    }

    [Fact]
    public void ProgressBar_ValueSetsWithinRange()
    {
        var pb = new LumiProgressBar { Value = 0.5f };
        Assert.Equal(0.5f, pb.Value);
    }

    [Fact]
    public void TabControl_AddTabAddsTab()
    {
        var tc = new LumiTabControl();
        var content = new TextElement("Page 1");
        tc.AddTab("Tab 1", content);
        var headerRow = tc.Root.Children[0];
        Assert.Single(headerRow.Children);
        var contentArea = tc.Root.Children[1];
        Assert.Single(contentArea.Children);
    }

    [Fact]
    public void TabControl_FirstTabAutoSelected()
    {
        var tc = new LumiTabControl();
        tc.AddTab("Tab 1", new TextElement("Content 1"));
        Assert.Equal(0, tc.SelectedIndex);
    }

    [Fact]
    public void TabControl_SelectingTabShowsContent()
    {
        var tc = new LumiTabControl();
        var c1 = new TextElement("Content 1");
        var c2 = new TextElement("Content 2");
        tc.AddTab("Tab 1", c1);
        tc.AddTab("Tab 2", c2);
        Assert.Equal(0, tc.SelectedIndex);
        Assert.DoesNotContain("display: none", c1.InlineStyle ?? "");
        Assert.Contains("display: none", c2.InlineStyle ?? "");
        var headerRow = tc.Root.Children[0];
        SimulateClick(FindChildByText(headerRow, "Tab 2")!);
        Assert.Equal(1, tc.SelectedIndex);
        Assert.Contains("display: none", c1.InlineStyle ?? "");
        Assert.DoesNotContain("display: none", c2.InlineStyle ?? "");
    }

    [Fact]
    public void TabControl_OnTabChangedFires()
    {
        var tc = new LumiTabControl();
        tc.AddTab("A", new TextElement("1"));
        tc.AddTab("B", new TextElement("2"));
        int? received = null;
        tc.OnTabChanged = idx => received = idx;
        var headerRow = tc.Root.Children[0];
        SimulateClick(FindChildByText(headerRow, "B")!);
        Assert.Equal(1, received);
    }

    [Fact]
    public void Tooltip_AttachCreatesTooltipElement()
    {
        var target = new BoxElement("div");
        var tooltip = LumiTooltip.Attach(target, "Help text");
        Assert.Equal("Help text", tooltip.Text);
        // Tooltip is not added to the tree until shown (on mouseenter)
        Assert.Null(tooltip.Root.Parent);
    }

    [Fact]
    public void Tooltip_StartsHidden()
    {
        var target = new BoxElement("div");
        var tooltip = LumiTooltip.Attach(target, "Info");
        // Tooltip starts without a parent (not in the element tree)
        Assert.Null(tooltip.Root.Parent);
    }

    [Fact]
    public void Tooltip_MouseEnterShowsTooltip()
    {
        var target = new BoxElement("div");
        var tooltip = LumiTooltip.Attach(target, "Info");
        EventDispatcher.Dispatch(new RoutedEvent("mouseenter"), target);
        Assert.DoesNotContain("display: none", tooltip.Root.InlineStyle ?? "");
    }

    [Fact]
    public void Tooltip_MouseLeaveHidesTooltip()
    {
        var target = new BoxElement("div");
        var tooltip = LumiTooltip.Attach(target, "Info");
        EventDispatcher.Dispatch(new RoutedEvent("mouseenter"), target);
        Assert.NotNull(tooltip.Root.Parent);
        EventDispatcher.Dispatch(new RoutedEvent("mouseleave"), target);
        // Tooltip is removed from the element tree on mouse leave
        Assert.Null(tooltip.Root.Parent);
    }

    private static Element? FindChildByText(Element parent, string text)
    {
        return parent.Children.FirstOrDefault(c => c is TextElement te && te.Text == text)
            ?? parent.Children.FirstOrDefault(c => c.Children.Any(gc => gc is TextElement te && te.Text == text));
    }

    private static void SimulateClick(Element target)
    {
        var e = new RoutedMouseEvent("click") { Button = MouseButton.Left };
        EventDispatcher.Dispatch(e, target);
    }
}
