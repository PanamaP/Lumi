using Lumi.Core;

namespace Lumi.Tests;

public class TextEditingTests
{
    private static Application CreateAppWithFocusedInput(InputElement input)
    {
        var root = new BoxElement("div");
        root.AddChild(input);
        root.LayoutBox = new LayoutBox(0, 0, 800, 600);
        input.LayoutBox = new LayoutBox(10, 10, 200, 30);

        var app = new Application();
        app.Root = root;
        app.Start();

        // Focus the input via mouse click
        app.ProcessInput([
            new MouseEvent { Type = MouseEventType.ButtonDown, X = 20, Y = 20, Button = MouseButton.Left },
            new MouseEvent { Type = MouseEventType.ButtonUp, X = 20, Y = 20, Button = MouseButton.Left }
        ]);

        return app;
    }

    private static void SendKey(Application app, KeyCode key, bool shift = false, bool ctrl = false)
    {
        app.ProcessInput([new KeyboardEvent { Key = key, Type = KeyboardEventType.KeyDown, Shift = shift, Ctrl = ctrl }]);
    }

    private static void SendText(Application app, string text)
    {
        app.ProcessInput([new TextInputEvent { Text = text }]);
    }

    // ── Cursor positioning ────────────────────────────────────────────

    [Fact]
    public void CursorStartsAtEndOfInitialValue()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        // After focus, cursor should be at end (set by initial value assignment)
        // CursorPosition defaults to 0, but clamped on Value set
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void LeftArrow_MovesCursorLeft()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.Left);

        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void RightArrow_MovesCursorRight()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 2;
        input.ClearSelection();

        SendKey(app, KeyCode.Right);

        Assert.Equal(3, input.CursorPosition);
    }

    [Fact]
    public void LeftArrow_ClampsAtZero()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 0;
        input.ClearSelection();

        SendKey(app, KeyCode.Left);

        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void RightArrow_ClampsAtValueLength()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 5;
        input.ClearSelection();

        SendKey(app, KeyCode.Right);

        Assert.Equal(5, input.CursorPosition);
    }

    [Fact]
    public void Home_MovesCursorToZero()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.Home);

        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void End_MovesCursorToValueLength()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 2;
        input.ClearSelection();

        SendKey(app, KeyCode.End);

        Assert.Equal(5, input.CursorPosition);
    }

    // ── Typing / insert at cursor ─────────────────────────────────────

    [Fact]
    public void Typing_InsertsAtCursorPosition()
    {
        var input = new InputElement { Value = "hllo" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 1;
        input.ClearSelection();

        SendText(app, "e");

        Assert.Equal("hello", input.Value);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Typing_InsertsAtBeginning()
    {
        var input = new InputElement { Value = "ello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 0;
        input.ClearSelection();

        SendText(app, "h");

        Assert.Equal("hello", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void Typing_AppendsAtEnd()
    {
        var input = new InputElement { Value = "hell" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 4;
        input.ClearSelection();

        SendText(app, "o");

        Assert.Equal("hello", input.Value);
        Assert.Equal(5, input.CursorPosition);
    }

    // ── Backspace ─────────────────────────────────────────────────────

    [Fact]
    public void Backspace_DeletesCharBeforeCursor()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.Backspace);

        Assert.Equal("helo", input.Value);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Backspace_AtPositionZero_DoesNothing()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 0;
        input.ClearSelection();

        SendKey(app, KeyCode.Backspace);

        Assert.Equal("hello", input.Value);
        Assert.Equal(0, input.CursorPosition);
    }

    // ── Delete key ────────────────────────────────────────────────────

    [Fact]
    public void Delete_DeletesCharAtCursor()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 2;
        input.ClearSelection();

        SendKey(app, KeyCode.Delete);

        Assert.Equal("helo", input.Value);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void Delete_AtEnd_DoesNothing()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 5;
        input.ClearSelection();

        SendKey(app, KeyCode.Delete);

        Assert.Equal("hello", input.Value);
        Assert.Equal(5, input.CursorPosition);
    }

    // ── Selection ─────────────────────────────────────────────────────

    [Fact]
    public void ShiftRight_CreatesSelection()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 1;
        input.ClearSelection();

        SendKey(app, KeyCode.Right, shift: true);

        Assert.True(input.HasSelection);
        Assert.Equal(1, input.SelectionStart);
        Assert.Equal(2, input.SelectionEnd);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void ShiftLeft_CreatesSelection()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.Left, shift: true);

        Assert.True(input.HasSelection);
        Assert.Equal(3, input.SelectionStart);
        Assert.Equal(2, input.SelectionEnd);
        Assert.Equal(2, input.CursorPosition);
    }

    [Fact]
    public void ShiftHome_SelectsToStart()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.Home, shift: true);

        Assert.True(input.HasSelection);
        Assert.Equal(3, input.SelectionStart);
        Assert.Equal(0, input.SelectionEnd);
    }

    [Fact]
    public void ShiftEnd_SelectsToEnd()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 2;
        input.ClearSelection();

        SendKey(app, KeyCode.End, shift: true);

        Assert.True(input.HasSelection);
        Assert.Equal(2, input.SelectionStart);
        Assert.Equal(5, input.SelectionEnd);
    }

    [Fact]
    public void CtrlA_SelectsAll()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);

        SendKey(app, KeyCode.A, ctrl: true);

        Assert.True(input.HasSelection);
        Assert.Equal(0, input.SelectionStart);
        Assert.Equal(5, input.SelectionEnd);
        Assert.Equal(5, input.CursorPosition);
    }

    // ── Typing / deleting with active selection ───────────────────────

    [Fact]
    public void Typing_WithSelection_ReplacesSelectedText()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 4;
        input.SelectionStart = 1;
        input.SelectionEnd = 4;

        SendText(app, "a");

        Assert.Equal("hao", input.Value);
        Assert.Equal(2, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void Backspace_WithSelection_DeletesSelectedText()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 4;
        input.SelectionStart = 1;
        input.SelectionEnd = 4;

        SendKey(app, KeyCode.Backspace);

        Assert.Equal("ho", input.Value);
        Assert.Equal(1, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void Delete_WithSelection_DeletesSelectedText()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 4;
        input.SelectionStart = 1;
        input.SelectionEnd = 4;

        SendKey(app, KeyCode.Delete);

        Assert.Equal("ho", input.Value);
        Assert.Equal(1, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    // ── Arrow keys collapse selection ─────────────────────────────────

    [Fact]
    public void LeftArrow_WithSelection_CollapsesToSelectionStart()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.SelectionStart = 1;
        input.SelectionEnd = 4;
        input.CursorPosition = 4;

        SendKey(app, KeyCode.Left);

        Assert.Equal(1, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void RightArrow_WithSelection_CollapsesToSelectionEnd()
    {
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.SelectionStart = 1;
        input.SelectionEnd = 4;
        input.CursorPosition = 1;

        SendKey(app, KeyCode.Right);

        Assert.Equal(4, input.CursorPosition);
        Assert.False(input.HasSelection);
    }
}
