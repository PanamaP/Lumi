using Lumi.Core;
using Lumi.Core.Binding;
using Lumi.Core.Components;
using Lumi.Core.Navigation;

namespace Lumi.Tests;

/// <summary>
/// Second pass of mutation refinements: covers Router edge cases, ClassList
/// integrated with ElementIndex (so the OnClassAdded/Removed side-effects
/// actually run), TemplateBinding text/format/INPC paths and a few
/// LumiTooltip arithmetic boundaries.
/// </summary>
public class MutationRefinement2Tests
{
    // ---------------- Router ----------------

    [Fact]
    public void Router_Construct_NullContainer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Router(null!));
    }

    [Fact]
    public void Router_Register_NullPattern_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentNullException>(() => r.Register((string)null!, () => new BoxElement("div")));
    }

    [Fact]
    public void Router_Register_NullParameterlessFactory_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentNullException>(() => r.Register("/x", (Func<Element>)null!));
    }

    [Fact]
    public void Router_Register_NullParameterizedFactory_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentNullException>(() => r.Register("/x", (Func<RouteParameters, Element>)null!));
    }

    [Fact]
    public void Router_Register_EmptyParameterName_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentException>(() => r.Register("/user/{}/page", _ => new BoxElement("div")));
    }

    [Fact]
    public void Router_Register_DuplicateParameterName_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentException>(() => r.Register("/user/{id}/file/{id}", _ => new BoxElement("div")));
    }

    [Fact]
    public void Router_Register_ReplacesExistingPattern()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/x", () => { var b = new BoxElement("a"); b.Attributes["v"] = "1"; return b; });
        r.Register("/x", () => { var b = new BoxElement("a"); b.Attributes["v"] = "2"; return b; });
        r.Navigate("/x");
        Assert.Equal("2", r.Container.Children[0].Attributes["v"]);
    }

    [Fact]
    public void Router_Navigate_UnknownRoute_ReturnsFalse_LeavesContainer()
    {
        var c = new BoxElement("body");
        c.AddChild(new TextElement("placeholder"));
        var r = new Router(c);
        Assert.False(r.Navigate("/unknown"));
        Assert.Single(c.Children); // placeholder untouched
    }

    [Fact]
    public void Router_Navigate_EmptyPath_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentException>(() => r.Navigate("/"));
    }

    [Fact]
    public void Router_Navigate_NullPath_Throws()
    {
        var r = new Router(new BoxElement("body"));
        Assert.Throws<ArgumentNullException>(() => r.Navigate(null!));
    }

    [Fact]
    public void Router_Navigate_ToCurrentRoute_DoesNotAddHistory()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/a", () => new BoxElement("a"));
        r.Navigate("/a");
        r.Navigate("/a");
        Assert.False(r.CanGoBack); // CurrentRoute == normalized => no push
    }

    [Fact]
    public void Router_GoBack_NoHistory_DoesNothing()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/a", () => new BoxElement("a"));
        r.Navigate("/a");
        r.GoBack();
        Assert.Equal("a", r.CurrentRoute);
    }

    [Fact]
    public void Router_GoBack_PopsHistory_AndChangesRoute()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/a", () => { var b = new BoxElement("a"); b.Attributes["t"] = "A"; return b; });
        r.Register("/b", () => { var b = new BoxElement("b"); b.Attributes["t"] = "B"; return b; });
        r.Navigate("/a");
        r.Navigate("/b");
        Assert.True(r.CanGoBack);

        r.GoBack();

        Assert.Equal("a", r.CurrentRoute);
        Assert.False(r.CanGoBack);
        Assert.Equal("A", r.Container.Children[0].Attributes["t"]);
    }

    [Fact]
    public void Router_RaisesRouteChanged_OnNavigate_AndOnGoBack()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/a", () => new BoxElement("a"));
        r.Register("/b", () => new BoxElement("b"));
        var log = new List<string>();
        r.RouteChanged += s => log.Add(s);

        r.Navigate("/a");
        r.Navigate("/b");
        r.GoBack();

        Assert.Equal(new[] { "a", "b", "a" }, log);
    }

    [Fact]
    public void Router_PathParam_ExtractsValue()
    {
        var r = new Router(new BoxElement("body"));
        string? receivedId = null;
        r.Register("/user/{id}", p => { receivedId = p["id"]; return new BoxElement("u"); });
        Assert.True(r.Navigate("/user/42"));
        Assert.Equal("42", receivedId);
    }

    [Fact]
    public void Router_PathParam_LengthMismatch_Misses()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/user/{id}/profile", _ => new BoxElement("u"));
        Assert.False(r.Navigate("/user/42")); // missing trailing segment
    }

    [Fact]
    public void Router_NormalizePath_TrimsSlashes()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/x/", () => new BoxElement("a"));
        Assert.True(r.Navigate("///x///"));
        Assert.Equal("x", r.CurrentRoute);
    }

    [Fact]
    public void Router_PageFactoryThrows_WrappedInInvalidOperation()
    {
        var r = new Router(new BoxElement("body"));
        r.Register("/x", () => throw new InvalidCastException("boom"));
        var ex = Assert.Throws<InvalidOperationException>(() => r.Navigate("/x"));
        Assert.IsType<InvalidCastException>(ex.InnerException);
    }

    // ---------------- ClassList with attached ElementIndex (kills callback statements) ----------------

    [Fact]
    public void ClassList_Add_NotifiesIndex()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        child.Classes.Add("foo");

        Assert.Contains(child, index.FindByClass("foo"));
    }

    [Fact]
    public void ClassList_Remove_NotifiesIndex()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("foo");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Contains(child, index.FindByClass("foo"));
        child.Classes.Remove("foo");
        Assert.DoesNotContain(child, index.FindByClass("foo"));
    }

    [Fact]
    public void ClassList_Insert_NotifiesIndex()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        child.Classes.Insert(0, "bar");

        Assert.Contains(child, index.FindByClass("bar"));
    }

    [Fact]
    public void ClassList_RemoveAt_NotifiesIndex()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("a");
        child.Classes.Add("b");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        child.Classes.RemoveAt(0);

        Assert.DoesNotContain(child, index.FindByClass("a"));
        Assert.Contains(child, index.FindByClass("b"));
    }

    [Fact]
    public void ClassList_Clear_NotifiesIndex_ForEachClass()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("a");
        child.Classes.Add("b");
        child.Classes.Add("c");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        child.Classes.Clear();

        Assert.Empty(index.FindByClass("a"));
        Assert.Empty(index.FindByClass("b"));
        Assert.Empty(index.FindByClass("c"));
    }

    [Fact]
    public void ClassList_SetFrom_NotifiesIndex_ForOldAndNewClasses()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("old1");
        child.Classes.Add("old2");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        Assert.Contains(child, index.FindByClass("old1"));

        child.Classes.SetFrom(new[] { "new1", "new2" });

        Assert.Empty(index.FindByClass("old1"));
        Assert.Empty(index.FindByClass("old2"));
        Assert.Contains(child, index.FindByClass("new1"));
        Assert.Contains(child, index.FindByClass("new2"));
    }

    [Fact]
    public void ClassList_IndexerReplace_NotifiesIndex_ForOldAndNew()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("alpha");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        child.Classes[0] = "beta";

        Assert.Empty(index.FindByClass("alpha"));
        Assert.Contains(child, index.FindByClass("beta"));
    }

    [Fact]
    public void ClassList_IndexerReplaceWithExisting_RemovesOldSlot_AndKeepsExisting()
    {
        var root = new BoxElement("body");
        var child = new BoxElement("div");
        child.Classes.Add("a");
        child.Classes.Add("b");
        root.AddChild(child);
        var index = new ElementIndex();
        index.AttachTo(root);

        child.Classes[0] = "b"; // assigning value that already exists at index 1
        Assert.Empty(index.FindByClass("a"));
        Assert.Contains(child, index.FindByClass("b"));
        Assert.Equal(new[] { "b" }, child.Classes);
    }

    // ---------------- TemplateBinding ----------------

    private sealed class Item : System.ComponentModel.INotifyPropertyChanged
    {
        public Item(string name, int count) { _name = name; _count = count; }
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); }
        }
        private int _count;
        public int Count
        {
            get => _count;
            set { _count = value; PropertyChanged?.Invoke(this, new(nameof(Count))); }
        }
        public override string ToString() => $"Item({_name})";
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    [Fact]
    public void TemplateBinding_TryCreate_NoBraces_ReturnsNull()
    {
        var t = new TextElement();
        var b = TemplateBinding.TryCreate(t, "Text", false, "no braces", "item", new Item("x", 0));
        Assert.Null(b);
    }

    [Fact]
    public void TemplateBinding_TryCreate_EmptyText_ReturnsNull()
    {
        var t = new TextElement();
        var b = TemplateBinding.TryCreate(t, "Text", false, "", "item", new Item("x", 0));
        Assert.Null(b);
    }

    [Fact]
    public void TemplateBinding_TryCreate_NoMatchingAlias_ReturnsNull()
    {
        var t = new TextElement();
        // Has braces and a path, but the alias doesn't match.
        var b = TemplateBinding.TryCreate(t, "Text", false, "{other.Name}", "item", new Item("x", 0));
        Assert.Null(b);
    }

    [Fact]
    public void TemplateBinding_FormatsMultipleInterpolations_AndReactsToPropertyChange()
    {
        var item = new Item("Apples", 3);
        var t = new TextElement("");
        using var b = TemplateBinding.TryCreate(t, "Text", false, "{item.Name}: {item.Count}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("Apples: 3", t.Text);

        item.Name = "Pears";
        Assert.Equal("Pears: 3", t.Text);

        item.Count = 7;
        Assert.Equal("Pears: 7", t.Text);
    }

    [Fact]
    public void TemplateBinding_AliasOnly_UsesSourceToString()
    {
        var item = new Item("Bananas", 2);
        var t = new TextElement("");
        using var b = TemplateBinding.TryCreate(t, "Text", false, "Hello {item}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("Hello Item(Bananas)", t.Text);
    }

    [Fact]
    public void TemplateBinding_DispatchesToInputValue()
    {
        var item = new Item("Z", 0);
        var input = new InputElement();
        using var b = TemplateBinding.TryCreate(input, "Value", false, "{item.Name}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("Z", input.Value);
    }

    [Fact]
    public void TemplateBinding_DispatchesToInlineStyle()
    {
        var item = new Item("color: red;", 0);
        var div = new BoxElement("div");
        using var b = TemplateBinding.TryCreate(div, "InlineStyle", false, "{item.Name}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("color: red;", div.InlineStyle);
    }

    [Fact]
    public void TemplateBinding_DispatchesToImageSource()
    {
        var item = new Item("logo.png", 0);
        var img = new ImageElement();
        using var b = TemplateBinding.TryCreate(img, "Source", false, "{item.Name}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("logo.png", img.Source);
    }

    [Fact]
    public void TemplateBinding_AttributeMode_WritesToAttribute()
    {
        var item = new Item("hello", 0);
        var div = new BoxElement("div");
        using var b = TemplateBinding.TryCreate(div, "title", true, "{item.Name}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("hello", div.Attributes["title"]);
        item.Name = "world";
        Assert.Equal("world", div.Attributes["title"]);
    }

    [Fact]
    public void TemplateBinding_NonNotifyingSource_DoesNotSubscribe()
    {
        var src = new { Name = "static" };
        var t = new TextElement();
        using var b = TemplateBinding.TryCreate(t, "Text", false, "{item.Name}", "item", src)!;
        Assert.NotNull(b);
        Assert.Equal("static", t.Text);
    }

    [Fact]
    public void TemplateBinding_Dispose_StopsUpdates()
    {
        var item = new Item("A", 0);
        var t = new TextElement();
        var b = TemplateBinding.TryCreate(t, "Text", false, "{item.Name}", "item", item)!;
        Assert.Equal("A", t.Text);

        b.Dispose();
        item.Name = "B";
        Assert.Equal("A", t.Text); // dispose unhooked

        // Dispose-after-dispose is idempotent
        var ex = Record.Exception(() => b.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void TemplateBinding_LiteralBracesPreservedThroughFormat()
    {
        var item = new Item("X", 0);
        var t = new TextElement();
        using var b = TemplateBinding.TryCreate(t, "Text", false, "{{ literal }} {item.Name}", "item", item)!;
        Assert.NotNull(b);
        Assert.Equal("{{ literal }} X", t.Text);
    }

    // ---------------- LumiTooltip arithmetic boundaries ----------------

    [Fact]
    public void Tooltip_RightFitting_BoundaryEdge_Fits_AtExactlyEqualToView()
    {
        // target.Right + 4 + tooltipW == viewW => "<= viewW" branch fires.
        // target at (0,0,30,30); estimated tooltipW for "ab" = 2*7+16 = 30.
        // 30 + 4 + 30 = 64. Set viewW=64 so exactly equal.
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 64, 600) };
        var target = new BoxElement("div") { LayoutBox = new LayoutBox(0, 0, 30, 30) };
        root.AddChild(target);
        var tooltip = LumiTooltip.Attach(target, "ab");
        EventDispatcher.Dispatch(new RoutedEvent("mouseenter"), target);
        Assert.Contains("left: 34px", tooltip.Root.InlineStyle ?? ""); // chose the "right" branch
    }

    [Fact]
    public void Tooltip_LeftFitting_BoundaryEdge_AtZero()
    {
        // For left branch: target.X - 4 - tooltipW >= 0. Choose values for exact equality.
        // tooltipW for "ab" = 30. target.X = 34, viewW = 50 (so right doesn't fit).
        // 34 - 4 - 30 = 0 >= 0 → left branch.
        var root = new BoxElement("body") { LayoutBox = new LayoutBox(0, 0, 50, 600) };
        var target = new BoxElement("div") { LayoutBox = new LayoutBox(34, 50, 16, 30) }; // right edge=50, leaves no right room
        root.AddChild(target);
        var tooltip = LumiTooltip.Attach(target, "ab");
        EventDispatcher.Dispatch(new RoutedEvent("mouseenter"), target);
        var style = tooltip.Root.InlineStyle ?? "";
        Assert.Contains("left: 0px", style);
        Assert.Contains("top: 50px", style);
    }

    [Fact]
    public void Tooltip_TextIsExposedViaTextElement_AfterAttach()
    {
        // Kills "Statement mutation L28" — _container = new BoxElement(...) initialization
        var t = new BoxElement("div");
        var tooltip = LumiTooltip.Attach(t, "test");
        Assert.NotNull(tooltip.Root);
        Assert.IsType<BoxElement>(tooltip.Root);
        // Container has the _textElement child plus nothing else initially.
        Assert.Single(tooltip.Root.Children);
    }
}
