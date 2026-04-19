using Lumi.Core;
using Lumi.Tests.Helpers;

namespace Lumi.Tests.Dst;

/// <summary>
/// Regression tests for the cursor-clamp ordering bug where Backspace would
/// drive <see cref="InputElement.CursorPosition"/> negative because the
/// <c>Value</c> setter clamped first, then a stale post-decrement subtracted again.
/// </summary>
public class InputBackspaceCursorTests
{
    private const string BaseCss =
        "div { display: flex; flex-direction: column; padding: 0; }" +
        "input { width: 200px; height: 24px; }";

    [Fact]
    public void Backspace_FromSingleChar_LeavesCursorAtZero()
    {
        using var app = new HeadlessApp("<div><input id='f' /></div>", BaseCss);
        var input = (InputElement)app.Pipeline.FindById("f")!;
        app.App.SetFocus(input);

        app.EnqueueInput(new TextInputEvent { Text = "a" });
        app.Tick();
        Assert.Equal("a", input.Value);
        Assert.Equal(1, input.CursorPosition);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();

        Assert.Equal("", input.Value);
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void Backspace_PreservesCorrectCursorAfterEachKey()
    {
        using var app = new HeadlessApp("<div><input id='f' /></div>", BaseCss);
        var input = (InputElement)app.Pipeline.FindById("f")!;
        app.App.SetFocus(input);

        foreach (var ch in "abcd")
        {
            app.EnqueueInput(new TextInputEvent { Text = ch.ToString() });
            app.Tick();
        }
        Assert.Equal("abcd", input.Value);
        Assert.Equal(4, input.CursorPosition);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();
        Assert.Equal("abc", input.Value);
        Assert.Equal(3, input.CursorPosition);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();
        Assert.Equal("ab", input.Value);
        Assert.Equal(2, input.CursorPosition);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();
        Assert.Equal("a", input.Value);
        Assert.Equal(1, input.CursorPosition);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();
        Assert.Equal("", input.Value);
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void Backspace_OnEmpty_IsNoOp()
    {
        using var app = new HeadlessApp("<div><input id='f' /></div>", BaseCss);
        var input = (InputElement)app.Pipeline.FindById("f")!;
        app.App.SetFocus(input);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();

        Assert.Equal("", input.Value);
        Assert.Equal(0, input.CursorPosition);
    }

    [Fact]
    public void Backspace_FromMiddle_DeletesCharBeforeCursor()
    {
        using var app = new HeadlessApp("<div><input id='f' /></div>", BaseCss);
        var input = (InputElement)app.Pipeline.FindById("f")!;
        app.App.SetFocus(input);

        input.Value = "abcd";
        input.CursorPosition = 2;

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();

        Assert.Equal("acd", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void Backspace_RemovesSurrogatePairAsSingleGrapheme()
    {
        using var app = new HeadlessApp("<div><input id='f' /></div>", BaseCss);
        var input = (InputElement)app.Pipeline.FindById("f")!;
        app.App.SetFocus(input);

        // U+1F600 GRINNING FACE encoded as surrogate pair (length 2 in UTF-16).
        input.Value = "a\uD83D\uDE00";
        input.CursorPosition = input.Value.Length;
        Assert.Equal(3, input.CursorPosition);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();

        Assert.Equal("a", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void TypeThenBackspace_RoundTrip_IsClean()
    {
        using var app = new HeadlessApp("<div><input id='f' /></div>", BaseCss);
        var input = (InputElement)app.Pipeline.FindById("f")!;
        app.App.SetFocus(input);

        for (int i = 0; i < 10; i++)
        {
            app.EnqueueInput(new TextInputEvent { Text = "x" });
            app.Tick();
        }
        Assert.Equal("xxxxxxxxxx", input.Value);
        Assert.Equal(10, input.CursorPosition);

        for (int i = 0; i < 10; i++)
        {
            app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
            app.Tick();
            Assert.True(input.CursorPosition >= 0,
                $"CursorPosition went negative on iteration {i}: {input.CursorPosition}");
            Assert.Equal(input.Value.Length, input.CursorPosition);
        }
        Assert.Equal("", input.Value);
        Assert.Equal(0, input.CursorPosition);

        // Extra backspaces past empty must remain a no-op.
        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.Backspace, Type = KeyboardEventType.KeyDown });
        app.Tick();
        Assert.Equal(0, input.CursorPosition);
    }
}
