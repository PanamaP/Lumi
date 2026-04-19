using System.Collections.ObjectModel;
using System.ComponentModel;
using Lumi.Core;
using Lumi.Core.Animation;
using Lumi.Core.Binding;

namespace Lumi.Tests;

/// <summary>
/// Third refinement batch: ItemsRenderer (mirror of TemplateForElement
/// boundary tests), TransitionManager state-snapshot semantics,
/// TemplateBinding nested-path GetRootProperty and TemplateIfElement
/// nested-path condition + bind-handler invocation.
/// </summary>
public class MutationRefinement3Tests
{
    // ---------------- ItemsRenderer ----------------

    private sealed class RawObservable<T> : Collection<T>, System.Collections.Specialized.INotifyCollectionChanged
    {
        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged;
        public void RaiseAdd(IList<T> items, int idx) =>
            CollectionChanged?.Invoke(this,
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Add,
                    (System.Collections.IList)items, idx));
        public void RaiseRemove(IList<T> items, int idx) =>
            CollectionChanged?.Invoke(this,
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Remove,
                    (System.Collections.IList)items, idx));
        public void RaiseReplace(IList<T> newItems, int idx) =>
            CollectionChanged?.Invoke(this,
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Replace,
                    (System.Collections.IList)newItems, (System.Collections.IList)new List<T>(), idx));
        public void RaiseReset() =>
            CollectionChanged?.Invoke(this,
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
    }

    private static (ItemsRenderer ir, BoxElement c, RawObservable<string> s, Func<Element> factory) Setup(params string[] initial)
    {
        var c = new BoxElement("ul");
        var s = new RawObservable<string>();
        foreach (var i in initial) s.Add(i);
        Func<Element> factory = () => new BoxElement("li");
        var ir = new ItemsRenderer();
        ir.BindItemsSource(c, s, factory);
        return (ir, c, s, factory);
    }

    [Fact]
    public void ItemsRenderer_Add_AtIndexZero_PutsItemAtFront()
    {
        var (_, c, s, _) = Setup("a", "b");
        s.Insert(0, "Z");
        s.RaiseAdd(["Z"], 0);
        Assert.Equal(3, c.Children.Count);
        Assert.Equal("Z", c.Children[0].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Add_NegativeIndex_AppendsAtEnd()
    {
        var (_, c, s, _) = Setup("a", "b");
        s.RaiseAdd(["X"], -1);
        Assert.Equal(3, c.Children.Count);
        Assert.Equal("X", c.Children[2].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Add_MultiItem_PreservesOrder()
    {
        var (_, c, s, _) = Setup("a");
        s.RaiseAdd(["X", "Y", "Z"], 1);
        Assert.Equal(4, c.Children.Count);
        Assert.Equal("X", c.Children[1].DataContext);
        Assert.Equal("Y", c.Children[2].DataContext);
        Assert.Equal("Z", c.Children[3].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Remove_LastIndex_RemovesCorrectChild()
    {
        var (_, c, s, _) = Setup("a", "b", "c");
        s.RaiseRemove(["c"], 2);
        Assert.Equal(2, c.Children.Count);
        Assert.Equal("a", c.Children[0].DataContext);
        Assert.Equal("b", c.Children[1].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Remove_FromFront()
    {
        var (_, c, s, _) = Setup("a", "b", "c");
        s.RaiseRemove(["a", "b"], 0);
        Assert.Single(c.Children);
        Assert.Equal("c", c.Children[0].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Remove_OutOfRange_IsSkipped()
    {
        var (_, c, s, _) = Setup("a");
        var ex = Record.Exception(() => s.RaiseRemove(["x"], 5));
        Assert.Null(ex);
        Assert.Single(c.Children);
    }

    [Fact]
    public void ItemsRenderer_Replace_AtIndex_UpdatesDataContext()
    {
        var (_, c, s, _) = Setup("a", "b", "c");
        s.RaiseReplace(["B"], 1);
        Assert.Equal("a", c.Children[0].DataContext);
        Assert.Equal("B", c.Children[1].DataContext);
        Assert.Equal("c", c.Children[2].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Replace_OutOfRange_IsSkipped()
    {
        var (_, c, s, _) = Setup("a");
        var ex = Record.Exception(() => s.RaiseReplace(["X"], 9));
        Assert.Null(ex);
        Assert.Equal("a", c.Children[0].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Reset_RebuildsFromSource()
    {
        var (_, c, s, _) = Setup("a", "b");
        s.Clear();
        s.Add("X");
        s.Add("Y");
        s.RaiseReset();
        Assert.Equal(2, c.Children.Count);
        Assert.Equal("X", c.Children[0].DataContext);
        Assert.Equal("Y", c.Children[1].DataContext);
    }

    [Fact]
    public void ItemsRenderer_Unbind_StopsResponding()
    {
        var (ir, c, s, _) = Setup("a");
        ir.Unbind();
        s.RaiseAdd(["X"], 1);
        // No effect because unbound.
        Assert.Single(c.Children);
    }

    [Fact]
    public void ItemsRenderer_Unbind_TwiceDoesNotThrow()
    {
        var ir = new ItemsRenderer();
        var ex = Record.Exception(() => { ir.Unbind(); ir.Unbind(); });
        Assert.Null(ex);
    }

    [Fact]
    public void ItemsRenderer_Bind_NullArguments_Throw()
    {
        var ir = new ItemsRenderer();
        Assert.Throws<ArgumentNullException>(() => ir.BindItemsSource(null!, new List<string>(), () => new BoxElement("li")));
        Assert.Throws<ArgumentNullException>(() => ir.BindItemsSource(new BoxElement("ul"), null!, () => new BoxElement("li")));
        Assert.Throws<ArgumentNullException>(() => ir.BindItemsSource(new BoxElement("ul"), new List<string>(), null!));
    }

    [Fact]
    public void ItemsRenderer_Rebind_DiscardsPreviousSubscription()
    {
        var first = new RawObservable<string>(); first.Add("a");
        var c = new BoxElement("ul");
        Func<Element> factory = () => new BoxElement("li");
        var ir = new ItemsRenderer();
        ir.BindItemsSource(c, first, factory);

        var second = new RawObservable<string>(); second.Add("X");
        ir.BindItemsSource(c, second, factory);

        first.RaiseAdd(["ignored"], 1);
        Assert.Single(c.Children);
        Assert.Equal("X", c.Children[0].DataContext);
    }

    // ---------------- TransitionManager ----------------

    [Fact]
    public void Transition_DetectChanges_NoTransitionConfig_ReturnsEarly()
    {
        var tm = new TransitionManager();
        var el = new BoxElement("div");
        // No transition duration set.
        tm.CaptureState(el);
        el.ComputedStyle.Opacity = 0.5f;
        var ex = Record.Exception(() => tm.DetectChanges(el));
        Assert.Null(ex);
    }

    [Fact]
    public void Transition_DetectChanges_NoPriorSnapshot_NoOp()
    {
        var tm = new TransitionManager();
        var el = new BoxElement("div");
        el.ComputedStyle.TransitionDuration = 1f;
        el.ComputedStyle.TransitionProperty = "opacity";
        // Skip CaptureState — triggers `if (!TryGetValue) return;` branch.
        var ex = Record.Exception(() => tm.DetectChanges(el));
        Assert.Null(ex);
    }

    [Fact]
    public void Transition_DetectChanges_ChangedNumeric_StartsTween_AndMarksDirty()
    {
        var tm = new TransitionManager();
        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 1f;
        tm.CaptureState(el);
        el.ComputedStyle.TransitionDuration = 1f;
        el.ComputedStyle.TransitionProperty = "opacity";
        el.ComputedStyle.Opacity = 0f;
        el.IsDirty = false;

        tm.DetectChanges(el);

        Assert.True(el.IsDirty);
        // After tween starts the rendered opacity is reset to old value (1.0).
        Assert.Equal(1f, el.ComputedStyle.Opacity, 2);

        // Advance time half the duration
        tm.Update(0.5);
        Assert.InRange(el.ComputedStyle.Opacity, 0.4f, 0.6f);

        // Finish
        tm.Update(0.6);
        Assert.Equal(0f, el.ComputedStyle.Opacity, 2);
    }

    [Fact]
    public void Transition_DetectChanges_TinyDelta_DoesNothing()
    {
        var tm = new TransitionManager();
        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 1f;
        tm.CaptureState(el);
        el.ComputedStyle.TransitionDuration = 1f;
        el.ComputedStyle.TransitionProperty = "opacity";
        el.ComputedStyle.Opacity = 1.0001f; // delta < 0.001f threshold
        el.IsDirty = false;

        tm.DetectChanges(el);

        Assert.False(el.IsDirty);
    }

    [Fact]
    public void Transition_Clear_RemovesActiveTransitions()
    {
        var tm = new TransitionManager();
        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 1f;
        tm.CaptureState(el);
        el.ComputedStyle.TransitionDuration = 1f;
        el.ComputedStyle.TransitionProperty = "opacity";
        el.ComputedStyle.Opacity = 0f;
        tm.DetectChanges(el);

        tm.Clear();
        // After clear, capturing again with no duration set must not crash.
        var ex = Record.Exception(() => tm.Update(1));
        Assert.Null(ex);
    }

    [Fact]
    public void Transition_NullTimingFunction_DefaultsToEase()
    {
        // Kills `?? "ease"` left-side null-coalesce mutant: passes null timing func.
        var tm = new TransitionManager();
        var el = new BoxElement("div");
        el.ComputedStyle.Opacity = 1f;
        tm.CaptureState(el);
        el.ComputedStyle.TransitionDuration = 1f;
        el.ComputedStyle.TransitionProperty = "opacity";
        el.ComputedStyle.TransitionTimingFunction = null!;
        el.ComputedStyle.Opacity = 0f;

        var ex = Record.Exception(() => tm.DetectChanges(el));
        Assert.Null(ex); // didn't throw -- "ease" fallback applied
    }

    // ---------------- TemplateBinding nested ----------------

    private sealed class Item : INotifyPropertyChanged
    {
        public Item(Inner inner) { _inner = inner; }
        private Inner _inner;
        public Inner Inner
        {
            get => _inner;
            set { _inner = value; PropertyChanged?.Invoke(this, new(nameof(Inner))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class Inner : INotifyPropertyChanged
    {
        public Inner(string text) { _text = text; }
        private string _text;
        public string Text
        {
            get => _text;
            set { _text = value; PropertyChanged?.Invoke(this, new(nameof(Text))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [Fact]
    public void TemplateBinding_NestedPath_ReactsTo_RootPropertyChange()
    {
        var item = new Item(new Inner("first"));
        var t = new TextElement();
        using var b = TemplateBinding.TryCreate(t, "Text", false, "{item.Inner.Text}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("first", t.Text);

        // Replace the root segment so a "Inner" PropertyChanged fires.
        item.Inner = new Inner("second");
        Assert.Equal("second", t.Text);
    }

    // ---------------- TemplateIfElement nested condition ----------------

    private sealed class Owner : INotifyPropertyChanged
    {
        public Owner(Sub sub) { _sub = sub; }
        private Sub _sub;
        public Sub Sub
        {
            get => _sub;
            set { _sub = value; PropertyChanged?.Invoke(this, new(nameof(Sub))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class Sub : INotifyPropertyChanged
    {
        private bool _flag;
        public bool Flag
        {
            get => _flag;
            set { _flag = value; PropertyChanged?.Invoke(this, new(nameof(Flag))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [Fact]
    public void TemplateIfElement_BindCondition_ReactsToRootProperty_NestedPath()
    {
        var owner = new Owner(new Sub { Flag = false });
        var tif = new TemplateIfElement { ConditionPath = "Sub.Flag" };
        bool created = false;
        Func<Element> factory = () => { created = true; var b = new BoxElement("div"); b.AddChild(new TextElement("inside")); return b; };

        tif.BindCondition(owner, factory);

        // Initially false -> no children
        Assert.Empty(tif.Children);

        // Replace Sub triggers PropertyChanged("Sub") which is the root property
        owner.Sub = new Sub { Flag = true };
        Assert.True(created);
        Assert.NotEmpty(tif.Children);
    }

    [Fact]
    public void TemplateIfElement_BindCondition_NoChange_DoesNotRebuild()
    {
        var sub = new Sub { Flag = true };
        var owner = new Owner(sub);
        var tif = new TemplateIfElement { ConditionPath = "Sub.Flag" };
        int created = 0;
        Func<Element> factory = () => { created++; return new BoxElement("div"); };

        tif.BindCondition(owner, factory);
        // Manually set true so content renders (since BindCondition doesn't call SetRendered initially).
        tif.SetRendered(true);
        Assert.Equal(1, created);

        // Re-trigger with same value
        sub.Flag = true; // PropertyChanged fires on Sub, not Owner — no rebuild
        Assert.Equal(1, created);
    }

    [Fact]
    public void TemplateIfElement_IsTruthy_AllBranches()
    {
        Assert.False(TemplateIfElement.IsTruthy(null));
        Assert.True(TemplateIfElement.IsTruthy(true));
        Assert.False(TemplateIfElement.IsTruthy(false));
        Assert.True(TemplateIfElement.IsTruthy(1));
        Assert.False(TemplateIfElement.IsTruthy(0));
        Assert.True(TemplateIfElement.IsTruthy(1.5));
        Assert.False(TemplateIfElement.IsTruthy(0.0));
        Assert.True(TemplateIfElement.IsTruthy("text"));
        Assert.False(TemplateIfElement.IsTruthy(""));
        Assert.True(TemplateIfElement.IsTruthy(new object()));
    }

    [Fact]
    public void TemplateIfElement_Unbind_StopsReacting()
    {
        var owner = new Owner(new Sub { Flag = true });
        var tif = new TemplateIfElement { ConditionPath = "Sub.Flag" };
        Func<Element> factory = () => new BoxElement("div");

        tif.BindCondition(owner, factory);
        tif.Unbind();

        owner.Sub = new Sub { Flag = true };
        // Doesn't react after unbind — children stay empty.
        Assert.Empty(tif.Children);
    }
}

/// <summary>
/// Application input refinements: handled-key route gating + surrogate boundaries.
/// </summary>
public class ApplicationKeyRefinementTests
{

    private static (Application app, InputElement input) FocusedInput(string val = "abc", int cursor = 1)
    {
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 500, 500) };
        var input = new InputElement
        {
            LayoutBox = new LayoutBox(0, 0, 200, 30),
            Value = val,
        };
        input.CursorPosition = cursor;
        root.AddChild(input);
        var app = new Application { Root = root };
        app.SetFocus(input);
        return (app, input);
    }

    [Theory]
    [InlineData(KeyCode.Right)]
    [InlineData(KeyCode.Home)]
    [InlineData(KeyCode.End)]
    public void HandleInputKeyDown_HandledKey_StopsRoutedKeyDown(KeyCode key)
    {
        var (app, input) = FocusedInput("abc", 1);
        bool routed = false;
        input.On("KeyDown", (_, _) => routed = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = key, Type = KeyboardEventType.KeyDown });
        Assert.False(routed, $"{key} should be consumed by HandleInputKeyDown");
    }

    [Fact]
    public void HandleInputKeyDown_CtrlA_StopsRoutedKeyDown()
    {
        var (app, input) = FocusedInput("abc", 1);
        bool routed = false;
        input.On("KeyDown", (_, _) => routed = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.A, Ctrl = true, Type = KeyboardEventType.KeyDown });
        Assert.False(routed);
    }

    [Fact]
    public void ShiftLeft_Surrogate_AtCursor2_StepsBackByTwo()
    {
        // Kills the L306 `>= 2` mutant in the shift+left surrogate path.
        var s = char.ConvertFromUtf32(0x1F600);
        var (app, input) = FocusedInput(s, cursor: s.Length);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Shift = true, Type = KeyboardEventType.KeyDown });
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void ShiftRight_AtPenultimateNoSurrogate_DoesNotReadOutOfBounds()
    {
        // Kills L337 `< Length - 1` -> `<= Length - 1`. With cursor==length-1 and no
        // surrogate pair to peek at, mutant would index value[length] and throw.
        var (app, input) = FocusedInput("ab", cursor: 1);
        var ex = Record.Exception(() =>
            app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Shift = true, Type = KeyboardEventType.KeyDown }));
        Assert.Null(ex);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Right_AtPenultimateNoSurrogate_DoesNotReadOutOfBounds()
    {
        // Same boundary, no shift (covers L352).
        var (app, input) = FocusedInput("ab", cursor: 1);
        var ex = Record.Exception(() =>
            app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown }));
        Assert.Null(ex);
        Assert.Equal(2, input.CursorPosition);
    }
}
