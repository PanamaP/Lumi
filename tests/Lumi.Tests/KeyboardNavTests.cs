using Lumi.Core;

namespace Lumi.Tests;

public class KeyboardNavTests
{
    [Fact]
    public void BoxElement_Button_IsFocusable()
    {
        var button = new BoxElement("button");
        Assert.True(button.IsFocusable);
    }

    [Fact]
    public void BoxElement_Anchor_IsFocusable()
    {
        var anchor = new BoxElement("a");
        Assert.True(anchor.IsFocusable);
    }

    [Fact]
    public void BoxElement_Div_IsNotFocusable()
    {
        var div = new BoxElement("div");
        Assert.False(div.IsFocusable);
    }

    [Fact]
    public void InputElement_IsFocusable()
    {
        var input = new InputElement();
        Assert.True(input.IsFocusable);
    }

    [Fact]
    public void Element_IsFocused_DefaultsFalse()
    {
        var el = new BoxElement("div");
        Assert.False(el.IsFocused);
    }

    [Fact]
    public void Element_TabIndex_DefaultsToZero()
    {
        var el = new BoxElement("div");
        Assert.Equal(0, el.TabIndex);
    }

    [Fact]
    public void KeyCode_HasSymbolRowKeys()
    {
        // Ensure a developer can bind to the punctuation/symbol row.
        var names = Enum.GetNames<KeyCode>();
        foreach (var expected in new[]
        {
            nameof(KeyCode.Minus), nameof(KeyCode.Equals), nameof(KeyCode.LeftBracket),
            nameof(KeyCode.RightBracket), nameof(KeyCode.Backslash), nameof(KeyCode.Semicolon),
            nameof(KeyCode.Apostrophe), nameof(KeyCode.Grave), nameof(KeyCode.Comma),
            nameof(KeyCode.Period), nameof(KeyCode.Slash)
        })
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public void KeyCode_HasNumpadKeys()
    {
        var names = Enum.GetNames<KeyCode>();
        foreach (var expected in new[]
        {
            nameof(KeyCode.NumLock), nameof(KeyCode.NumpadDivide), nameof(KeyCode.NumpadMultiply),
            nameof(KeyCode.NumpadSubtract), nameof(KeyCode.NumpadAdd), nameof(KeyCode.NumpadEnter),
            nameof(KeyCode.NumpadDecimal), nameof(KeyCode.NumpadEquals), nameof(KeyCode.Numpad0),
            nameof(KeyCode.Numpad1), nameof(KeyCode.Numpad2), nameof(KeyCode.Numpad3),
            nameof(KeyCode.Numpad4), nameof(KeyCode.Numpad5), nameof(KeyCode.Numpad6),
            nameof(KeyCode.Numpad7), nameof(KeyCode.Numpad8), nameof(KeyCode.Numpad9)
        })
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public void RoutedTextInputEvent_CarriesText()
    {
        var ev = new RoutedTextInputEvent("TextInput") { Text = "*" };
        Assert.Equal("TextInput", ev.Name);
        Assert.Equal("*", ev.Text);
        Assert.IsAssignableFrom<RoutedEvent>(ev);
    }
}
