using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiSlider: drag mouse handlers, normalized value math,
/// thumb left position, fill width, and Min/Max boundary clamping.
/// </summary>
public class LumiSliderTests
{
    private const float TrackWidth = 200f;
    private const float TrackHeight = 8f;
    private const float ThumbSize = 24f;

    private static void SetTrackLayout(LumiSlider s)
    {
        // The slider's UpdateValueFromPosition uses _track.LayoutBox; explicitly set it.
        var track = s.Root.Children[0];
        track.LayoutBox = new LayoutBox(0, 0, TrackWidth, TrackHeight);
    }

    private static (Element track, Element fill, Element thumb) Parts(LumiSlider s)
    {
        var track = s.Root.Children[0];
        var fill = track.Children[0];
        var thumb = s.Root.Children[1];
        return (track, fill, thumb);
    }

    [Fact]
    public void Default_FillWidthIsZero_AndThumbAtLeft()
    {
        var s = new LumiSlider();
        var (_, fill, thumb) = Parts(s);
        Assert.Contains("width: 0.0px", fill.InlineStyle);
        Assert.Contains("left: 0.0px", thumb.InlineStyle);
    }

    [Fact]
    public void ValueAtMax_FillWidthEqualsTrackWidth_AndThumbAtRightEdge()
    {
        var s = new LumiSlider();
        s.Value = 1;
        var (_, fill, thumb) = Parts(s);
        Assert.Contains("width: 200.0px", fill.InlineStyle);
        // thumbLeft = 1 * (200 - 24) = 176
        Assert.Contains("left: 176.0px", thumb.InlineStyle);
    }

    [Fact]
    public void ValueAtMid_HalfFill_AndThumbHalfwayMinusHalfThumb()
    {
        var s = new LumiSlider { Min = 0, Max = 1, Value = 0.5f };
        var (_, fill, thumb) = Parts(s);
        Assert.Contains("width: 100.0px", fill.InlineStyle);
        // thumbLeft = 0.5 * (200 - 24) = 88
        Assert.Contains("left: 88.0px", thumb.InlineStyle);
    }

    [Fact]
    public void NormalizedValue_RangeOffset_NotZeroBased()
    {
        // Set Max first so Min=10 doesn't trip ClampValue against the default Max=1.
        var s = new LumiSlider { Max = 20, Min = 10, Value = 15 };
        var (_, fill, _) = Parts(s);
        // (15-10)/(20-10) = 0.5 → fill = 100px
        Assert.Contains("width: 100.0px", fill.InlineStyle);
    }

    [Fact]
    public void NormalizedValue_DegenerateRange_ReturnsZero()
    {
        // When min == max, NormalizedValue should return 0 (avoid divide-by-zero).
        var s = new LumiSlider { Max = 5, Min = 5 };
        s.Value = 5;
        var (_, fill, _) = Parts(s);
        Assert.Contains("width: 0.0px", fill.InlineStyle);
    }

    [Fact]
    public void MouseDown_AtTrackStart_SetsValueToMin()
    {
        var s = new LumiSlider { Min = 0, Max = 100 };
        SetTrackLayout(s);
        float? lastReported = null;
        s.OnValueChanged = v => lastReported = v;

        var down = new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = 0, Y = 0 };
        EventDispatcher.Dispatch(down, s.Root);
        Assert.Equal(0f, s.Value);
        Assert.Equal(0f, lastReported);
    }

    [Fact]
    public void MouseDown_AtTrackEnd_SetsValueToMax()
    {
        var s = new LumiSlider { Min = 0, Max = 100 };
        SetTrackLayout(s);
        var down = new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = TrackWidth, Y = 0 };
        EventDispatcher.Dispatch(down, s.Root);
        Assert.Equal(100f, s.Value);
    }

    [Fact]
    public void MouseDown_AtTrackMidpoint_SetsValueToMidpoint()
    {
        var s = new LumiSlider { Min = 0, Max = 200 };
        SetTrackLayout(s);
        var down = new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = TrackWidth / 2f, Y = 0 };
        EventDispatcher.Dispatch(down, s.Root);
        Assert.Equal(100f, s.Value, 1);
    }

    [Fact]
    public void MouseMove_WithoutMouseDown_DoesNotChangeValue()
    {
        var s = new LumiSlider { Min = 0, Max = 100, Value = 25 };
        SetTrackLayout(s);
        var move = new RoutedMouseEvent("mousemove") { Button = MouseButton.Left, X = TrackWidth, Y = 0 };
        EventDispatcher.Dispatch(move, s.Root);
        Assert.Equal(25f, s.Value);
    }

    [Fact]
    public void MouseUp_StopsDragging_SubsequentMoveIgnored()
    {
        var s = new LumiSlider { Min = 0, Max = 100 };
        SetTrackLayout(s);

        EventDispatcher.Dispatch(new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = 50 }, s.Root);
        Assert.Equal(25f, s.Value, 1); // 50/200 * 100

        EventDispatcher.Dispatch(new RoutedMouseEvent("mouseup") { Button = MouseButton.Left, X = 50 }, s.Root);

        EventDispatcher.Dispatch(new RoutedMouseEvent("mousemove") { Button = MouseButton.Left, X = TrackWidth }, s.Root);
        // After mouseup, drag is off — value should not be 100.
        Assert.Equal(25f, s.Value, 1);
    }

    [Fact]
    public void MouseDown_PositionPastEnd_ClampsToMax()
    {
        var s = new LumiSlider { Min = 0, Max = 100 };
        SetTrackLayout(s);
        EventDispatcher.Dispatch(new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = TrackWidth + 50 }, s.Root);
        Assert.Equal(100f, s.Value);
    }

    [Fact]
    public void MouseDown_NegativePosition_ClampsToMin()
    {
        var s = new LumiSlider { Min = 0, Max = 100 };
        SetTrackLayout(s);
        EventDispatcher.Dispatch(new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = -25 }, s.Root);
        Assert.Equal(0f, s.Value);
    }

    [Fact]
    public void TrackWidth_Changing_UpdatesContainerWidth()
    {
        var s = new LumiSlider();
        s.TrackWidth = 300f;
        Assert.Contains("width: 300px", s.Root.InlineStyle);
    }

    [Fact]
    public void TrackWidth_BelowMinimum_ClampsToTwenty()
    {
        var s = new LumiSlider();
        s.TrackWidth = 10f;
        Assert.Equal(20f, s.TrackWidth);
        Assert.Contains("width: 20px", s.Root.InlineStyle);
    }

    [Fact]
    public void Drag_RaisesOnValueChangedForEachMove()
    {
        var s = new LumiSlider { Min = 0, Max = 100 };
        SetTrackLayout(s);
        var values = new List<float>();
        s.OnValueChanged = v => values.Add(v);

        EventDispatcher.Dispatch(new RoutedMouseEvent("mousedown") { Button = MouseButton.Left, X = 50 }, s.Root);
        EventDispatcher.Dispatch(new RoutedMouseEvent("mousemove") { Button = MouseButton.Left, X = 100 }, s.Root);
        EventDispatcher.Dispatch(new RoutedMouseEvent("mousemove") { Button = MouseButton.Left, X = 150 }, s.Root);

        Assert.Equal(3, values.Count);
        Assert.Equal(25f, values[0], 1);
        Assert.Equal(50f, values[1], 1);
        Assert.Equal(75f, values[2], 1);
    }
}
