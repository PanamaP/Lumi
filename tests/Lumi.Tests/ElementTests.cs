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

public class ClassListTests
{
    [Fact]
    public void Add_And_Contains()
    {
        var list = new ClassList();
        list.Add("active");
        list.Add("primary");

        Assert.True(list.Contains("active"));
        Assert.True(list.Contains("primary"));
        Assert.False(list.Contains("hidden"));
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Add_Deduplicates()
    {
        var list = new ClassList();
        list.Add("active");
        list.Add("active");

        Assert.Single(list);
    }

    [Fact]
    public void Remove_Works()
    {
        var list = new ClassList();
        list.Add("a");
        list.Add("b");

        Assert.True(list.Remove("a"));
        Assert.False(list.Contains("a"));
        Assert.True(list.Contains("b"));
        Assert.Single(list);
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalse()
    {
        var list = new ClassList();
        Assert.False(list.Remove("nope"));
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var list = new ClassList();
        list.Add("a");
        list.Add("b");
        list.Clear();

        Assert.Empty(list);
        Assert.False(list.Contains("a"));
    }

    [Fact]
    public void Constructor_WithEnumerable()
    {
        var list = new ClassList(["x", "y", "z", "x"]);

        Assert.Equal(3, list.Count);
        Assert.True(list.Contains("x"));
        Assert.True(list.Contains("y"));
        Assert.True(list.Contains("z"));
    }

    [Fact]
    public void SetFrom_ReplacesAll()
    {
        var list = new ClassList(["old1", "old2"]);
        list.SetFrom(["new1", "new2"]);

        Assert.Equal(2, list.Count);
        Assert.False(list.Contains("old1"));
        Assert.True(list.Contains("new1"));
        Assert.True(list.Contains("new2"));
    }

    [Fact]
    public void Indexer_Works()
    {
        var list = new ClassList(["a", "b", "c"]);

        Assert.Equal("a", list[0]);
        Assert.Equal("b", list[1]);
        Assert.Equal("c", list[2]);
    }

    [Fact]
    public void Enumeration_PreservesOrder()
    {
        var list = new ClassList(["z", "a", "m"]);
        var items = list.ToArray();

        Assert.Equal(["z", "a", "m"], items);
    }
}

public class ElementIndexTests
{
    [Fact]
    public void FindById_ReturnsElement()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("span") { Id = "target" };
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Same(child, index.FindById("target"));
    }

    [Fact]
    public void FindById_ReturnsNull_WhenNotFound()
    {
        var root = new BoxElement("div");
        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Null(index.FindById("nonexistent"));
    }

    [Fact]
    public void FindByClass_ReturnsMatchingElements()
    {
        var root = new BoxElement("div");
        var a = new BoxElement("div");
        a.Classes.Add("highlight");
        var b = new BoxElement("div");
        b.Classes.Add("highlight");
        var c = new BoxElement("div");
        c.Classes.Add("other");
        root.AddChild(a);
        root.AddChild(b);
        root.AddChild(c);

        var index = new ElementIndex();
        index.AttachTo(root);

        var results = index.FindByClass("highlight");
        Assert.Equal(2, results.Count);
        Assert.Contains(a, results);
        Assert.Contains(b, results);
    }

    [Fact]
    public void FindByClass_ReturnsEmpty_WhenNoMatch()
    {
        var root = new BoxElement("div");
        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Empty(index.FindByClass("nope"));
    }

    [Fact]
    public void Index_UpdatesOnAddChild()
    {
        var root = new BoxElement("div");
        var index = new ElementIndex();
        index.AttachTo(root);

        var child = new BoxElement("span") { Id = "added" };
        child.Classes.Add("dynamic");
        root.AddChild(child);

        Assert.Same(child, index.FindById("added"));
        Assert.Single(index.FindByClass("dynamic"));
    }

    [Fact]
    public void Index_UpdatesOnRemoveChild()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("span") { Id = "removable" };
        child.Classes.Add("temp");
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);
        Assert.Same(child, index.FindById("removable"));

        root.RemoveChild(child);

        Assert.Null(index.FindById("removable"));
        Assert.Empty(index.FindByClass("temp"));
    }

    [Fact]
    public void Index_TracksIdChange()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("span") { Id = "old-id" };
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);
        Assert.Same(child, index.FindById("old-id"));

        child.Id = "new-id";

        Assert.Null(index.FindById("old-id"));
        Assert.Same(child, index.FindById("new-id"));
    }

    [Fact]
    public void Index_TracksClassAdd()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("span");
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);
        Assert.Empty(index.FindByClass("added-later"));

        child.Classes.Add("added-later");

        Assert.Single(index.FindByClass("added-later"));
    }

    [Fact]
    public void Index_TracksClassRemove()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("span");
        child.Classes.Add("removable");
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);
        Assert.Single(index.FindByClass("removable"));

        child.Classes.Remove("removable");

        Assert.Empty(index.FindByClass("removable"));
    }

    [Fact]
    public void Index_TracksDeepSubtree()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        var grandchild = new BoxElement("span") { Id = "deep" };
        grandchild.Classes.Add("nested");
        child.AddChild(grandchild);
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Same(grandchild, index.FindById("deep"));
        Assert.Single(index.FindByClass("nested"));
    }

    [Fact]
    public void Index_ClearChildren_UnregistersAll()
    {
        var root = new BoxElement("div");
        var a = new BoxElement("span") { Id = "a" };
        var b = new BoxElement("span") { Id = "b" };
        root.AddChild(a);
        root.AddChild(b);

        var index = new ElementIndex();
        index.AttachTo(root);
        Assert.NotNull(index.FindById("a"));
        Assert.NotNull(index.FindById("b"));

        root.ClearChildren();

        Assert.Null(index.FindById("a"));
        Assert.Null(index.FindById("b"));
    }

    [Fact]
    public void Detach_ClearsIndex()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("span") { Id = "child" };
        root.AddChild(child);

        var index = new ElementIndex();
        index.AttachTo(root);
        Assert.Same(child, index.FindById("child"));

        index.Detach();

        Assert.Null(index.FindById("child"));
    }

    [Fact]
    public void Index_AddSubtreeAfterAttach()
    {
        var root = new BoxElement("div");
        var index = new ElementIndex();
        index.AttachTo(root);

        // Build a subtree separately, then attach
        var parent = new BoxElement("div");
        var child = new BoxElement("span") { Id = "subtree-child" };
        child.Classes.Add("sub");
        parent.AddChild(child);

        root.AddChild(parent);

        Assert.Same(child, index.FindById("subtree-child"));
        Assert.Single(index.FindByClass("sub"));
    }
}
