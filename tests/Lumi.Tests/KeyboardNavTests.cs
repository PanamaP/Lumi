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
}
