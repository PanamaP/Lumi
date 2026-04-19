using Lumi.Core;
using Lumi.Core.DragDrop;

namespace Lumi.Tests;

/// <summary>
/// Unit-level coverage for <see cref="Application"/>: input dispatch, focus, drag,
/// hit-test (transform/scroll/clip), key handling and lifecycle. Targets surviving
/// mutants in Application.cs (the largest remaining offender after phase 1).
/// </summary>
public class ApplicationLifecycleTests : IDisposable
{
    public ApplicationLifecycleTests()
    {
        // Install a deterministic in-memory clipboard for the duration of each test.
        string? content = "";
        Clipboard.ResetForTesting();
        Clipboard.Initialize(() => content, t => content = t);
    }

    public void Dispose() => Clipboard.ResetForTesting();

    private static BoxElement Box(float x, float y, float w, float h, string tag = "div")
    {
        var el = new BoxElement(tag);
        el.LayoutBox = new LayoutBox(x, y, w, h);
        return el;
    }

    private static (Application app, BoxElement root) NewApp()
    {
        var root = Box(0, 0, 500, 500, "body");
        var app = new Application { Root = root };
        return (app, root);
    }

    // ---------------- lifecycle ----------------

    [Fact]
    public void Start_SetsIsRunning_True()
    {
        var (app, _) = NewApp();
        Assert.False(app.IsRunning);
        app.Start();
        Assert.True(app.IsRunning);
    }

    [Fact]
    public void RequestStop_ClearsIsRunning()
    {
        var (app, _) = NewApp();
        app.Start();
        app.RequestStop();
        Assert.False(app.IsRunning);
    }

    [Fact]
    public void IsDirty_DelegatesToRoot()
    {
        var (app, root) = NewApp();
        root.IsDirty = false;
        Assert.False(app.IsDirty);
        root.MarkDirty();
        Assert.True(app.IsDirty);
    }

    [Fact]
    public void Update_DoesNothingObservable()
    {
        var (app, _) = NewApp();
        var ex = Record.Exception(() => app.Update());
        Assert.Null(ex);
    }

    [Fact]
    public void DefaultRoot_IsBodyElement()
    {
        var app = new Application();
        Assert.NotNull(app.Root);
        Assert.Equal("body", app.Root.TagName);
    }

    // ---------------- ProcessInput / dispatch routing ----------------

    [Fact]
    public void ProcessInput_DispatchesAllEventsInList()
    {
        var (app, root) = NewApp();
        var child = Box(0, 0, 500, 500);
        root.AddChild(child);
        int moves = 0;
        child.On("MouseMove", (_, _) => moves++);

        app.ProcessInput([
            new MouseEvent { X = 10, Y = 10, Type = MouseEventType.Move },
            new MouseEvent { X = 20, Y = 20, Type = MouseEventType.Move },
            new MouseEvent { X = 30, Y = 30, Type = MouseEventType.Move },
        ]);

        Assert.Equal(3, moves);
    }

    [Fact]
    public void ProcessInputEvent_UnknownEventType_DoesNothing()
    {
        var (app, _) = NewApp();
        // Custom subtype that hits no case — should silently no-op, not throw.
        var ex = Record.Exception(() => app.ProcessInputEvent(new UnknownInputEvent()));
        Assert.Null(ex);
    }

    private sealed class UnknownInputEvent : InputEvent { }

    // ---------------- mouse routing & event payload ----------------

    [Fact]
    public void MouseDown_SetsFocus_OnFocusableTarget_AndDispatchesMouseDown()
    {
        var (app, root) = NewApp();
        var btn = Box(10, 10, 80, 30);
        btn.IsFocusable = true;
        root.AddChild(btn);

        RoutedMouseEvent? captured = null;
        btn.On("MouseDown", (_, e) => captured = (RoutedMouseEvent)e);

        app.ProcessInputEvent(new MouseEvent
        {
            X = 50, Y = 25, Button = MouseButton.Right, Type = MouseEventType.ButtonDown
        });

        Assert.Same(btn, app.FocusedElement);
        Assert.NotNull(captured);
        Assert.Equal(50, captured!.X);
        Assert.Equal(25, captured.Y);
        Assert.Equal(MouseButton.Right, captured.Button);
    }

    [Fact]
    public void MouseDown_OutsideAnyElement_DoesNothing()
    {
        var (app, root) = NewApp();
        // Make root itself not pointer-hittable so HitTest returns null.
        root.LayoutBox = new LayoutBox(0, 0, 0, 0);
        var ex = Record.Exception(() =>
            app.ProcessInputEvent(new MouseEvent { X = 999, Y = 999, Type = MouseEventType.ButtonDown }));
        Assert.Null(ex);
        Assert.Null(app.FocusedElement);
    }

    [Fact]
    public void MouseUp_DispatchesBothMouseUpAndClick_WithCorrectButton()
    {
        var (app, root) = NewApp();
        var btn = Box(0, 0, 100, 100);
        root.AddChild(btn);

        RoutedMouseEvent? upEvent = null;
        RoutedMouseEvent? clickEvent = null;
        btn.On("MouseUp", (_, e) => upEvent = (RoutedMouseEvent)e);
        btn.On("Click", (_, e) => clickEvent = (RoutedMouseEvent)e);

        app.ProcessInputEvent(new MouseEvent
        {
            X = 5, Y = 7, Button = MouseButton.Middle, Type = MouseEventType.ButtonUp
        });

        Assert.NotNull(upEvent);
        Assert.NotNull(clickEvent);
        Assert.Equal(MouseButton.Middle, upEvent!.Button);
        Assert.Equal(MouseButton.Middle, clickEvent!.Button);
        Assert.Equal(5, clickEvent.X);
        Assert.Equal(7, clickEvent.Y);
    }

    [Fact]
    public void MouseMove_DispatchesMouseMove_WithCoordinates()
    {
        var (app, root) = NewApp();
        var child = Box(0, 0, 200, 200);
        root.AddChild(child);

        RoutedMouseEvent? captured = null;
        child.On("MouseMove", (_, e) => captured = (RoutedMouseEvent)e);

        app.ProcessInputEvent(new MouseEvent { X = 13, Y = 17, Type = MouseEventType.Move });

        Assert.NotNull(captured);
        Assert.Equal(13, captured!.X);
        Assert.Equal(17, captured.Y);
    }

    // ---------------- hover enter/leave ----------------

    [Fact]
    public void Hover_FiresEnterAndLeave_WhenTargetChanges()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 100, 100);
        var b = Box(200, 0, 100, 100);
        root.AddChild(a);
        root.AddChild(b);

        var log = new List<string>();
        a.On("MouseEnter", (_, _) => log.Add("a-enter"));
        a.On("MouseLeave", (_, _) => log.Add("a-leave"));
        b.On("MouseEnter", (_, _) => log.Add("b-enter"));
        b.On("MouseLeave", (_, _) => log.Add("b-leave"));

        app.ProcessInputEvent(new MouseEvent { X = 50, Y = 50, Type = MouseEventType.Move });
        app.ProcessInputEvent(new MouseEvent { X = 50, Y = 50, Type = MouseEventType.Move }); // same target -> no new events
        app.ProcessInputEvent(new MouseEvent { X = 250, Y = 50, Type = MouseEventType.Move });

        Assert.Equal(new[] { "a-enter", "a-leave", "b-enter" }, log);
    }

    [Fact]
    public void Hover_SameTarget_DoesNotRedispatch()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 100, 100);
        root.AddChild(a);
        int enters = 0;
        a.On("MouseEnter", (_, _) => enters++);

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.Move });
        app.ProcessInputEvent(new MouseEvent { X = 20, Y = 20, Type = MouseEventType.Move });

        Assert.Equal(1, enters);
    }

    // ---------------- focus ----------------

    [Fact]
    public void SetFocus_WalksUp_ToNearestFocusableAncestor()
    {
        var (app, root) = NewApp();
        var outer = Box(0, 0, 100, 100);
        outer.IsFocusable = true;
        var inner = Box(0, 0, 50, 50);
        // inner is NOT focusable
        root.AddChild(outer);
        outer.AddChild(inner);

        app.SetFocus(inner);
        Assert.Same(outer, app.FocusedElement);
        Assert.True(outer.IsFocused);
    }

    [Fact]
    public void SetFocus_NoFocusableAncestor_LeavesFocusNull()
    {
        var (app, root) = NewApp();
        var leaf = Box(0, 0, 10, 10);
        root.AddChild(leaf);
        app.SetFocus(leaf);
        Assert.Null(app.FocusedElement);
    }

    [Fact]
    public void SetFocus_FiresBlurOnPrevious_AndFocusOnNew()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 50, 50); a.IsFocusable = true;
        var b = Box(60, 0, 50, 50); b.IsFocusable = true;
        root.AddChild(a); root.AddChild(b);

        var log = new List<string>();
        a.On("Blur", (_, _) => log.Add("a-blur"));
        a.On("Focus", (_, _) => log.Add("a-focus"));
        b.On("Blur", (_, _) => log.Add("b-blur"));
        b.On("Focus", (_, _) => log.Add("b-focus"));

        app.SetFocus(a);
        app.SetFocus(b);

        Assert.Equal(new[] { "a-focus", "a-blur", "b-focus" }, log);
        Assert.False(a.IsFocused);
        Assert.True(b.IsFocused);
    }

    [Fact]
    public void SetFocus_SameTarget_NoOps()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 50, 50); a.IsFocusable = true;
        root.AddChild(a);

        app.SetFocus(a);
        int focusEvents = 0;
        a.On("Focus", (_, _) => focusEvents++);
        a.On("Blur", (_, _) => focusEvents++);

        app.SetFocus(a); // already focused

        Assert.Equal(0, focusEvents);
    }

    [Fact]
    public void SetFocus_Null_ClearsAndBlursPrevious()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 50, 50); a.IsFocusable = true;
        root.AddChild(a);
        app.SetFocus(a);

        bool blurred = false;
        a.On("Blur", (_, _) => blurred = true);
        app.SetFocus(null);

        Assert.True(blurred);
        Assert.Null(app.FocusedElement);
        Assert.False(a.IsFocused);
    }

    // ---------------- ClearInputState ----------------

    [Fact]
    public void ClearInputState_ResetsFocusHoverDragWithoutDispatching()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 100, 100); a.IsFocusable = true; a.IsDraggable = true;
        root.AddChild(a);

        app.SetFocus(a);
        // Begin a drag
        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        app.ProcessInputEvent(new MouseEvent { X = 20, Y = 20, Type = MouseEventType.Move });
        Assert.True(app.DragState.IsDragging);

        bool blurFired = false;
        a.On("Blur", (_, _) => blurFired = true);

        app.ClearInputState();

        Assert.Null(app.FocusedElement);
        Assert.False(a.IsFocused);
        Assert.False(app.DragState.IsDragging);
        Assert.Null(app.DragState.Source);
        Assert.Null(app.DragState.Data);
        Assert.False(blurFired); // explicitly does NOT dispatch
    }

    // ---------------- keyboard routing ----------------

    [Fact]
    public void KeyDown_NoFocus_DispatchesToRoot_AsKeyDown()
    {
        var (app, root) = NewApp();
        RoutedKeyEvent? captured = null;
        root.On("KeyDown", (_, e) => captured = (RoutedKeyEvent)e);

        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.F1, Type = KeyboardEventType.KeyDown, Shift = true, Ctrl = true, Alt = true });

        Assert.NotNull(captured);
        Assert.Equal("KeyDown", captured!.Name);
        Assert.Equal(KeyCode.F1, captured.Key);
        Assert.True(captured.Shift);
        Assert.True(captured.Ctrl);
        Assert.True(captured.Alt);
    }

    [Fact]
    public void KeyUp_DispatchesAsKeyUp_ToFocusedTarget()
    {
        var (app, root) = NewApp();
        var btn = Box(0, 0, 50, 50); btn.IsFocusable = true;
        root.AddChild(btn);
        app.SetFocus(btn);

        string? eventName = null;
        btn.On("KeyUp", (_, e) => eventName = e.Name);
        root.On("KeyUp", (_, e) => eventName ??= e.Name);

        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Escape, Type = KeyboardEventType.KeyUp });

        Assert.Equal("KeyUp", eventName);
    }

    [Fact]
    public void DisabledInputElement_DoesNotConsumeKey()
    {
        var (app, root) = NewApp();
        var input = new InputElement { LayoutBox = new LayoutBox(0, 0, 100, 30), Value = "abc", CursorPosition = 1, IsDisabled = true };
        root.AddChild(input);
        app.SetFocus(input);

        bool routed = false;
        input.On("KeyDown", (_, _) => routed = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });

        // Disabled: the InputElement-specific handler is skipped, but the routed
        // KeyDown event is still dispatched (so generic listeners receive it).
        Assert.True(routed);
        Assert.Equal(1, input.CursorPosition); // built-in caret movement did NOT run
    }

    // ---------------- HandleInputKeyDown: Left / Right with selection & surrogates ----------------

    private static InputElement FocusedInput(out Application app, string value = "hello", int cursor = 5)
    {
        var (a, root) = NewApp();
        var input = new InputElement
        {
            LayoutBox = new LayoutBox(0, 0, 200, 30),
            Value = value,
        };
        input.CursorPosition = cursor;
        root.AddChild(input);
        a.SetFocus(input);
        app = a;
        return input;
    }

    [Fact]
    public void Left_Plain_MovesCursorBackOne()
    {
        var input = FocusedInput(out var app, "abcd", cursor: 3);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });
        Assert.Equal(2, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void Left_AtZero_StaysAtZero()
    {
        var input = FocusedInput(out var app, "abcd", cursor: 0);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void Left_OverSurrogatePair_MovesByTwo()
    {
        // "a" + 😀 (U+1F600 = surrogate pair) => length 3, cursor at end
        var s = "a" + char.ConvertFromUtf32(0x1F600);
        var input = FocusedInput(out var app, s, cursor: s.Length);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });
        Assert.Equal(1, input.CursorPosition); // skipped both halves
    }

    [Fact]
    public void Left_WithSelection_CollapsesToLowerEnd()
    {
        var input = FocusedInput(out var app, "abcdef", cursor: 5);
        input.SelectionStart = 4; input.SelectionEnd = 2;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown });
        Assert.Equal(2, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void Left_Shift_StartsSelection_AndExtends()
    {
        var input = FocusedInput(out var app, "hello", cursor: 3);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown, Shift = true });
        Assert.True(input.HasSelection);
        Assert.Equal(3, input.SelectionStart);
        Assert.Equal(2, input.SelectionEnd);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Left_Shift_SurrogatePair_MovesByTwo()
    {
        var s = "a" + char.ConvertFromUtf32(0x1F600);
        var input = FocusedInput(out var app, s, cursor: s.Length);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Left, Type = KeyboardEventType.KeyDown, Shift = true });
        Assert.Equal(1, input.CursorPosition);
        Assert.True(input.HasSelection);
        Assert.Equal(s.Length, input.SelectionStart);
        Assert.Equal(1, input.SelectionEnd);
    }

    [Fact]
    public void Right_Plain_MovesForwardOne()
    {
        var input = FocusedInput(out var app, "abcd", cursor: 1);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown });
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Right_AtEnd_StaysAtEnd()
    {
        var input = FocusedInput(out var app, "abcd", cursor: 4);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown });
        Assert.Equal(4, input.CursorPosition);
    }

    [Fact]
    public void Right_OverSurrogatePair_MovesByTwo()
    {
        var s = char.ConvertFromUtf32(0x1F600) + "z";
        var input = FocusedInput(out var app, s, cursor: 0);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown });
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Right_WithSelection_CollapsesToHigherEnd()
    {
        var input = FocusedInput(out var app, "abcdef", cursor: 1);
        input.SelectionStart = 4; input.SelectionEnd = 2;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown });
        Assert.Equal(4, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void Right_Shift_ExtendsSelection()
    {
        var input = FocusedInput(out var app, "hello", cursor: 1);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown, Shift = true });
        Assert.Equal(2, input.CursorPosition);
        Assert.Equal(1, input.SelectionStart);
        Assert.Equal(2, input.SelectionEnd);
    }

    [Fact]
    public void Right_Shift_SurrogatePair_MovesByTwo()
    {
        var s = char.ConvertFromUtf32(0x1F600) + "z";
        var input = FocusedInput(out var app, s, cursor: 0);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Right, Type = KeyboardEventType.KeyDown, Shift = true });
        Assert.Equal(2, input.CursorPosition);
    }

    // ---------------- Home / End ----------------

    [Fact]
    public void Home_GoesToStart_ClearsSelection()
    {
        var input = FocusedInput(out var app, "hello", cursor: 4);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Home, Type = KeyboardEventType.KeyDown });
        Assert.Equal(0, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void Home_Shift_SelectsToStart()
    {
        var input = FocusedInput(out var app, "hello", cursor: 3);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Home, Type = KeyboardEventType.KeyDown, Shift = true });
        Assert.Equal(0, input.CursorPosition);
        Assert.Equal(3, input.SelectionStart);
        Assert.Equal(0, input.SelectionEnd);
    }

    [Fact]
    public void End_GoesToEnd_ClearsSelection()
    {
        var input = FocusedInput(out var app, "hello", cursor: 1);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.End, Type = KeyboardEventType.KeyDown });
        Assert.Equal(5, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void End_Shift_SelectsToEnd()
    {
        var input = FocusedInput(out var app, "hello", cursor: 1);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.End, Type = KeyboardEventType.KeyDown, Shift = true });
        Assert.Equal(5, input.CursorPosition);
        Assert.Equal(1, input.SelectionStart);
        Assert.Equal(5, input.SelectionEnd);
    }

    // ---------------- Ctrl-A / C / V / X ----------------

    [Fact]
    public void CtrlA_SelectsAll()
    {
        var input = FocusedInput(out var app, "hello", cursor: 0);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.A, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal(0, input.SelectionStart);
        Assert.Equal(5, input.SelectionEnd);
        Assert.Equal(5, input.CursorPosition);
    }

    [Fact]
    public void CtrlC_WithSelection_CopiesToClipboard()
    {
        Clipboard.SetText("");
        var input = FocusedInput(out var app, "hello", cursor: 4);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.C, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("ell", Clipboard.GetText());
    }

    [Fact]
    public void CtrlC_WithoutSelection_DoesNotChangeClipboard()
    {
        Clipboard.SetText("PRIOR");
        var input = FocusedInput(out var app, "hello", cursor: 2);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.C, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("PRIOR", Clipboard.GetText());
    }

    [Fact]
    public void CtrlV_PastesIntoInput_AndStripsNewlinesForSingleLine()
    {
        Clipboard.SetText("a\r\nb\nc\rd");
        var input = FocusedInput(out var app, "X", cursor: 1);
        bool inputFired = false;
        input.On("input", (_, _) => inputFired = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.V, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("Xa b c d", input.Value);
        Assert.True(inputFired);
        Assert.Equal(8, input.CursorPosition);
    }

    [Fact]
    public void CtrlV_TextArea_KeepsNewlines()
    {
        Clipboard.SetText("a\nb");
        var input = FocusedInput(out var app, "", cursor: 0);
        input.InputType = "textarea";
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.V, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("a\nb", input.Value);
    }

    [Fact]
    public void CtrlV_EmptyClipboard_NoOp()
    {
        Clipboard.SetText("");
        var input = FocusedInput(out var app, "abc", cursor: 1);
        bool fired = false;
        input.On("input", (_, _) => fired = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.V, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("abc", input.Value);
        Assert.False(fired);
    }

    [Fact]
    public void CtrlV_ReplacesSelection()
    {
        Clipboard.SetText("XYZ");
        var input = FocusedInput(out var app, "hello", cursor: 4);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.V, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("hXYZo", input.Value);
    }

    [Fact]
    public void CtrlX_WithSelection_CutsToClipboard()
    {
        Clipboard.SetText("");
        var input = FocusedInput(out var app, "hello", cursor: 4);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        bool fired = false;
        input.On("input", (_, _) => fired = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.X, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("ell", Clipboard.GetText());
        Assert.Equal("ho", input.Value);
        Assert.True(fired);
    }

    [Fact]
    public void CtrlX_WithoutSelection_NoOp()
    {
        Clipboard.SetText("PRIOR");
        var input = FocusedInput(out var app, "hello", cursor: 2);
        bool fired = false;
        input.On("input", (_, _) => fired = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.X, Type = KeyboardEventType.KeyDown, Ctrl = true });
        Assert.Equal("PRIOR", Clipboard.GetText());
        Assert.Equal("hello", input.Value);
        Assert.False(fired);
    }

    // ---------------- Backspace / Delete ----------------

    [Fact]
    public void Backspace_DeletesPreviousChar_AndDispatchesInput()
    {
        var input = FocusedInput(out var app, "abc", cursor: 2);
        bool fired = false;
        input.On("input", (_, _) => fired = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        Assert.Equal("ac", input.Value);
        Assert.Equal(1, input.CursorPosition);
        Assert.True(fired);
    }

    [Fact]
    public void Backspace_AtZero_NoChange()
    {
        var input = FocusedInput(out var app, "abc", cursor: 0);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        Assert.Equal("abc", input.Value);
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void Backspace_OverSurrogatePair_DeletesBothHalves()
    {
        var s = "a" + char.ConvertFromUtf32(0x1F600);
        var input = FocusedInput(out var app, s, cursor: s.Length);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        Assert.Equal("a", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void Backspace_WithSelection_DeletesSelection()
    {
        var input = FocusedInput(out var app, "hello", cursor: 4);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        Assert.Equal("ho", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void Delete_RemovesNextChar()
    {
        var input = FocusedInput(out var app, "abc", cursor: 1);
        bool fired = false;
        input.On("input", (_, _) => fired = true);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Delete, Type = KeyboardEventType.KeyDown });
        Assert.Equal("ac", input.Value);
        Assert.Equal(1, input.CursorPosition);
        Assert.True(fired);
    }

    [Fact]
    public void Delete_AtEnd_NoChange()
    {
        var input = FocusedInput(out var app, "abc", cursor: 3);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Delete, Type = KeyboardEventType.KeyDown });
        Assert.Equal("abc", input.Value);
    }

    [Fact]
    public void Delete_OverSurrogatePair_RemovesBothHalves()
    {
        var s = "a" + char.ConvertFromUtf32(0x1F600) + "z";
        var input = FocusedInput(out var app, s, cursor: 1);
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Delete, Type = KeyboardEventType.KeyDown });
        Assert.Equal("az", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void Delete_WithSelection_DeletesSelection()
    {
        var input = FocusedInput(out var app, "hello", cursor: 1);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        app.ProcessInputEvent(new KeyboardEvent { Key = KeyCode.Delete, Type = KeyboardEventType.KeyDown });
        Assert.Equal("ho", input.Value);
    }

    // ---------------- TextInput ----------------

    [Fact]
    public void TextInput_AppendsAtCursor_AndDispatchesInput()
    {
        var input = FocusedInput(out var app, "ac", cursor: 1);
        bool fired = false;
        input.On("input", (_, _) => fired = true);
        app.ProcessInputEvent(new TextInputEvent { Text = "b" });
        Assert.Equal("abc", input.Value);
        Assert.Equal(2, input.CursorPosition);
        Assert.True(fired);
    }

    [Fact]
    public void TextInput_ReplacesSelection()
    {
        var input = FocusedInput(out var app, "hello", cursor: 4);
        input.SelectionStart = 1; input.SelectionEnd = 4;
        app.ProcessInputEvent(new TextInputEvent { Text = "X" });
        Assert.Equal("hXo", input.Value);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void TextInput_NoFocus_DispatchesRoutedTextInputToRoot()
    {
        var (app, root) = NewApp();
        RoutedTextInputEvent? captured = null;
        root.On("TextInput", (_, e) => captured = (RoutedTextInputEvent)e);

        app.ProcessInputEvent(new TextInputEvent { Text = "+" });

        Assert.NotNull(captured);
        Assert.Equal("+", captured!.Text);
    }

    [Fact]
    public void TextInput_FocusedNonInput_DispatchesRoutedTextInput()
    {
        var (app, root) = NewApp();
        var btn = Box(0, 0, 50, 50); btn.IsFocusable = true;
        root.AddChild(btn);
        app.SetFocus(btn);

        RoutedTextInputEvent? captured = null;
        btn.On("TextInput", (_, e) => captured = (RoutedTextInputEvent)e);

        app.ProcessInputEvent(new TextInputEvent { Text = "Q" });
        Assert.NotNull(captured);
        Assert.Equal("Q", captured!.Text);
    }

    // ---------------- Scroll / FileDrop / Window ----------------

    [Fact]
    public void Scroll_AtElement_DispatchesScrollEvent()
    {
        var (app, root) = NewApp();
        var a = Box(0, 0, 100, 100);
        root.AddChild(a);
        bool fired = false;
        a.On("Scroll", (_, _) => fired = true);

        app.ProcessInputEvent(new ScrollEvent { X = 50, Y = 50, DeltaY = 1 });

        Assert.True(fired);
    }

    [Fact]
    public void Scroll_OffElement_NoDispatch()
    {
        var (app, root) = NewApp();
        root.LayoutBox = new LayoutBox(0, 0, 0, 0);
        bool fired = false;
        root.On("Scroll", (_, _) => fired = true);
        app.ProcessInputEvent(new ScrollEvent { X = 50, Y = 50, DeltaY = 1 });
        Assert.False(fired);
    }

    [Fact]
    public void FileDrop_AtElement_RaisesDropWithFiles()
    {
        var (app, root) = NewApp();
        var zone = Box(0, 0, 200, 200);
        root.AddChild(zone);

        DragData? captured = null;
        zone.OnDrop += d => captured = d;

        var files = new[] { "a.txt", "b.txt" };
        app.ProcessInputEvent(new FileDropEvent { X = 50, Y = 50, Files = files });

        Assert.NotNull(captured);
        Assert.Equal(files, captured!.Files);
    }

    [Fact]
    public void FileDrop_OffElement_NoOp()
    {
        var (app, root) = NewApp();
        root.LayoutBox = new LayoutBox(0, 0, 0, 0);
        var ex = Record.Exception(() =>
            app.ProcessInputEvent(new FileDropEvent { X = 50, Y = 50, Files = ["x"] }));
        Assert.Null(ex);
    }

    [Fact]
    public void Window_Close_RequestsStop()
    {
        var (app, _) = NewApp();
        app.Start();
        app.ProcessInputEvent(new WindowEvent { Type = WindowEventType.Close });
        Assert.False(app.IsRunning);
    }

    [Fact]
    public void Window_Close_DuringDrag_EndsDragCleanly()
    {
        var (app, root) = NewApp();
        var src = Box(0, 0, 100, 100); src.IsDraggable = true;
        root.AddChild(src);
        bool dragEnded = false;
        src.OnDragEnd += () => dragEnded = true;

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        app.ProcessInputEvent(new MouseEvent { X = 30, Y = 30, Type = MouseEventType.Move });
        Assert.True(app.DragState.IsDragging);

        app.ProcessInputEvent(new WindowEvent { Type = WindowEventType.Close });

        Assert.True(dragEnded);
        Assert.False(app.DragState.IsDragging);
        Assert.Null(app.DragState.Source);
    }

    [Fact]
    public void Window_Resized_DoesNotChangeRunning()
    {
        var (app, _) = NewApp();
        app.Start();
        app.ProcessInputEvent(new WindowEvent { Type = WindowEventType.Resized, Width = 800, Height = 600 });
        Assert.True(app.IsRunning);
    }

    // ---------------- Drag enter/leave/over ----------------

    [Fact]
    public void Drag_OverNewTarget_FiresLeaveAndEnter()
    {
        var (app, root) = NewApp();
        var src = Box(0, 0, 50, 50); src.IsDraggable = true;
        var t1 = Box(60, 0, 50, 50);
        var t2 = Box(120, 0, 50, 50);
        root.AddChild(src); root.AddChild(t1); root.AddChild(t2);

        var log = new List<string>();
        t1.OnDragEnter += _ => log.Add("t1-enter");
        t1.OnDragLeave += _ => log.Add("t1-leave");
        t2.OnDragEnter += _ => log.Add("t2-enter");
        t2.OnDragOver += _ => log.Add("t2-over");

        // Start drag at src
        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        // Cross threshold over t1
        app.ProcessInputEvent(new MouseEvent { X = 70, Y = 10, Type = MouseEventType.Move });
        // Move to t2
        app.ProcessInputEvent(new MouseEvent { X = 130, Y = 10, Type = MouseEventType.Move });

        Assert.Contains("t1-enter", log);
        Assert.Contains("t1-leave", log);
        Assert.Contains("t2-enter", log);
        Assert.Contains("t2-over", log);
    }

    [Fact]
    public void Drag_AtThresholdBoundary_StartsAtExactly5px()
    {
        var (app, root) = NewApp();
        var src = Box(0, 0, 100, 100); src.IsDraggable = true;
        root.AddChild(src);
        bool started = false;
        src.OnDragStart += _ => started = true;

        app.ProcessInputEvent(new MouseEvent { X = 0, Y = 0, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        // Move exactly 5 in x: dx*dx + dy*dy = 25 == DragThreshold^2 (>=)
        app.ProcessInputEvent(new MouseEvent { X = 5, Y = 0, Type = MouseEventType.Move });
        Assert.True(started);
    }

    [Fact]
    public void NonLeftButton_DoesNotInitiateDrag()
    {
        var (app, root) = NewApp();
        var src = Box(0, 0, 100, 100); src.IsDraggable = true;
        root.AddChild(src);
        bool started = false;
        src.OnDragStart += _ => started = true;

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Right });
        app.ProcessInputEvent(new MouseEvent { X = 30, Y = 30, Type = MouseEventType.Move });
        Assert.False(started);
    }

    [Fact]
    public void MouseUp_AfterPotentialDrag_ButNoDrag_ClearsPotential_AndDispatchesClick()
    {
        var (app, root) = NewApp();
        var src = Box(0, 0, 100, 100); src.IsDraggable = true;
        root.AddChild(src);
        int clicks = 0;
        src.On("Click", (_, _) => clicks++);

        app.ProcessInputEvent(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.ButtonDown, Button = MouseButton.Left });
        app.ProcessInputEvent(new MouseEvent { X = 11, Y = 10, Type = MouseEventType.Move }); // within threshold
        app.ProcessInputEvent(new MouseEvent { X = 11, Y = 10, Type = MouseEventType.ButtonUp, Button = MouseButton.Left });
        Assert.Equal(1, clicks);
        Assert.False(app.DragState.IsDragging);
    }

    // ---------------- Hit test transforms / scroll / clipping ----------------

    [Fact]
    public void HitTest_DisplayNone_ReturnsNull()
    {
        var root = Box(0, 0, 100, 100);
        root.ComputedStyle.Display = DisplayMode.None;
        Assert.Null(Application.HitTest(root, 10, 10));
    }

    [Fact]
    public void HitTest_VisibilityHidden_ReturnsNull()
    {
        var root = Box(0, 0, 100, 100);
        root.ComputedStyle.Visibility = Visibility.Hidden;
        Assert.Null(Application.HitTest(root, 10, 10));
    }

    [Fact]
    public void HitTest_PointerEventsFalse_ReturnsNullForLeaf()
    {
        var root = Box(0, 0, 100, 100);
        root.ComputedStyle.PointerEvents = false;
        Assert.Null(Application.HitTest(root, 10, 10));
    }

    [Fact]
    public void HitTest_TopMostChildWins()
    {
        var root = Box(0, 0, 200, 200);
        var below = Box(0, 0, 200, 200);
        var above = Box(0, 0, 200, 200);
        root.AddChild(below);
        root.AddChild(above);
        Assert.Same(above, Application.HitTest(root, 50, 50));
    }

    [Fact]
    public void HitTest_OverflowHidden_ClipsChildrenOutsideRoot()
    {
        var root = Box(0, 0, 50, 50);
        root.ComputedStyle.Overflow = Overflow.Hidden;
        var child = Box(100, 100, 200, 200);
        root.AddChild(child);
        // Point is within the child's box, but outside the clipping root: should miss.
        Assert.Null(Application.HitTest(root, 150, 150));
    }

    [Fact]
    public void HitTest_OverflowScroll_AppliesScrollOffset()
    {
        var root = Box(0, 0, 200, 200);
        root.ComputedStyle.Overflow = Overflow.Scroll;
        root.ScrollLeft = 100;
        root.ScrollTop = 50;
        var child = Box(120, 70, 30, 30); // visible window's (20..50, 20..50)
        root.AddChild(child);
        // Hit at (130, 80): childX = 130 + 100 = 230? No, childX = localX + ScrollLeft.
        // localX = 130, +100 = 230 — outside child(120..150). So pick a point inside (130, 80) actually:
        //   We want localX so that localX+ScrollLeft is in [120,150) and localY+ScrollTop in [70,100).
        //   childX = 25 + 100 = 125 (in range), childY = 25 + 50 = 75 (in range).
        var hit = Application.HitTest(root, 25, 25);
        Assert.Same(child, hit);
    }

    [Fact]
    public void HitTest_TransformTranslate_AffectsHit()
    {
        var root = Box(0, 0, 100, 100);
        root.ComputedStyle.Transform = new CssTransform(50, 0, 1, 1, 0, 0, 0);
        // Original box (0..100,0..100). With +50 translation, on-screen it occupies (50..150).
        // Inverse transform on hit (60,10): localX=60-origin(50)-tx(50)+origin=10; in original box.
        Assert.NotNull(Application.HitTest(root, 60, 10));
        Assert.Null(Application.HitTest(root, 5, 10));
    }

    [Fact]
    public void HitTest_TransformDegenerateScale_ReturnsNull()
    {
        var root = Box(0, 0, 100, 100);
        root.ComputedStyle.Transform = new CssTransform(0, 0, 0, 1, 0, 0, 0);
        Assert.Null(Application.HitTest(root, 50, 50));
    }

    [Fact]
    public void HitTest_TransformRotate90_HitsRotatedRegion()
    {
        var root = Box(0, 0, 100, 100); // origin = center (50,50)
        root.ComputedStyle.Transform = new CssTransform(0, 0, 1, 1, 90, 0, 0);
        // For a square the rotation around center maps the box onto itself; verify a hit lands
        Assert.NotNull(Application.HitTest(root, 50, 50));
    }

    [Fact]
    public void HitTest_TransformSkew_AppliesInverse()
    {
        var root = Box(0, 0, 100, 100);
        // small skew shouldn't displace center
        root.ComputedStyle.Transform = new CssTransform(0, 0, 1, 1, 0, 10, 5);
        Assert.NotNull(Application.HitTest(root, 50, 50));
    }

    // ---------------- MarkClean / snapshot ----------------

    [Fact]
    public void MarkClean_ClearsDirtyAndSnapshotsLayout()
    {
        var (app, root) = NewApp();
        var child = Box(10, 20, 30, 40);
        root.AddChild(child);
        root.MarkDirty();
        child.MarkDirty();
        Assert.True(root.IsDirty);

        app.MarkClean();

        Assert.False(root.IsDirty);
        Assert.False(child.IsDirty);
        Assert.False(root.IsLayoutDirty);
        Assert.False(child.IsLayoutDirty);
        // Snapshot: child is at (10,20) within root at (0,0) -> absolute (10,20)
        Assert.Equal(10, child.PreviousLayoutBox.X);
        Assert.Equal(20, child.PreviousLayoutBox.Y);
        Assert.Equal(30, child.PreviousLayoutBox.Width);
        Assert.Equal(40, child.PreviousLayoutBox.Height);
    }

    [Fact]
    public void MarkClean_NestedAbsoluteBox_AccumulatesAncestors()
    {
        var (app, root) = NewApp();
        var mid = Box(5, 6, 100, 100);
        var leaf = Box(7, 8, 10, 10);
        root.AddChild(mid);
        mid.AddChild(leaf);

        app.MarkClean();

        Assert.Equal(12, leaf.PreviousLayoutBox.X);
        Assert.Equal(14, leaf.PreviousLayoutBox.Y);
    }
}
