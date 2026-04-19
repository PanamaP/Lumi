using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiTextBox: Value/Placeholder propagation, ReadOnly
/// suppression of OnValueChanged, label sync, and InputElement default type.
/// </summary>
public class LumiTextBoxTests
{
    [Fact]
    public void Default_InputTypeIsText()
    {
        var tb = new LumiTextBox();
        Assert.Equal("text", tb.InputElement.InputType);
    }

    [Fact]
    public void Default_ChildrenIncludeLabelAndInputInOrder()
    {
        var tb = new LumiTextBox();
        Assert.Equal(2, tb.Root.Children.Count);
        Assert.IsType<TextElement>(tb.Root.Children[0]);
        Assert.IsType<InputElement>(tb.Root.Children[1]);
    }

    [Fact]
    public void Label_SyncsToTextElement()
    {
        var tb = new LumiTextBox();
        Assert.Null(tb.Label);
        tb.Label = "Email";
        Assert.Equal("Email", tb.Label);
        var label = (TextElement)tb.Root.Children[0];
        Assert.Equal("Email", label.Text);
    }

    [Fact]
    public void Label_NullClearsTextElement()
    {
        var tb = new LumiTextBox { Label = "Name" };
        tb.Label = null;
        var label = (TextElement)tb.Root.Children[0];
        Assert.Equal("", label.Text);
    }

    [Fact]
    public void Value_GetReturnsLatestSet()
    {
        var tb = new LumiTextBox();
        tb.Value = "abc";
        Assert.Equal("abc", tb.Value);
        tb.Value = "xyz";
        Assert.Equal("xyz", tb.Value);
    }

    [Fact]
    public void Placeholder_GetReturnsLatestSet()
    {
        var tb = new LumiTextBox();
        Assert.Equal("", tb.Placeholder);
        tb.Placeholder = "type here";
        Assert.Equal("type here", tb.Placeholder);
    }

    [Fact]
    public void Setting_Value_MarksInputDirty()
    {
        var tb = new LumiTextBox();
        tb.InputElement.IsDirty = false;
        tb.Value = "hi";
        Assert.True(tb.InputElement.IsDirty);
    }

    [Fact]
    public void OnValueChanged_FiresWithCurrentInputValue()
    {
        var tb = new LumiTextBox();
        string? captured = null;
        tb.OnValueChanged = v => captured = v;

        tb.InputElement.Value = "hello";
        EventDispatcher.Dispatch(new RoutedEvent("input"), tb.InputElement);

        Assert.Equal("hello", captured);
    }

    [Fact]
    public void IsReadOnly_SuppressesOnValueChanged()
    {
        var tb = new LumiTextBox { IsReadOnly = true };
        bool fired = false;
        tb.OnValueChanged = _ => fired = true;

        tb.InputElement.Value = "blocked";
        EventDispatcher.Dispatch(new RoutedEvent("input"), tb.InputElement);

        Assert.False(fired);
    }

    [Fact]
    public void IsReadOnly_FalseAfterTrue_AllowsCallbackAgain()
    {
        var tb = new LumiTextBox { IsReadOnly = true };
        int count = 0;
        tb.OnValueChanged = _ => count++;

        EventDispatcher.Dispatch(new RoutedEvent("input"), tb.InputElement);
        tb.IsReadOnly = false;
        EventDispatcher.Dispatch(new RoutedEvent("input"), tb.InputElement);

        Assert.Equal(1, count);
    }

    [Fact]
    public void OnValueChanged_NullCallback_DoesNotThrow()
    {
        var tb = new LumiTextBox();
        tb.OnValueChanged = null;
        var ex = Record.Exception(() => EventDispatcher.Dispatch(new RoutedEvent("input"), tb.InputElement));
        Assert.Null(ex);
    }
}
