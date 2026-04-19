using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiDialog: Content swap removes old child first,
/// IsOpen toggles overlay display, Close click flips IsOpen and fires OnClose,
/// title text propagates to TextElement.
/// </summary>
public class LumiDialogTests
{
    [Fact]
    public void Default_IsClosedAndOverlayHidden()
    {
        var d = new LumiDialog();
        Assert.False(d.IsOpen);
        Assert.Contains("display: none", d.Root.InlineStyle);
    }

    [Fact]
    public void IsOpen_True_OverlayShowsAsFlex()
    {
        var d = new LumiDialog { IsOpen = true };
        Assert.Contains("display: flex", d.Root.InlineStyle);
        Assert.DoesNotContain("display: none", d.Root.InlineStyle);
    }

    [Fact]
    public void Title_PropagatesToTextElement()
    {
        var d = new LumiDialog { Title = "Confirm" };
        // overlay -> panel -> titleBar -> [titleText, closeButton]
        var panel = d.Root.Children[0];
        var titleBar = panel.Children[0];
        var titleText = (TextElement)titleBar.Children[0];
        Assert.Equal("Confirm", titleText.Text);
        Assert.Equal("Confirm", d.Title);
    }

    [Fact]
    public void Content_AssignsAddsChild_NullSwapClearsIt()
    {
        var d = new LumiDialog();
        var inner = new BoxElement("p");
        d.Content = inner;

        var panel = d.Root.Children[0];
        // panel children: [titleBar, contentArea]
        var contentArea = panel.Children[1];
        Assert.Single(contentArea.Children);
        Assert.Same(inner, contentArea.Children[0]);

        d.Content = null;
        Assert.Empty(contentArea.Children);
    }

    [Fact]
    public void Content_ReplacingExisting_RemovesPriorChild()
    {
        var d = new LumiDialog();
        var first = new BoxElement("p");
        var second = new BoxElement("span");
        d.Content = first;
        d.Content = second;

        var contentArea = d.Root.Children[0].Children[1];
        Assert.Single(contentArea.Children);
        Assert.Same(second, contentArea.Children[0]);
    }

    [Fact]
    public void CloseButton_Click_SetsClosedAndFiresCallback()
    {
        var d = new LumiDialog { IsOpen = true };
        bool closed = false;
        d.OnClose = () => closed = true;

        var titleBar = d.Root.Children[0].Children[0];
        var closeButton = titleBar.Children[1];
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, closeButton);

        Assert.False(d.IsOpen);
        Assert.True(closed);
    }
}
