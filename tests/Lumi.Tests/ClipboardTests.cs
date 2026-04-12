using Lumi.Core;

namespace Lumi.Tests;

public class ClipboardTests : IDisposable
{
    public ClipboardTests()
    {
        Clipboard.ResetForTesting();
    }

    public void Dispose()
    {
        Clipboard.ResetForTesting();
    }

    private static void SetupMockClipboard(out Func<string?> getContent)
    {
        string? content = null;
        Clipboard.Initialize(() => content, t => content = t);
        getContent = () => content;
    }

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

    // ── Clipboard delegates ──────────────────────────────────────────

    [Fact]
    public void ClipboardDelegates_CanBeSetAndCalled()
    {
        string? stored = null;
        Clipboard.Initialize(() => stored, t => stored = t);

        Clipboard.SetText("hello");
        Assert.Equal("hello", Clipboard.GetText());
    }

    [Fact]
    public void ClipboardInitialize_CanBeCalledMultipleTimes()
    {
        Clipboard.Initialize(() => "first", _ => { });
        Assert.Equal("first", Clipboard.GetText());

        // Second call updates the delegates instead of throwing
        Clipboard.Initialize(() => "second", _ => { });
        Assert.Equal("second", Clipboard.GetText());
    }

    [Fact]
    public void ClipboardReset_AllowsFreshInitialize()
    {
        string? stored = null;
        Clipboard.Initialize(() => stored, t => stored = t);
        Clipboard.SetText("before");
        Assert.Equal("before", stored);

        Clipboard.Reset();

        // After reset, delegates are cleared
        Assert.Null(Clipboard.GetText());

        // Can re-initialize with new delegates
        string? stored2 = null;
        Clipboard.Initialize(() => stored2, t => stored2 = t);
        Clipboard.SetText("after");
        Assert.Equal("after", stored2);
    }

    // ── Ctrl+C ───────────────────────────────────────────────────────

    [Fact]
    public void CtrlC_CopiesSelectedText()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "hello world" };
        var app = CreateAppWithFocusedInput(input);
        input.SelectionStart = 6;
        input.SelectionEnd = 11;
        input.CursorPosition = 11;

        SendKey(app, KeyCode.C, ctrl: true);

        Assert.Equal("world", getContent());
        Assert.Equal("hello world", input.Value); // value unchanged
    }

    [Fact]
    public void CtrlC_DoesNothing_WhenNoSelection()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.C, ctrl: true);

        Assert.Null(getContent()); // no selection → nothing copied
        Assert.Equal("hello", input.Value); // value unchanged
    }

    [Fact]
    public void CtrlC_EmptyValue_DoesNotCrash()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "" };
        var app = CreateAppWithFocusedInput(input);

        SendKey(app, KeyCode.C, ctrl: true);

        Assert.Null(getContent()); // nothing copied
    }

    // ── Ctrl+V ───────────────────────────────────────────────────────

    [Fact]
    public void CtrlV_PastesTextAtCursor()
    {
        SetupMockClipboard(out _);
        Clipboard.SetText("world");

        var input = new InputElement { Value = "hello " };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 6;
        input.ClearSelection();

        SendKey(app, KeyCode.V, ctrl: true);

        Assert.Equal("hello world", input.Value);
        Assert.Equal(11, input.CursorPosition);
    }

    [Fact]
    public void CtrlV_ReplacesSelection()
    {
        SetupMockClipboard(out _);
        Clipboard.SetText("planet");

        var input = new InputElement { Value = "hello world" };
        var app = CreateAppWithFocusedInput(input);
        input.SelectionStart = 6;
        input.SelectionEnd = 11;
        input.CursorPosition = 11;

        SendKey(app, KeyCode.V, ctrl: true);

        Assert.Equal("hello planet", input.Value);
        Assert.Equal(12, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void CtrlV_IntoEmptyInput()
    {
        SetupMockClipboard(out _);
        Clipboard.SetText("pasted");

        var input = new InputElement { Value = "" };
        var app = CreateAppWithFocusedInput(input);

        SendKey(app, KeyCode.V, ctrl: true);

        Assert.Equal("pasted", input.Value);
        Assert.Equal(6, input.CursorPosition);
    }

    [Fact]
    public void CtrlV_EmptyClipboard_DoesNothing()
    {
        SetupMockClipboard(out _);
        // clipboard is null by default

        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.V, ctrl: true);

        Assert.Equal("hello", input.Value);
        Assert.Equal(3, input.CursorPosition);
    }

    // ── Ctrl+X ───────────────────────────────────────────────────────

    [Fact]
    public void CtrlX_CutsSelectedText()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "hello world" };
        var app = CreateAppWithFocusedInput(input);
        input.SelectionStart = 5;
        input.SelectionEnd = 11;
        input.CursorPosition = 11;

        SendKey(app, KeyCode.X, ctrl: true);

        Assert.Equal(" world", getContent());
        Assert.Equal("hello", input.Value);
        Assert.Equal(5, input.CursorPosition);
        Assert.False(input.HasSelection);
    }

    [Fact]
    public void CtrlX_DoesNothing_WhenNoSelection()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);
        input.CursorPosition = 3;
        input.ClearSelection();

        SendKey(app, KeyCode.X, ctrl: true);

        Assert.Null(getContent()); // no selection → nothing cut
        Assert.Equal("hello", input.Value); // value unchanged
        Assert.Equal(3, input.CursorPosition); // cursor unchanged
    }

    [Fact]
    public void CtrlX_EmptyValue_DoesNotCrash()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "" };
        var app = CreateAppWithFocusedInput(input);

        SendKey(app, KeyCode.X, ctrl: true);

        Assert.Null(getContent());
        Assert.Equal("", input.Value);
    }

    // ── Ctrl+C with reversed selection ───────────────────────────────

    [Fact]
    public void CtrlC_ReversedSelection_CopiesCorrectly()
    {
        SetupMockClipboard(out var getContent);

        var input = new InputElement { Value = "abcdef" };
        var app = CreateAppWithFocusedInput(input);
        // Selection where start > end (selected right-to-left)
        input.SelectionStart = 4;
        input.SelectionEnd = 1;
        input.CursorPosition = 1;

        SendKey(app, KeyCode.C, ctrl: true);

        Assert.Equal("bcd", getContent());
    }

    // ── No clipboard delegates ───────────────────────────────────────

    [Fact]
    public void CtrlC_WithNoClipboard_DoesNotCrash()
    {
        // Clipboard not initialized — delegates are null
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);

        SendKey(app, KeyCode.C, ctrl: true);

        Assert.Equal("hello", input.Value); // no crash
    }

    [Fact]
    public void CtrlV_WithNoClipboard_DoesNotCrash()
    {
        // Clipboard not initialized — delegates are null
        var input = new InputElement { Value = "hello" };
        var app = CreateAppWithFocusedInput(input);

        SendKey(app, KeyCode.V, ctrl: true);

        Assert.Equal("hello", input.Value); // no crash, no change
    }
}
