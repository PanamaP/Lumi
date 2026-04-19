using Lumi.Core;

namespace Lumi.Tests.Core;

/// <summary>
/// Targets surviving mutants in ClassList: deduplicated Add/Remove/Insert,
/// indexer self-replacement, IndexOf, IsReadOnly, enumeration order.
/// </summary>
public class ClassListTests
{
    [Fact]
    public void Default_IsEmpty_AndNotReadOnly()
    {
        var cl = new ClassList();
        Assert.Empty(cl);
        Assert.False(cl.IsReadOnly);
    }

    [Fact]
    public void Constructor_FromIEnumerable_DeduplicatesAndPreservesOrder()
    {
        var cl = new ClassList(new[] { "a", "b", "a", "c", "b", "d" });
        Assert.Equal(new[] { "a", "b", "c", "d" }, cl);
    }

    [Fact]
    public void Add_RejectsDuplicates_KeepsCount()
    {
        var cl = new ClassList();
        cl.Add("primary");
        cl.Add("primary");
        cl.Add("primary");
        Assert.Single(cl);
        Assert.Equal("primary", cl[0]);
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalseAndDoesNotChangeCount()
    {
        var cl = new ClassList(new[] { "a", "b" });
        Assert.False(cl.Remove("ghost"));
        Assert.Equal(2, cl.Count);
    }

    [Fact]
    public void Remove_Existing_ReturnsTrueAndRemovesEntry()
    {
        var cl = new ClassList(new[] { "a", "b", "c" });
        Assert.True(cl.Remove("b"));
        Assert.Equal(new[] { "a", "c" }, cl);
    }

    [Fact]
    public void Insert_AtIndex_PlacesEntry_AndRejectsDuplicates()
    {
        var cl = new ClassList(new[] { "a", "c" });
        cl.Insert(1, "b");
        Assert.Equal(new[] { "a", "b", "c" }, cl);

        cl.Insert(0, "b"); // duplicate, no-op
        Assert.Equal(new[] { "a", "b", "c" }, cl);
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex_OrMinusOne()
    {
        var cl = new ClassList(new[] { "x", "y", "z" });
        Assert.Equal(0, cl.IndexOf("x"));
        Assert.Equal(2, cl.IndexOf("z"));
        Assert.Equal(-1, cl.IndexOf("missing"));
    }

    [Fact]
    public void Indexer_AssigningSameValue_NoOp()
    {
        var cl = new ClassList(new[] { "a", "b" });
        cl[0] = "a";
        Assert.Equal(new[] { "a", "b" }, cl);
    }

    [Fact]
    public void Indexer_AssigningExistingOtherValue_RemovesOldSlot()
    {
        var cl = new ClassList(new[] { "a", "b", "c" });
        cl[0] = "c";
        Assert.Equal(new[] { "b", "c" }, cl);
    }

    [Fact]
    public void Indexer_AssigningNewValue_ReplacesAtSameIndex()
    {
        var cl = new ClassList(new[] { "a", "b", "c" });
        cl[1] = "X";
        Assert.Equal(new[] { "a", "X", "c" }, cl);
    }

    [Fact]
    public void Clear_OnEmpty_NoOp_OnNonEmpty_ResetsAll()
    {
        var cl = new ClassList();
        cl.Clear();
        Assert.Empty(cl);

        cl.Add("a"); cl.Add("b");
        cl.Clear();
        Assert.Empty(cl);
    }

    [Fact]
    public void RemoveAt_RemovesEntryAtIndex()
    {
        var cl = new ClassList(new[] { "a", "b", "c" });
        cl.RemoveAt(1);
        Assert.Equal(new[] { "a", "c" }, cl);
    }

    [Fact]
    public void Contains_HitMissCases()
    {
        var cl = new ClassList(new[] { "x" });
        Assert.Contains("x", cl);
        Assert.DoesNotContain("y", cl);
    }

    [Fact]
    public void SetFrom_ReplacesContentsAndPreservesDeduplication()
    {
        var cl = new ClassList(new[] { "a", "b" });
        cl.SetFrom(new[] { "x", "y", "x", "z" });
        Assert.Equal(new[] { "x", "y", "z" }, cl);
    }

    [Fact]
    public void CopyTo_FillsArrayFromArrayIndex()
    {
        var cl = new ClassList(new[] { "a", "b", "c" });
        var arr = new string[5];
        cl.CopyTo(arr, 1);
        Assert.Equal(new[] { null, "a", "b", "c", null }, arr);
    }
}
