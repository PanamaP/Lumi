using Lumi;
using Lumi.Core;
using Lumi.Styling;

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

        // Verify the window is properly initialized with its own tree and resolver
        Assert.NotNull(window.Root);
        Assert.Equal("body", window.Root.TagName);
        Assert.NotNull(window.StyleResolver);
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
    public void SecondaryWindow_LoadStyleSheetString_AppliesStyles()
    {
        var window = new SecondaryWindow();
        window.LoadTemplateString("<div><span>Styled</span></div>");
        window.LoadStyleSheetString("div { color: red; }");

        // Resolve styles and verify the CSS rule is applied
        window.StyleResolver.ResolveStyles(window.Root, new PseudoClassState(false, false, false));

        var div = window.Root.Children[0];
        Assert.Equal(new Color(255, 0, 0, 255), div.ComputedStyle.Color);
    }

    [Fact]
    public void SecondaryWindow_Lifecycle_OpenUpdateClose()
    {
        var window = new SecondaryWindow { Title = "Lifecycle Test" };
        Assert.False(window.IsOpen);

        // Simulate open
        window.IsOpen = true;
        Assert.True(window.IsOpen);

        // Build content during the "open" phase
        window.LoadTemplateString("<div id='main'><p>Content</p></div>");
        var main = window.FindById("main");
        Assert.NotNull(main);

        // Simulate an update: add a child
        var extra = new TextElement("Dynamic");
        main!.AddChild(extra);
        Assert.Equal(2, main.Children.Count);

        // Close
        window.Close();
        Assert.False(window.IsOpen);

        // Element tree is still intact after close (not disposed, just closed)
        Assert.Equal(2, main.Children.Count);
    }

    // ── NavigateTo ──────────────────────────────────────────────────

    [Fact]
    public void NavigateTo_ThrowsWhenNotHosted()
    {
        var window = new Window();
        var target = new Window();

        var ex = Assert.Throws<InvalidOperationException>(() => window.NavigateTo(target));
        Assert.Contains("hosted by LumiApp", ex.Message);
    }

    [Fact]
    public void NavigateTo_ThrowsOnNull()
    {
        var window = new Window();

        Assert.Throws<ArgumentNullException>(() => window.NavigateTo(null!));
    }
}
