using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiRadioGroup: bounds checking on SelectedIndex,
/// indicator visibility per row, callback firing, and dispose cleanup.
/// </summary>
public class LumiRadioGroupTests
{
    [Fact]
    public void Constructor_NullOptions_DoesNotThrow_AndProducesEmptyGroup()
    {
        var rg = new LumiRadioGroup(null!);
        Assert.Empty(rg.Options);
        Assert.Empty(rg.Root.Children);
    }

    [Fact]
    public void Constructor_FirstOptionSelectedAndIndicatorVisible()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        Assert.Equal(0, rg.SelectedIndex);
        AssertIndicatorVisible(rg, 0, true);
        AssertIndicatorVisible(rg, 1, false);
        AssertIndicatorVisible(rg, 2, false);
    }

    [Fact]
    public void SelectedIndex_BelowZero_IsRejected()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        rg.SelectedIndex = 1;
        rg.SelectedIndex = -1;
        Assert.Equal(1, rg.SelectedIndex); // unchanged
    }

    [Fact]
    public void SelectedIndex_OutOfRange_IsRejected()
    {
        var rg = new LumiRadioGroup(["A", "B"]);
        rg.SelectedIndex = 5;
        Assert.Equal(0, rg.SelectedIndex); // unchanged
    }

    [Fact]
    public void SelectedIndex_AtBoundary_LastValid_Accepted()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        rg.SelectedIndex = 2;
        Assert.Equal(2, rg.SelectedIndex);
        AssertIndicatorVisible(rg, 2, true);
    }

    [Fact]
    public void Click_OnRow_SwitchesIndicators()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        Click(rg.Root.Children[2]);
        AssertIndicatorVisible(rg, 0, false);
        AssertIndicatorVisible(rg, 1, false);
        AssertIndicatorVisible(rg, 2, true);
    }

    [Fact]
    public void Click_FiresCallbackOncePerSelection()
    {
        var rg = new LumiRadioGroup(["A", "B", "C"]);
        int callCount = 0;
        int? lastIdx = null;
        rg.OnSelectionChanged = idx => { callCount++; lastIdx = idx; };

        Click(rg.Root.Children[1]);
        Click(rg.Root.Children[2]);
        Click(rg.Root.Children[0]);

        Assert.Equal(3, callCount);
        Assert.Equal(0, lastIdx);
    }

    [Fact]
    public void Selected_CircleBorderUsesAccent_OthersUseBorder()
    {
        var rg = new LumiRadioGroup(["A", "B"]);
        rg.SelectedIndex = 1;

        var row1 = rg.Root.Children[1];
        var circle1 = row1.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), circle1.InlineStyle);

        var row0 = rg.Root.Children[0];
        var circle0 = row0.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Border), circle0.InlineStyle);
    }

    [Fact]
    public void Options_ListIsCopy_NotReferenceToInput()
    {
        var input = new List<string> { "X", "Y" };
        var rg = new LumiRadioGroup(input);
        input.Add("Z");
        Assert.Equal(2, rg.Options.Count);
    }

    [Fact]
    public void Dispose_RemovesHandlers_ClickIsNoOp()
    {
        var rg = new LumiRadioGroup(["A", "B"]);
        int count = 0;
        rg.OnSelectionChanged = _ => count++;

        rg.Dispose();
        Click(rg.Root.Children[1]);
        Assert.Equal(0, count);
    }

    [Fact]
    public void EmptyOptions_NoIndicatorsCreated_SelectedIndexUnchanged()
    {
        var rg = new LumiRadioGroup([]);
        Assert.Empty(rg.Root.Children);
        // Setting an index on an empty group is rejected.
        rg.SelectedIndex = 0;
        Assert.Equal(0, rg.SelectedIndex);
    }

    private static void AssertIndicatorVisible(LumiRadioGroup rg, int index, bool visible)
    {
        var row = rg.Root.Children[index];
        var circle = row.Children[0];
        var indicator = circle.Children[0];
        if (visible)
            Assert.Contains("display: block", indicator.InlineStyle);
        else
            Assert.Contains("display: none", indicator.InlineStyle);
    }

    private static void Click(Element target)
    {
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, target);
    }
}
