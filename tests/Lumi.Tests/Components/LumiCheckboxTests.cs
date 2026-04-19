using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiCheckbox: click-toggles checked state,
/// indicator visibility, border color depends on checked, callback receives new value.
/// </summary>
public class LumiCheckboxTests
{
    [Fact]
    public void Default_IsUncheckedAndIndicatorHidden()
    {
        var c = new LumiCheckbox();
        Assert.False(c.IsChecked);
        var indicator = c.Root.Children[0].Children[0];
        Assert.Contains("display: none", indicator.InlineStyle);
    }

    [Fact]
    public void IsChecked_True_ShowsIndicatorWithBlock()
    {
        var c = new LumiCheckbox { IsChecked = true };
        var indicator = c.Root.Children[0].Children[0];
        Assert.Contains("display: block", indicator.InlineStyle);
    }

    [Fact]
    public void IsChecked_True_BorderColorIsAccent()
    {
        var c = new LumiCheckbox { IsChecked = true };
        var box = c.Root.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), box.InlineStyle);
    }

    [Fact]
    public void IsChecked_False_BorderColorIsBorder()
    {
        var c = new LumiCheckbox { IsChecked = true };
        c.IsChecked = false;
        var box = c.Root.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Border), box.InlineStyle);
    }

    [Fact]
    public void Click_TogglesAndFiresCallback()
    {
        var c = new LumiCheckbox();
        var seen = new List<bool>();
        c.OnChanged = v => seen.Add(v);

        Click(c.Root);
        Click(c.Root);
        Click(c.Root);

        Assert.Equal(new[] { true, false, true }, seen);
        Assert.True(c.IsChecked);
    }

    [Fact]
    public void Label_SettingUpdatesUnderlyingText()
    {
        var c = new LumiCheckbox { Label = "Accept Terms" };
        var labelEl = (TextElement)c.Root.Children[1];
        Assert.Equal("Accept Terms", labelEl.Text);
        Assert.Equal("Accept Terms", c.Label);
    }

    [Fact]
    public void Click_AfterDirectIsCheckedSet_TogglesFromCurrentState()
    {
        var c = new LumiCheckbox { IsChecked = true };
        Click(c.Root);
        Assert.False(c.IsChecked);
    }

    private static void Click(Element target)
    {
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, target);
    }
}
