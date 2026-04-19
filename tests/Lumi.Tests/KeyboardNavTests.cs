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
            "Minus", "Equals", "LeftBracket", "RightBracket", "Backslash",
            "Semicolon", "Apostrophe", "Grave", "Comma", "Period", "Slash"
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
            "NumLock", "NumpadDivide", "NumpadMultiply", "NumpadSubtract",
            "NumpadAdd", "NumpadEnter", "NumpadDecimal", "NumpadEquals",
            "Numpad0", "Numpad1", "Numpad2", "Numpad3", "Numpad4",
            "Numpad5", "Numpad6", "Numpad7", "Numpad8", "Numpad9"
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
