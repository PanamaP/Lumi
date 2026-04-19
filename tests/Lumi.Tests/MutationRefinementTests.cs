using System.ComponentModel;
using System.Collections.Specialized;
using Lumi.Core;
using Lumi.Core.Binding;
using Lumi.Core.Time;

namespace Lumi.Tests;

/// <summary>
/// Stryker-targeted refinements: kills statement-removal mutants on side
/// effects (MarkDirty, ResetBlink, EventDispatcher.Dispatch) by asserting
/// those side effects after each input key. Also covers a few BindingEngine
/// and TemplateForElement edges that the previous batch missed.
/// </summary>
/// <remarks>
/// Placed in the non-parallel "Dst" collection because the constructor mutates
/// <see cref="TimeSource.Default"/> — a process-wide global also exercised by
/// the deterministic-simulation tests under <c>Dst/</c>. Running concurrently
/// with those tests would cause flaky time-related failures.
/// </remarks>
[Xunit.Collection("Dst")]
public class MutationRefinementTests : IDisposable
{
    private readonly ITimeSource _previousTime;
    private readonly ManualTimeSource _clock = new();

    public MutationRefinementTests()
    {
        _previousTime = TimeSource.Default;
        TimeSource.Default = _clock;
    }

    public void Dispose()
    {
        TimeSource.Default = _previousTime;
    }

    private static (Application app, InputElement input) FocusedInputApp(string val = "hello", int cursor = 2)
    {
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 500, 500) };
        var input = new InputElement
        {
            LayoutBox = new LayoutBox(0, 0, 200, 30),
            Value = val,
        };
        input.CursorPosition = cursor;
        root.AddChild(input);
        var app = new Application { Root = input.Parent! };
        app.SetFocus(input);
        return (app, input);
    }

    // ---------------- Side-effect verification on every key path ----------------

    [Theory]
    [InlineData(KeyCode.Left, false, false)]
    [InlineData(KeyCode.Right, false, false)]
    [InlineData(KeyCode.Home, false, false)]
    [InlineData(KeyCode.End, false, false)]
    [InlineData(KeyCode.A, true, false)] // Ctrl+A
    public void Movement_Keys_ResetBlink_AndMarkDirty_AndDoNotDispatchInput(KeyCode key, bool ctrl, bool shift)
    {
        var (app, input) = FocusedInputApp("abcde", cursor: 2);
        _clock.Advance(1.0);
        input.LastEditTick = 0;
        input.IsDirty = false;
        bool inputFired = false;
        input.On("input", (_, _) => inputFired = true);

        app.ProcessInputEvent(new KeyboardEvent { Key = key, Type = KeyboardEventType.KeyDown, Ctrl = ctrl, Shift = shift });

        Assert.True(input.LastEditTick > 0, "ResetBlink should have set LastEditTick");
        Assert.True(input.IsDirty, "MarkDirty should have set IsDirty");
        Assert.False(inputFired, "Movement keys must not dispatch 'input'");
    }

    [Theory]
    [InlineData(KeyCode.Backspace)]
    [InlineData(KeyCode.Delete)]
    public void Mutating_Keys_DispatchInput_AndResetBlink_AndMarkDirty(KeyCode key)
    {
        var (app, input) = FocusedInputApp("abcde", cursor: 2);
        _clock.Advance(1.0);
        input.LastEditTick = 0;
        input.IsDirty = false;
        int inputFired = 0;
        input.On("input", (_, _) => inputFired++);

        app.ProcessInputEvent(new KeyboardEvent { Key = key, Type = KeyboardEventType.KeyDown });

        Assert.Equal(1, inputFired);
        Assert.True(input.LastEditTick > 0);
        Assert.True(input.IsDirty);
    }

    [Fact]
    public void TextInput_DispatchesInputEvent_AndMarksDirty_AndResetsBlink()
    {
        var (app, input) = FocusedInputApp("ab", cursor: 1);
        _clock.Advance(1.0);
        input.LastEditTick = 0;
        input.IsDirty = false;
        bool fired = false;
        input.On("input", (_, _) => fired = true);

        app.ProcessInputEvent(new TextInputEvent { Text = "Z" });

        Assert.True(fired);
        Assert.True(input.LastEditTick > 0);
        Assert.True(input.IsDirty);
    }

    // ---------------- Application: subtle equality / negate mutants ----------------

    [Fact]
    public void Left_Surrogate_AtCursor2_StepsBackByTwo()
    {
        // value "😀" length=2, cursor=2 — needs `>= 2` to be true so we read [0,1] and detect the pair.
        var s = char.ConvertFromUtf32(0x1F600);
        var (app, input) = FocusedInputApp(s, cursor: s.Length);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });
        Assert.Equal(0, input.CursorPosition); // moved by 2
    }

    [Fact]
    public void Right_Surrogate_AtPenultimate_StepsForwardByTwo()
    {
        // To trigger `< Length - 1`, cursor must be < Length-1 AND there must be
        // a surrogate pair starting at cursor. value = "😀" length=2, cursor=0 < 1, pair at [0,1].
        var s = char.ConvertFromUtf32(0x1F600);
        var (app, input) = FocusedInputApp(s, cursor: 0);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown });
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void HandleInputKeyDown_ReturnsFalse_For_UnhandledKey_RoutesToTreeAsKeyDown()
    {
        // Pressing F1 while focused on an input: HandleInputKeyDown returns false
        // (default branch), so the routed KeyDown still dispatches.
        var (app, input) = FocusedInputApp("a", cursor: 0);
        bool routed = false;
        input.On("KeyDown", (_, _) => routed = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.F1, Type = KeyboardEventType.KeyDown });
        Assert.True(routed, "Unhandled key should still dispatch routed KeyDown");
    }

    [Fact]
    public void HandleInputKeyDown_ReturnsTrue_For_HandledKey_StopsKeyDownRoute()
    {
        // Left key is handled => routed KeyDown should NOT fire afterward.
        var (app, input) = FocusedInputApp("ab", cursor: 1);
        bool routed = false;
        input.On("KeyDown", (_, _) => routed = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });
        Assert.False(routed);
    }

    [Fact]
    public void DragOver_OnSameTarget_DoesNotRefireEnterLeave()
    {
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 500, 500) };
        var src = new BoxElement("div") { LayoutBox = new LayoutBox(0, 0, 50, 50), IsDraggable = true };
        var t1 = new BoxElement("div") { LayoutBox = new LayoutBox(60, 0, 50, 50) };
        root.AddChild(src); root.AddChild(t1);
        var app = new Application { Root = root };

        int enters = 0, leaves = 0, overs = 0;
        t1.OnDragEnter += _ => enters++;
        t1.OnDragLeave += _ => leaves++;
        t1.OnDragOver += _ => overs++;

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        app.ProcessInputEvent(new MouseEvent { X = 70, Y = 10, Type = MouseEventType.Move }); // over t1 (drag start + enter)
        app.ProcessInputEvent(new MouseEvent { X = 80, Y = 10, Type = MouseEventType.Move }); // still over t1
        app.ProcessInputEvent(new MouseEvent { X = 90, Y = 10, Type = MouseEventType.Move }); // still over t1

        Assert.Equal(1, enters);
        Assert.Equal(0, leaves);
        Assert.True(overs >= 2, $"DragOver fires every frame inside same target (got {overs})");
    }

    [Fact]
    public void Drop_OnTarget_FiresDropAndDragEnd_ResetsState()
    {
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 500, 500) };
        var src = new BoxElement("div") { LayoutBox = new LayoutBox(0, 0, 50, 50), IsDraggable = true };
        var t1 = new BoxElement("div") { LayoutBox = new LayoutBox(60, 0, 50, 50) };
        root.AddChild(src); root.AddChild(t1);
        var app = new Application { Root = root };

        bool dropped = false; bool ended = false;
        t1.OnDrop += _ => dropped = true;
        src.OnDragEnd += () => ended = true;

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        app.ProcessInputEvent(new MouseEvent { X = 70, Y = 10, Type = MouseEventType.Move });
        app.ProcessInputEvent(new MouseEvent { X = 70, Y = 10, Type = MouseEventType.ButtonUp, Button = MouseButton.Left });

        Assert.True(dropped);
        Assert.True(ended);
        Assert.False(app.DragState.IsDragging);
        Assert.Null(app.DragState.Source);
        Assert.Null(app.DragState.Data);
    }

    // ---------------- Hover events also dispatch with coordinates ----------------

    [Fact]
    public void Hover_LeaveAndEnter_PassMouseCoordinates()
    {
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 500, 500) };
        var a = new BoxElement("div") { LayoutBox = new LayoutBox(0, 0, 100, 100) };
        var b = new BoxElement("div") { LayoutBox = new LayoutBox(200, 0, 100, 100) };
        root.AddChild(a); root.AddChild(b);
        var app = new Application { Root = root };

        RoutedMouseEvent? leaveA = null, enterB = null;
        a.On("MouseLeave", (_, e) => leaveA = (RoutedMouseEvent)e);
        b.On("MouseEnter", (_, e) => enterB = (RoutedMouseEvent)e);

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.Move });
        app.ProcessInputEvent(new MouseEvent { X = 250, Y = 70, Type = MouseEventType.Move });

        Assert.NotNull(leaveA);
        Assert.NotNull(enterB);
        Assert.Equal(250, leaveA!.X); // leave fires with the new mouse coords
        Assert.Equal(70, leaveA.Y);
        Assert.Equal(250, enterB!.X);
        Assert.Equal(70, enterB.Y);
    }

    // ---------------- BindingEngine: ClearAll on non-INPC source ----------------

    private sealed class PlainSource
    {
        public string Name { get; set; } = "Plain";
    }

    [Fact]
    public void ClearAll_NonNotifyingSource_DoesNotThrow()
    {
        var src = new PlainSource();
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", src, new BindingExpression { Path = "Name" });
        var ex = Record.Exception(() => engine.ClearAll());
        Assert.Null(ex);
    }

    [Fact]
    public void ClearAll_OneWayBindingToNonInputElement_NoReverseHandlerToUnhook()
    {
        // Covers the b.ReverseHandler == null branch in ClearAll.
        var src = new NotifyingNameSource("A");
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", src, new BindingExpression { Path = "Name", Mode = BindingMode.OneWay });
        engine.ClearAll();
        // After ClearAll, source change must not propagate.
        src.Name = "B";
        Assert.Equal("A", text.Text);
    }

    private sealed class NotifyingNameSource : INotifyPropertyChanged
    {
        public NotifyingNameSource(string name) { _name = name; }
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // ---------------- TemplateForElement: malformed event index handling ----------------

    [Fact]
    public void Add_WithNegativeIndex_AppendsAtEnd()
    {
        // Mutation: `(true?e.NewStartingIndex :Children.Count)` — covers the
        // ternary branch where NewStartingIndex < 0 falls through to Children.Count.
        var template = new TemplateForElement();
        var coll = new RawNotifyCollection<string>();
        Func<object, Element> factory = item => new BoxElement("li") { DataContext = item };
        template.AddChild(new BoxElement("li") { DataContext = "existing-0" });
        template.AddChild(new BoxElement("li") { DataContext = "existing-1" });
        template.BindCollection(coll, factory);

        coll.RaiseAdd(["X"], -1);

        Assert.Equal(3, template.Children.Count);
        Assert.Equal("X", template.Children[2].DataContext); // appended at end
    }

    [Fact]
    public void Remove_WithIndexBeyondChildren_IsSkipped()
    {
        // Covers `idx < Children.Count` boundary.
        var template = new TemplateForElement();
        var coll = new RawNotifyCollection<string>();
        Func<object, Element> factory = item => new BoxElement("li") { DataContext = item };
        template.AddChild(new BoxElement("li") { DataContext = "0" });
        template.BindCollection(coll, factory);

        // Try to remove index 5 of a 1-child template — should be a no-op, no exception.
        var ex = Record.Exception(() => coll.RaiseRemove(["x"], 5));
        Assert.Null(ex);
        Assert.Single(template.Children);
    }

    [Fact]
    public void Replace_WithIndexBeyondChildren_IsSkipped()
    {
        var template = new TemplateForElement();
        var coll = new RawNotifyCollection<string>();
        Func<object, Element> factory = item => new BoxElement("li") { DataContext = item };
        template.AddChild(new BoxElement("li") { DataContext = "0" });
        template.BindCollection(coll, factory);

        var ex = Record.Exception(() => coll.RaiseReplace(["new"], 9));
        Assert.Null(ex);
        Assert.Single(template.Children);
        Assert.Equal("0", template.Children[0].DataContext); // unchanged
    }

    [Fact]
    public void Remove_StartingAt0_RemovesFromFront()
    {
        // Tests the `e.OldStartingIndex + i` arithmetic at index 0 boundary.
        var template = new TemplateForElement();
        var coll = new RawNotifyCollection<string>();
        Func<object, Element> factory = item => new BoxElement("li") { DataContext = item };
        for (int i = 0; i < 4; i++) template.AddChild(new BoxElement("li") { DataContext = $"item-{i}" });
        template.BindCollection(coll, factory);

        coll.RaiseRemove(["a", "b"], 0); // remove indices 0 and 1

        Assert.Equal(2, template.Children.Count);
        Assert.Equal("item-2", template.Children[0].DataContext);
        Assert.Equal("item-3", template.Children[1].DataContext);
    }

    /// <summary>
    /// A bare INotifyCollectionChanged source that lets us synthesize raw events
    /// with arbitrary indices (including invalid ones) for boundary testing.
    /// </summary>
    private sealed class RawNotifyCollection<T> : System.Collections.ObjectModel.Collection<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public void RaiseAdd(IList<T> items, int startIndex)
        {
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)items, startIndex));
        }
        public void RaiseRemove(IList<T> items, int startIndex)
        {
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (System.Collections.IList)items, startIndex));
        }
        public void RaiseReplace(IList<T> items, int startIndex)
        {
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                    (System.Collections.IList)items, (System.Collections.IList)new List<T>(), startIndex));
        }
    }

    // ---------------- KeyframePlayer refinements ----------------

    [Fact]
    public void KeyframePlayer_ZeroDuration_ImmediatelyJumpsTo100Percent_AndRemoves()
    {
        var anim = new Lumi.Core.Animation.KeyframeAnimation
        {
            Name = "z",
            Duration = 0f, // forces totalDuration = 0
            IterationCount = 1,
            Keyframes =
            [
                new Lumi.Core.Animation.Keyframe(0f, new Dictionary<string, string> { ["opacity"] = "0" }),
                new Lumi.Core.Animation.Keyframe(1f, new Dictionary<string, string> { ["opacity"] = "1" }),
            ]
        };
        var p = new Lumi.Core.Animation.KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        // duration arg <= 0 falls back to animation.Duration which is also 0.
        p.Play(el, "z", duration: 0f);
        p.Update(0.001f);
        Assert.Equal(0, p.ActiveCount);
        Assert.Equal(1f, el.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void KeyframePlayer_AlternateDirection_FirstIteration_RunsForward()
    {
        // Targets `currentIteration % 2 == 1` mutants: at iteration 0 the result must be false (forward).
        var anim = new Lumi.Core.Animation.KeyframeAnimation
        {
            Name = "alt",
            Duration = 1f,
            IterationCount = 2,
            Keyframes =
            [
                new Lumi.Core.Animation.Keyframe(0f, new Dictionary<string, string> { ["opacity"] = "0" }),
                new Lumi.Core.Animation.Keyframe(1f, new Dictionary<string, string> { ["opacity"] = "1" }),
            ]
        };
        var p = new Lumi.Core.Animation.KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        p.Play(el, "alt", 1f, iterationCount: 2, direction: Lumi.Core.Animation.AnimationDirection.Alternate);
        p.Update(0.25f);
        // Forward at t=0.25 => opacity ≈ 0.25
        Assert.InRange(el.ComputedStyle.Opacity, 0.2f, 0.3f);
    }

    [Fact]
    public void KeyframePlayer_AlternateReverse_SecondIteration_RunsForward()
    {
        var anim = new Lumi.Core.Animation.KeyframeAnimation
        {
            Name = "altr",
            Duration = 1f,
            IterationCount = 2,
            Keyframes =
            [
                new Lumi.Core.Animation.Keyframe(0f, new Dictionary<string, string> { ["opacity"] = "0" }),
                new Lumi.Core.Animation.Keyframe(1f, new Dictionary<string, string> { ["opacity"] = "1" }),
            ]
        };
        var p = new Lumi.Core.Animation.KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        p.Play(el, "altr", 1f, iterationCount: 2, direction: Lumi.Core.Animation.AnimationDirection.AlternateReverse);
        // Halfway through second iteration: forward => opacity ≈ 0.5
        p.Update(1.5f);
        Assert.InRange(el.ComputedStyle.Opacity, 0.4f, 0.6f);
    }

    [Fact]
    public void KeyframePlayer_BeforeAfterSamePercent_DoesNotDivideByZero()
    {
        // before == after (both at percent 0.5): segmentT must stay at 0 (no NaN).
        var anim = new Lumi.Core.Animation.KeyframeAnimation
        {
            Name = "single",
            Duration = 1f,
            Keyframes =
            [
                new Lumi.Core.Animation.Keyframe(0.5f, new Dictionary<string, string> { ["opacity"] = "0.7" }),
            ]
        };
        var p = new Lumi.Core.Animation.KeyframePlayer();
        p.Register(anim);
        var el = new BoxElement("div");
        p.Play(el, "single", 1f);
        p.Update(0.25f);
        Assert.Equal(0.7f, el.ComputedStyle.Opacity, 2);
    }
}
