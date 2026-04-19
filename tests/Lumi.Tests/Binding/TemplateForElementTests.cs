using System.Collections.ObjectModel;
using Lumi.Core;
using Lumi.Core.Binding;

namespace Lumi.Tests.Binding;

/// <summary>
/// Targets surviving mutants in TemplateForElement: OnCollectionChanged behaviour
/// for Add/Insert/Remove/Replace/Reset, Unbind detaching the handler, and DeepClone
/// preserving the directive metadata.
/// </summary>
public class TemplateForElementTests
{
    /// <summary>
    /// Calls the internal BindCollection with a trivial element factory so we can
    /// drive the OnCollectionChanged switch directly without invoking the
    /// HTML parser-based TemplateEngine.
    /// </summary>
    private static TemplateForElement BindEmptyTemplate(ObservableCollection<string> coll, out Func<object, Element> factory)
    {
        var t = new TemplateForElement
        {
            CollectionPath = "Items",
            ItemAlias = "item",
            TemplateHtml = "<span></span>"
        };
        factory = item => new BoxElement("li") { DataContext = item };
        t.BindCollection(coll, factory);
        return t;
    }

    private static void Seed(TemplateForElement t, ObservableCollection<string> coll, Func<object, Element> factory)
    {
        // Mirror what TemplateEngine.ActivateFor does for the initial population.
        foreach (var item in coll) t.AddChild(factory(item));
    }

    [Fact]
    public void Add_AppendsChild_PreservesDataContext()
    {
        var coll = new ObservableCollection<string> { "a" };
        var t = BindEmptyTemplate(coll, out var factory);
        Seed(t, coll, factory);

        coll.Add("b");

        Assert.Equal(2, t.Children.Count);
        Assert.Equal("b", t.Children[1].DataContext);
    }

    [Fact]
    public void Insert_PlacesChildAtSpecifiedIndex()
    {
        var coll = new ObservableCollection<string> { "a", "c" };
        var t = BindEmptyTemplate(coll, out var factory);
        Seed(t, coll, factory);

        coll.Insert(1, "b");

        Assert.Equal(3, t.Children.Count);
        Assert.Equal("a", t.Children[0].DataContext);
        Assert.Equal("b", t.Children[1].DataContext);
        Assert.Equal("c", t.Children[2].DataContext);
    }

    [Fact]
    public void Remove_DropsCorrectChildByIndex()
    {
        var coll = new ObservableCollection<string> { "a", "b", "c" };
        var t = BindEmptyTemplate(coll, out var factory);
        Seed(t, coll, factory);

        coll.RemoveAt(0);

        Assert.Equal(2, t.Children.Count);
        Assert.Equal("b", t.Children[0].DataContext);
        Assert.Equal("c", t.Children[1].DataContext);
    }

    [Fact]
    public void Replace_SwapsTheChildElement()
    {
        var coll = new ObservableCollection<string> { "a", "b" };
        var t = BindEmptyTemplate(coll, out var factory);
        Seed(t, coll, factory);

        var originalSecond = t.Children[1];
        coll[1] = "B";

        Assert.NotSame(originalSecond, t.Children[1]);
        Assert.Equal("B", t.Children[1].DataContext);
    }

    [Fact]
    public void Clear_ResetsAllChildren_AndKeepsSubscriptionLive()
    {
        var coll = new ObservableCollection<string> { "a", "b", "c" };
        var t = BindEmptyTemplate(coll, out var factory);
        Seed(t, coll, factory);

        coll.Clear();

        Assert.Empty(t.Children);

        // After Clear (which goes through Reset), the subscription should still be live.
        coll.Add("post-clear");
        Assert.Single(t.Children);
        Assert.Equal("post-clear", t.Children[0].DataContext);
    }

    [Fact]
    public void Unbind_StopsListeningAndDisposesBindings()
    {
        var coll = new ObservableCollection<string> { "a" };
        var t = BindEmptyTemplate(coll, out var factory);
        Seed(t, coll, factory);

        t.Unbind();
        coll.Add("ignored");

        Assert.Single(t.Children); // unchanged
    }

    [Fact]
    public void BindCollection_OnNonObservable_DoesNotThrow()
    {
        var t = new TemplateForElement();
        var ex = Record.Exception(() => t.BindCollection(new[] { 1, 2 }, _ => new BoxElement("li")));
        Assert.Null(ex);
    }

    [Fact]
    public void DeepClone_PreservesCollectionPath_ItemAlias_AndTemplateHtml()
    {
        var t = new TemplateForElement
        {
            CollectionPath = "Items",
            ItemAlias = "x",
            TemplateHtml = "<span>{x.Name}</span>"
        };
        var clone = (TemplateForElement)t.DeepClone();

        Assert.Equal("Items", clone.CollectionPath);
        Assert.Equal("x", clone.ItemAlias);
        Assert.Equal("<span>{x.Name}</span>", clone.TemplateHtml);
        Assert.NotSame(t, clone);
    }

    [Fact]
    public void TagName_IsTemplate()
    {
        var t = new TemplateForElement();
        Assert.Equal("template", t.TagName);
    }

    [Fact]
    public void Rebind_DiscardsPreviousSubscription()
    {
        var first = new ObservableCollection<string> { "a" };
        var t = BindEmptyTemplate(first, out var factory);
        Seed(t, first, factory);

        var second = new ObservableCollection<string> { "x" };
        // Re-seed to keep child counts and indices consistent.
        t.ClearChildren();
        t.BindCollection(second, factory);
        Seed(t, second, factory);

        // Adding to "first" must NOT affect template now.
        first.Add("ignored");
        Assert.Single(t.Children); // unchanged

        // Adding to "second" must affect the template.
        second.Add("z");
        Assert.Equal(2, t.Children.Count);
        Assert.Equal("z", t.Children[1].DataContext);
    }
}
