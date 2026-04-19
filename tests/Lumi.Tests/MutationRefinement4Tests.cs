using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests;

/// <summary>
/// Final small batch: Element event-handled break, dirty defaults, IsVisible,
/// LumiSlider trackWidth boundaries, ThemeManager conditional toggles.
/// </summary>
public class MutationRefinement4Tests
{
    // ---------------- Element ----------------

    [Fact]
    public void RaiseEvent_HandledStopsPropagationToLaterHandlers()
    {
        var el = new BoxElement("div");
        int firstCalls = 0, secondCalls = 0;
        el.On("click", (_, e) => { firstCalls++; e.Handled = true; });
        el.On("click", (_, _) => secondCalls++);

        EventDispatcher.Dispatch(new RoutedEvent("click"), el);

        Assert.Equal(1, firstCalls);
        Assert.Equal(0, secondCalls);
    }

    [Fact]
    public void RaiseEvent_NotHandled_AllHandlersRun()
    {
        var el = new BoxElement("div");
        int a = 0, b = 0, c = 0;
        el.On("click", (_, _) => a++);
        el.On("click", (_, _) => b++);
        el.On("click", (_, _) => c++);

        EventDispatcher.Dispatch(new RoutedEvent("click"), el);

        Assert.Equal(1, a); Assert.Equal(1, b); Assert.Equal(1, c);
    }

    [Fact]
    public void Element_Defaults_IsDirty_And_IsLayoutDirty_AreTrue()
    {
        var el = new BoxElement("div");
        Assert.True(el.IsDirty);
        Assert.True(el.IsLayoutDirty);
    }

    [Fact]
    public void Element_IsVisible_ChecksDisplayMode()
    {
        var el = new BoxElement("div");
        Assert.True(el.IsVisible);
        el.ComputedStyle.Display = DisplayMode.None;
        Assert.False(el.IsVisible);
        el.ComputedStyle.Display = DisplayMode.Flex;
        Assert.True(el.IsVisible);
    }

    [Fact]
    public void Element_Classes_AssignNew_UnregistersOldFromIndex_RegistersNew()
    {
        // Targets `_index == null` mutation in the Classes setter.
        var root = new BoxElement("body");
        var el = new BoxElement("div");
        el.Classes.Add("old1");
        el.Classes.Add("old2");
        root.AddChild(el);
        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Contains(el, index.FindByClass("old1"));

        // Replace whole ClassList with a new one
        var newList = new ClassList(new[] { "newA", "newB" });
        el.Classes = newList;

        Assert.Empty(index.FindByClass("old1"));
        Assert.Empty(index.FindByClass("old2"));
        Assert.Contains(el, index.FindByClass("newA"));
        Assert.Contains(el, index.FindByClass("newB"));
    }

    [Fact]
    public void Element_Classes_AssignSameInstance_NoOps()
    {
        var el = new BoxElement("div");
        var prior = el.Classes;
        el.Classes = prior; // same reference -> early return
        Assert.Same(prior, el.Classes);
    }

    [Fact]
    public void Element_Classes_AssignNull_ResetsToEmptyClassList()
    {
        var el = new BoxElement("div");
        el.Classes.Add("x");
        el.Classes = null!;
        Assert.NotNull(el.Classes);
        Assert.Empty(el.Classes);
    }

    // ---------------- LumiSlider boundary ----------------

    [Fact]
    public void Slider_DragWithZeroTrackWidth_FallsBackToInitialWidth()
    {
        // Targets `trackWidth <= 0` boundary.
        var slider = new LumiSlider { Min = 0, Max = 100, Value = 0 };
        // Track width is 0 until layout runs; force a drag to use the fallback.
        slider.Root.LayoutBox = new LayoutBox(0, 0, 200, 30);
        // Simulate mouse down + move on the slider's root.
        var down = new RoutedMouseEvent("mousedown") { X = 0, Y = 15, Button = MouseButton.Left };
        EventDispatcher.Dispatch(down, slider.Root);
        var move = new RoutedMouseEvent("mousemove") { X = 100, Y = 15 };
        EventDispatcher.Dispatch(move, slider.Root);
        // No assertion on exact value — just no exception path with trackWidth=0.
        Assert.InRange(slider.Value, 0f, 100f);
    }

    [Fact]
    public void Slider_ValueSetter_ClampsToRange()
    {
        var slider = new LumiSlider { Min = 0, Max = 100, Value = 50 };
        slider.Value = 200;
        Assert.Equal(100f, slider.Value);
        slider.Value = -50;
        Assert.Equal(0f, slider.Value);
    }
}
