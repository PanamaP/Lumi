using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiList: Items setter null-coalesce, RebuildItems
/// produces correct rows, click handlers report the right index.
/// </summary>
public class LumiListTests
{
    [Fact]
    public void Default_NoItems_NoChildren()
    {
        var list = new LumiList();
        Assert.Empty(list.Items);
        Assert.Empty(list.Root.Children);
    }

    [Fact]
    public void Items_AssigningNull_ResultsInEmptyList()
    {
        var list = new LumiList { Items = null! };
        Assert.NotNull(list.Items);
        Assert.Empty(list.Items);
        Assert.Empty(list.Root.Children);
    }

    [Fact]
    public void Items_AssignedList_BecomesRowsOfMatchingCount()
    {
        var list = new LumiList { Items = ["one", "two", "three"] };
        Assert.Equal(3, list.Root.Children.Count);
        for (int i = 0; i < 3; i++)
        {
            var row = list.Root.Children[i];
            var text = (TextElement)row.Children[0];
            Assert.Equal(list.Items[i], text.Text);
        }
    }

    [Fact]
    public void Items_Replacing_RebuildsEntirely()
    {
        var list = new LumiList { Items = ["a", "b"] };
        Assert.Equal(2, list.Root.Children.Count);

        list.Items = ["x", "y", "z", "w"];
        Assert.Equal(4, list.Root.Children.Count);
        Assert.Equal("x", ((TextElement)list.Root.Children[0].Children[0]).Text);
        Assert.Equal("w", ((TextElement)list.Root.Children[3].Children[0]).Text);
    }

    [Fact]
    public void Click_RowAtIndex_CallsOnItemClickWithThatIndex()
    {
        var list = new LumiList { Items = ["zero", "one", "two", "three"] };
        int? lastIdx = null;
        list.OnItemClick = i => lastIdx = i;

        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, list.Root.Children[2]);
        Assert.Equal(2, lastIdx);

        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, list.Root.Children[0]);
        Assert.Equal(0, lastIdx);
    }

    [Fact]
    public void Click_DoesNotThrow_WhenCallbackUnset()
    {
        var list = new LumiList { Items = ["a"] };
        list.OnItemClick = null;
        var ex = Record.Exception(() =>
            EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, list.Root.Children[0]));
        Assert.Null(ex);
    }
}
