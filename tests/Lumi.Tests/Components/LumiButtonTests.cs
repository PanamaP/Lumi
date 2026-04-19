using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets LumiButton surviving mutants: handler suppression on disabled, root structure,
/// inline-style updates on Variant change, and label propagation through Text.
/// </summary>
public class LumiButtonTests
{
    [Fact]
    public void Constructor_DefaultsToPrimaryVariant_AndEmptyText()
    {
        var b = new LumiButton();
        Assert.Equal(ButtonVariant.Primary, b.Variant);
        Assert.Equal("", b.Text);
        // Primary uses accent.
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), b.Root.InlineStyle);
    }

    [Fact]
    public void Text_SettingUpdatesLabelChild()
    {
        var b = new LumiButton();
        b.Text = "Click Me";
        var label = (TextElement)b.Root.Children[0];
        Assert.Equal("Click Me", label.Text);
        Assert.Equal("Click Me", b.Text);
    }

    [Fact]
    public void Click_ReentrantHandler_FiresEachTime()
    {
        var b = new LumiButton();
        int count = 0;
        b.OnClick = () => count++;
        Click(b.Root);
        Click(b.Root);
        Click(b.Root);
        Assert.Equal(3, count);
    }

    [Fact]
    public void Disabled_BlocksOnClickAndMarksHandled()
    {
        var b = new LumiButton { IsDisabled = true };
        bool fired = false;
        b.OnClick = () => fired = true;
        var ev = new RoutedMouseEvent("click") { Button = MouseButton.Left };
        EventDispatcher.Dispatch(ev, b.Root);
        Assert.False(fired);
        Assert.True(ev.Handled);
    }

    [Fact]
    public void Disabled_AppliesDisabledStyles()
    {
        var b = new LumiButton { IsDisabled = true };
        Assert.Contains("opacity: 0.6", b.Root.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Disabled), b.Root.InlineStyle);
    }

    [Fact]
    public void ReEnable_RestoresEnabledStyleAndAcceptsClicks()
    {
        var b = new LumiButton { IsDisabled = true };
        b.IsDisabled = false;
        Assert.DoesNotContain("opacity: 0.6", b.Root.InlineStyle);

        bool fired = false;
        b.OnClick = () => fired = true;
        Click(b.Root);
        Assert.True(fired);
    }

    [Fact]
    public void Variant_DangerSwitch_RewritesInlineStyle()
    {
        var b = new LumiButton();
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), b.Root.InlineStyle);
        b.Variant = ButtonVariant.Danger;
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Danger), b.Root.InlineStyle);
        Assert.DoesNotContain($"background-color: var(--accent", b.Root.InlineStyle);
    }

    [Fact]
    public void OnClick_NullCallback_DoesNotThrow()
    {
        var b = new LumiButton();
        b.OnClick = null;
        var ex = Record.Exception(() => Click(b.Root));
        Assert.Null(ex);
    }

    [Fact]
    public void Root_IsButtonTag()
    {
        var b = new LumiButton();
        Assert.Equal("button", b.Root.TagName);
    }

    private static void Click(Element target)
    {
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, target);
    }
}
