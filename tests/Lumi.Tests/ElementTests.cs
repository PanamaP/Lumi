using Lumi.Core;

namespace Lumi.Tests;

public class ElementTests
{
    [Fact]
    public void AddChild_SetsParent()
    {
        var parent = new BoxElement("div");
        var child = new BoxElement("span");

        parent.AddChild(child);

        Assert.Equal(parent, child.Parent);
        Assert.Single(parent.Children);
    }

    [Fact]
    public void RemoveChild_ClearsParent()
    {
        var parent = new BoxElement("div");
        var child = new BoxElement("span");
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.Null(child.Parent);
        Assert.Empty(parent.Children);
    }

    [Fact]
    public void AddChild_MarksDirty()
    {
        var parent = new BoxElement("div");
        parent.IsDirty = false;

        parent.AddChild(new BoxElement("span"));

        Assert.True(parent.IsDirty);
    }

    [Fact]
    public void MarkDirty_PropagatesUpward()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        var grandchild = new BoxElement("div");
        root.AddChild(child);
        child.AddChild(grandchild);
        root.IsDirty = false;
        child.IsDirty = false;

        grandchild.MarkDirty();

        Assert.True(child.IsDirty);
        Assert.True(root.IsDirty);
    }

    [Fact]
    public void ElementRegistry_CreatesCorrectTypes()
    {
        Assert.IsType<BoxElement>(ElementRegistry.Create("div"));
        Assert.IsType<TextElement>(ElementRegistry.Create("span"));
        Assert.IsType<ImageElement>(ElementRegistry.Create("img"));
        Assert.IsType<InputElement>(ElementRegistry.Create("input"));
        Assert.IsType<BoxElement>(ElementRegistry.Create("unknown-tag"));
    }

    [Fact]
    public void ToString_ShowsTagAndAttributes()
    {
        var element = new BoxElement("div") { Id = "main" };
        element.Classes.Add("container");

        Assert.Equal("<div id=\"main\" class=\"container\">", element.ToString());
    }
}
