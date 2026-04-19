using System.Collections.ObjectModel;
using Lumi.Core;
using Lumi.Core.Binding;

namespace Lumi.Tests.Binding;

/// <summary>
/// Targets surviving mutants in ItemsRenderer: initial population, observable
/// add/remove/replace/reset, unbind detaches handler, and Children indices stay
/// in sync with collection mutations.
/// </summary>
public class ItemsRendererTests
{
    private static (Element container, ItemsRenderer r) NewRenderer()
    {
        var c = new BoxElement("div");
        return (c, new ItemsRenderer());
    }

    [Fact]
    public void BindItemsSource_NullArgs_Throw()
    {
        var (c, r) = NewRenderer();
        Assert.Throws<ArgumentNullException>(() => r.BindItemsSource(null!, new[] { 1 }, () => new BoxElement()));
        Assert.Throws<ArgumentNullException>(() => r.BindItemsSource(c, null!, () => new BoxElement()));
        Assert.Throws<ArgumentNullException>(() => r.BindItemsSource(c, new[] { 1 }, null!));
    }

    [Fact]
    public void BindItemsSource_PopulatesEachItemAsChildWithDataContext()
    {
        var (c, r) = NewRenderer();
        var items = new[] { "a", "b", "c" };
        r.BindItemsSource(c, items, () => new BoxElement("li"));

        Assert.Equal(3, c.Children.Count);
        Assert.Equal("a", c.Children[0].DataContext);
        Assert.Equal("b", c.Children[1].DataContext);
        Assert.Equal("c", c.Children[2].DataContext);
    }

    [Fact]
    public void BindItemsSource_ClearsExistingChildrenBeforePopulating()
    {
        var (c, r) = NewRenderer();
        c.AddChild(new BoxElement("stale"));
        c.AddChild(new BoxElement("stale"));

        r.BindItemsSource(c, new[] { 1, 2 }, () => new BoxElement("li"));
        Assert.Equal(2, c.Children.Count);
        Assert.All(c.Children, child => Assert.Equal("li", child.TagName));
    }

    [Fact]
    public void ObservableAdd_AppendsChild_WithCorrectDataContext()
    {
        var (c, r) = NewRenderer();
        var coll = new ObservableCollection<string> { "a" };
        r.BindItemsSource(c, coll, () => new BoxElement("li"));

        coll.Add("b");

        Assert.Equal(2, c.Children.Count);
        Assert.Equal("b", c.Children[1].DataContext);
    }

    [Fact]
    public void ObservableInsertAtIndex_PlacesChildAtCorrectPosition()
    {
        var (c, r) = NewRenderer();
        var coll = new ObservableCollection<string> { "a", "c" };
        r.BindItemsSource(c, coll, () => new BoxElement("li"));

        coll.Insert(1, "b");

        Assert.Equal(3, c.Children.Count);
        Assert.Equal("a", c.Children[0].DataContext);
        Assert.Equal("b", c.Children[1].DataContext);
        Assert.Equal("c", c.Children[2].DataContext);
    }

    [Fact]
    public void ObservableRemove_RemovesCorrectChild()
    {
        var (c, r) = NewRenderer();
        var coll = new ObservableCollection<string> { "a", "b", "c" };
        r.BindItemsSource(c, coll, () => new BoxElement("li"));

        coll.RemoveAt(1);

        Assert.Equal(2, c.Children.Count);
        Assert.Equal("a", c.Children[0].DataContext);
        Assert.Equal("c", c.Children[1].DataContext);
    }

    [Fact]
    public void ObservableReplace_UpdatesDataContext_NotElementIdentity()
    {
        var (c, r) = NewRenderer();
        var coll = new ObservableCollection<string> { "a", "b" };
        r.BindItemsSource(c, coll, () => new BoxElement("li"));

        var originalChild = c.Children[1];
        coll[1] = "B";

        Assert.Same(originalChild, c.Children[1]);
        Assert.Equal("B", c.Children[1].DataContext);
    }

    [Fact]
    public void ObservableClear_RemovesAllChildren()
    {
        var (c, r) = NewRenderer();
        var coll = new ObservableCollection<int> { 1, 2, 3, 4 };
        r.BindItemsSource(c, coll, () => new BoxElement("li"));

        coll.Clear();

        Assert.Empty(c.Children);
    }

    [Fact]
    public void Unbind_StopsListeningToCollectionChanges()
    {
        var (c, r) = NewRenderer();
        var coll = new ObservableCollection<int> { 1 };
        r.BindItemsSource(c, coll, () => new BoxElement("li"));
        Assert.Single(c.Children);

        r.Unbind();
        coll.Add(2);

        // Container is still reachable but no longer wired up — child count unchanged.
        Assert.Single(c.Children);
    }

    [Fact]
    public void Rebind_NewSource_DiscardsPreviousSubscription()
    {
        var (c, r) = NewRenderer();
        var first = new ObservableCollection<int> { 1, 2 };
        r.BindItemsSource(c, first, () => new BoxElement("li"));

        var second = new ObservableCollection<int> { 10, 20, 30 };
        r.BindItemsSource(c, second, () => new BoxElement("p"));

        Assert.Equal(3, c.Children.Count);
        Assert.All(c.Children, child => Assert.Equal("p", child.TagName));

        // Mutating the first collection no longer affects the container.
        first.Add(99);
        Assert.Equal(3, c.Children.Count);
    }

    [Fact]
    public void NonObservableSource_OnlyInitialPopulation_NoUpdates()
    {
        var (c, r) = NewRenderer();
        var list = new List<int> { 1, 2, 3 };
        r.BindItemsSource(c, list, () => new BoxElement("li"));

        list.Add(4); // not observable
        Assert.Equal(3, c.Children.Count);
    }
}
