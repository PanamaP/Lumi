using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiToggle: thumb position math, click toggle semantics,
/// label visibility, track color update, and OnToggle callback receives the *new* value.
/// </summary>
public class LumiToggleTests
{
    // Geometry constants must mirror LumiToggle's internals.
    private const float TrackWidth = 44f;
    private const float ThumbSize = 20f;
    private const float ThumbMargin = 2f;

    [Fact]
    public void Initial_ThumbAtLeftMargin()
    {
        var t = new LumiToggle();
        var thumb = t.Root.Children[0].Children[0];
        Assert.Contains($"left: {ThumbMargin:F0}px", thumb.InlineStyle);
    }

    [Fact]
    public void TurnedOn_ThumbAtRightSide()
    {
        var t = new LumiToggle { IsOn = true };
        var thumb = t.Root.Children[0].Children[0];
        float expectedLeft = TrackWidth - ThumbSize - ThumbMargin; // 22
        Assert.Contains($"left: {expectedLeft:F0}px", thumb.InlineStyle);
    }

    [Fact]
    public void OnState_TrackUsesAccent()
    {
        var t = new LumiToggle { IsOn = true };
        var track = t.Root.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), track.InlineStyle);
    }

    [Fact]
    public void OffState_TrackUsesBorder()
    {
        var t = new LumiToggle { IsOn = false };
        var track = t.Root.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Border), track.InlineStyle);
    }

    [Fact]
    public void Click_TogglesAndPersistsState()
    {
        var t = new LumiToggle();
        Click(t.Root);
        Assert.True(t.IsOn);
        Click(t.Root);
        Assert.False(t.IsOn);
        Click(t.Root);
        Assert.True(t.IsOn);
    }

    [Fact]
    public void Click_FiresCallbackWithNewValueEachTime()
    {
        var t = new LumiToggle();
        var received = new List<bool>();
        t.OnToggle = v => received.Add(v);

        Click(t.Root);
        Click(t.Root);
        Click(t.Root);

        Assert.Equal(new[] { true, false, true }, received);
    }

    [Fact]
    public void Label_NullByDefaultAndLabelElementHidden()
    {
        var t = new LumiToggle();
        Assert.Null(t.Label);
        var label = t.Root.Children[1];
        Assert.Contains("display: none", label.InlineStyle);
    }

    [Fact]
    public void Label_SetToNonEmpty_BecomesVisible()
    {
        var t = new LumiToggle { Label = "Dark Mode" };
        var label = t.Root.Children[1];
        Assert.Equal("Dark Mode", t.Label);
        Assert.Contains("display: block", label.InlineStyle);
    }

    [Fact]
    public void Label_SetToEmpty_StaysHidden()
    {
        var t = new LumiToggle { Label = "" };
        var label = t.Root.Children[1];
        Assert.Contains("display: none", label.InlineStyle);
    }

    [Fact]
    public void Label_SetToNullAfterValue_ClearsAndHides()
    {
        var t = new LumiToggle { Label = "Visible" };
        t.Label = null;
        var label = (TextElement)t.Root.Children[1];
        Assert.Equal("", label.Text);
        Assert.Contains("display: none", label.InlineStyle);
    }

    [Fact]
    public void Setting_IsOnDirectly_UpdatesThumbWithoutCallback()
    {
        bool? received = null;
        var t = new LumiToggle { OnToggle = v => received = v };
        t.IsOn = true;

        var thumb = t.Root.Children[0].Children[0];
        float expectedLeft = TrackWidth - ThumbSize - ThumbMargin;
        Assert.Contains($"left: {expectedLeft:F0}px", thumb.InlineStyle);
        // Setting IsOn directly does NOT raise OnToggle (only click does).
        Assert.Null(received);
    }

    [Fact]
    public void Dispose_RemovesEventHandlers_NoOnToggleAfter()
    {
        bool fired = false;
        var t = new LumiToggle { OnToggle = _ => fired = true };
        t.Dispose();
        Click(t.Root);
        Assert.False(fired);
    }

    private static void Click(Element target)
    {
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, target);
    }
}
