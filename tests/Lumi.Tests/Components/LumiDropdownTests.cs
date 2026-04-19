using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets surviving mutants in LumiDropdown: open/close state machine, item rebuild,
/// list reparenting to root on open, selection state, and selected-row styling.
/// </summary>
public class LumiDropdownTests
{
    [Fact]
    public void Default_IsClosed_AndButtonShowsPlaceholder()
    {
        var dd = new LumiDropdown();
        Assert.False(dd.IsOpen);
        Assert.Equal(-1, dd.SelectedIndex);
        var buttonText = (TextElement)dd.Root.Children[0].Children[0];
        Assert.Equal("Select...", buttonText.Text);
    }

    [Fact]
    public void Items_AssigningNull_ReplacesWithEmptyList()
    {
        var dd = new LumiDropdown { Items = null! };
        Assert.NotNull(dd.Items);
        Assert.Empty(dd.Items);
    }

    [Fact]
    public void SelectedIndex_Negative_LeavesPlaceholderText()
    {
        var dd = new LumiDropdown { Items = ["A", "B", "C"] };
        dd.SelectedIndex = -1;
        var buttonText = (TextElement)dd.Root.Children[0].Children[0];
        Assert.Equal("Select...", buttonText.Text);
    }

    [Fact]
    public void SelectedIndex_OutOfRange_ShowsPlaceholder()
    {
        var dd = new LumiDropdown { Items = ["A", "B"] };
        dd.SelectedIndex = 99;
        var buttonText = (TextElement)dd.Root.Children[0].Children[0];
        Assert.Equal("Select...", buttonText.Text);
    }

    [Fact]
    public void SelectedIndex_Valid_UpdatesButtonText()
    {
        var dd = new LumiDropdown { Items = ["Apple", "Banana", "Cherry"] };
        dd.SelectedIndex = 1;
        var buttonText = (TextElement)dd.Root.Children[0].Children[0];
        Assert.Equal("Banana", buttonText.Text);
    }

    [Fact]
    public void Open_AttachesListToRootAndPopulatesItems()
    {
        var root = new BoxElement("root");
        var dd = new LumiDropdown { Items = ["X", "Y", "Z"] };
        root.AddChild(dd.Root);

        Click(dd.Root.Children[0]); // click button
        Assert.True(dd.IsOpen);

        // List should have been re-parented to the root.
        var list = root.Children.OfType<BoxElement>().LastOrDefault(c => c != dd.Root);
        Assert.NotNull(list);
        Assert.Equal(3, list!.Children.Count);
    }

    [Fact]
    public void Close_DetachesListFromTree()
    {
        var root = new BoxElement("root");
        var dd = new LumiDropdown { Items = ["X"] };
        root.AddChild(dd.Root);

        Click(dd.Root.Children[0]);
        Assert.Equal(2, root.Children.Count);

        Click(dd.Root.Children[0]);
        Assert.False(dd.IsOpen);
        Assert.Single(root.Children); // only the dropdown container remains
    }

    [Fact]
    public void ClickItem_SetsSelection_FiresCallback_AndCloses()
    {
        var root = new BoxElement("root");
        var dd = new LumiDropdown { Items = ["A", "B", "C"] };
        root.AddChild(dd.Root);

        int? received = null;
        dd.OnSelectionChanged = i => received = i;

        Click(dd.Root.Children[0]); // open
        var list = root.Children.Last(c => c != dd.Root);
        Click(list.Children[2]);

        Assert.Equal(2, received);
        Assert.Equal(2, dd.SelectedIndex);
        Assert.False(dd.IsOpen);
        var buttonText = (TextElement)dd.Root.Children[0].Children[0];
        Assert.Equal("C", buttonText.Text);
    }

    [Fact]
    public void Items_ReplacingList_RebuildsListContents()
    {
        var root = new BoxElement("root");
        var dd = new LumiDropdown { Items = ["A", "B"] };
        root.AddChild(dd.Root);
        Click(dd.Root.Children[0]); // open with 2 items
        var list = root.Children.Last(c => c != dd.Root);
        Assert.Equal(2, list.Children.Count);

        // Replacing items should rebuild the list (still attached because still open).
        dd.Items = ["X", "Y", "Z", "W"];
        // Re-find list (still same element).
        Assert.Equal(4, list.Children.Count);
    }

    [Fact]
    public void OpenedRow_SelectedIndex_HighlightedWithAccent()
    {
        var root = new BoxElement("root");
        var dd = new LumiDropdown { Items = ["A", "B", "C"] };
        root.AddChild(dd.Root);

        // Open and click the second item to trigger a rebuild that paints the selected row.
        Click(dd.Root.Children[0]);
        var list = root.Children.Last(c => c != dd.Root);
        Click(list.Children[1]);

        // Reopen and re-locate the (rebuilt) list.
        Click(dd.Root.Children[0]);
        list = root.Children.Last(c => c != dd.Root);
        var selectedRow = list.Children[1];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), selectedRow.InlineStyle);
    }

    [Fact]
    public void Open_AppliesAbsolutePositionToList()
    {
        var root = new BoxElement("root");
        var dd = new LumiDropdown { Items = ["A"] };
        root.AddChild(dd.Root);

        Click(dd.Root.Children[0]);
        var list = root.Children.Last(c => c != dd.Root);
        Assert.Contains("position: absolute", list.InlineStyle);
        Assert.Contains("max-height: 200px", list.InlineStyle);
    }

    private static void Click(Element target)
    {
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, target);
    }
}
