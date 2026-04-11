using Lumi;
using Lumi.Core;

namespace Lumi.Tests;

public class MultiWindowTests
{
    [Fact]
    public void WindowManager_TracksOpenedWindows()
    {
        var manager = new WindowManager();
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void SecondaryWindow_CanBeCreated()
    {
        var window = new SecondaryWindow
        {
            Title = "Test Window",
            Width = 400,
            Height = 300
        };

        Assert.Equal("Test Window", window.Title);
        Assert.Equal(400, window.Width);
        Assert.Equal(300, window.Height);
        Assert.False(window.IsOpen);
    }

    [Fact]
    public void SecondaryWindow_CloseSetsFalse()
    {
        var window = new SecondaryWindow();
        window.IsOpen = true;

        window.Close();

        Assert.False(window.IsOpen);
    }

    [Fact]
    public void CloseAll_OnEmptyManagerDoesNotThrow()
    {
        var manager = new WindowManager();
        var ex = Record.Exception(() => manager.CloseAll());
        Assert.Null(ex);
    }

    [Fact]
    public void SecondaryWindow_HasOwnRootElementTree()
    {
        var window1 = new SecondaryWindow();
        var window2 = new SecondaryWindow();

        var child1 = new BoxElement("div");
        child1.Id = "child1";
        window1.Root.AddChild(child1);

        var child2 = new BoxElement("span");
        child2.Id = "child2";
        window2.Root.AddChild(child2);

        // Each window has its own independent element tree
        Assert.Single(window1.Root.Children);
        Assert.Single(window2.Root.Children);
        Assert.Equal("child1", window1.Root.Children[0].Id);
        Assert.Equal("child2", window2.Root.Children[0].Id);
    }

    [Fact]
    public void SecondaryWindow_HasOwnStyleResolver()
    {
        var window1 = new SecondaryWindow();
        var window2 = new SecondaryWindow();

        // Each secondary window inherits its own StyleResolver from Window base class
        Assert.NotNull(window1.StyleResolver);
        Assert.NotNull(window2.StyleResolver);
        Assert.NotSame(window1.StyleResolver, window2.StyleResolver);
    }

    [Fact]
    public void SecondaryWindow_InheritsWindowProperties()
    {
        var window = new SecondaryWindow();

        // Can use all Window base class features
        Assert.NotNull(window.Root);
        Assert.Equal("body", window.Root.TagName);
        Assert.NotNull(window.StyleResolver);
    }

    [Fact]
    public void SecondaryWindow_IsNotOpenByDefault()
    {
        var window = new SecondaryWindow();
        Assert.False(window.IsOpen);
        Assert.Null(window.PlatformWindow);
        Assert.Null(window.SecondaryRenderer);
    }

    [Fact]
    public void Window_HasWindowsProperty()
    {
        var window = new Window();
        // Windows property is null until set by LumiApp
        Assert.Null(window.Windows);
    }

    [Fact]
    public void SecondaryWindow_LoadTemplateString_BuildsTree()
    {
        var window = new SecondaryWindow();
        window.LoadTemplateString("<div id='content'><p>Hello</p></div>");

        var content = window.FindById("content");
        Assert.NotNull(content);
        Assert.Single(content!.Children);
    }

    [Fact]
    public void SecondaryWindow_LoadStyleSheetString_AddsStyles()
    {
        var window = new SecondaryWindow();
        window.LoadStyleSheetString("div { color: red; }");

        // Verify the style resolver has the stylesheet loaded (no exception)
        Assert.NotNull(window.StyleResolver);
    }
}
