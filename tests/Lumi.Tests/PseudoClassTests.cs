using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class PseudoClassTests
{
    private static BoxElement BuildSiblings(int count)
    {
        var parent = new BoxElement("ul");
        for (int i = 0; i < count; i++)
            parent.AddChild(new BoxElement("li"));
        return parent;
    }

    // ── :nth-child ──

    [Fact]
    public void NthChild_Odd_Matches_1st_3rd_5th()
    {
        var parent = BuildSiblings(5);
        Assert.True(SelectorMatcher.Matches(parent.Children[0], ":nth-child(odd)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[1], ":nth-child(odd)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[2], ":nth-child(odd)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[3], ":nth-child(odd)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[4], ":nth-child(odd)"));
    }

    [Fact]
    public void NthChild_Even_Matches_2nd_4th()
    {
        var parent = BuildSiblings(5);
        Assert.False(SelectorMatcher.Matches(parent.Children[0], ":nth-child(even)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[1], ":nth-child(even)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[2], ":nth-child(even)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[3], ":nth-child(even)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[4], ":nth-child(even)"));
    }

    [Fact]
    public void NthChild_Integer_Matches_Only_That_Position()
    {
        var parent = BuildSiblings(5);
        Assert.False(SelectorMatcher.Matches(parent.Children[0], ":nth-child(3)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[1], ":nth-child(3)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[2], ":nth-child(3)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[3], ":nth-child(3)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[4], ":nth-child(3)"));
    }

    [Fact]
    public void NthChild_2nPlus1_Same_As_Odd()
    {
        var parent = BuildSiblings(5);
        Assert.True(SelectorMatcher.Matches(parent.Children[0], ":nth-child(2n+1)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[1], ":nth-child(2n+1)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[2], ":nth-child(2n+1)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[3], ":nth-child(2n+1)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[4], ":nth-child(2n+1)"));
    }

    [Fact]
    public void NthChild_3n_Matches_3rd_6th_9th()
    {
        var parent = BuildSiblings(9);
        for (int i = 0; i < 9; i++)
        {
            bool expected = (i + 1) % 3 == 0;
            Assert.Equal(expected, SelectorMatcher.Matches(parent.Children[i], ":nth-child(3n)"));
        }
    }

    [Fact]
    public void NthChild_NegN_Plus3_Matches_First_3()
    {
        var parent = BuildSiblings(5);
        Assert.True(SelectorMatcher.Matches(parent.Children[0], ":nth-child(-n+3)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[1], ":nth-child(-n+3)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[2], ":nth-child(-n+3)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[3], ":nth-child(-n+3)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[4], ":nth-child(-n+3)"));
    }

    // ── :nth-last-child ──

    [Fact]
    public void NthLastChild_1_Matches_Last_Child()
    {
        var parent = BuildSiblings(5);
        for (int i = 0; i < 5; i++)
        {
            bool expected = i == 4;
            Assert.Equal(expected, SelectorMatcher.Matches(parent.Children[i], ":nth-last-child(1)"));
        }
    }

    [Fact]
    public void NthLastChild_Odd_Matches_From_End()
    {
        var parent = BuildSiblings(5);
        // Positions from end: child[0]=5, child[1]=4, child[2]=3, child[3]=2, child[4]=1
        // Odd positions from end: 1,3,5 → indices 4,2,0
        Assert.True(SelectorMatcher.Matches(parent.Children[0], ":nth-last-child(odd)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[1], ":nth-last-child(odd)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[2], ":nth-last-child(odd)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[3], ":nth-last-child(odd)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[4], ":nth-last-child(odd)"));
    }

    // ── :not ──

    [Fact]
    public void Not_Class_Excludes_Matching_Elements()
    {
        var parent = new BoxElement("ul");
        var visible = new BoxElement("li");
        var hidden = new BoxElement("li");
        hidden.Classes.Add("hidden");
        parent.AddChild(visible);
        parent.AddChild(hidden);

        Assert.True(SelectorMatcher.Matches(visible, ":not(.hidden)"));
        Assert.False(SelectorMatcher.Matches(hidden, ":not(.hidden)"));
    }

    [Fact]
    public void Not_Type_Excludes_Matching_Tag()
    {
        var div = new BoxElement("div");
        var span = new BoxElement("span");

        Assert.True(SelectorMatcher.Matches(div, ":not(span)"));
        Assert.False(SelectorMatcher.Matches(span, ":not(span)"));
    }

    // ── :is ──

    [Fact]
    public void Is_Matches_Any_Of_Multiple_Selectors()
    {
        var a = new BoxElement("div");
        a.Classes.Add("a");
        var b = new BoxElement("div");
        b.Classes.Add("b");
        var c = new BoxElement("div");
        c.Classes.Add("c");

        Assert.True(SelectorMatcher.Matches(a, ":is(.a, .b)"));
        Assert.True(SelectorMatcher.Matches(b, ":is(.a, .b)"));
        Assert.False(SelectorMatcher.Matches(c, ":is(.a, .b)"));
    }

    // ── Regression: :first-child and :last-child ──

    [Fact]
    public void FirstChild_Still_Works()
    {
        var parent = BuildSiblings(3);
        Assert.True(SelectorMatcher.Matches(parent.Children[0], ":first-child"));
        Assert.False(SelectorMatcher.Matches(parent.Children[1], ":first-child"));
        Assert.False(SelectorMatcher.Matches(parent.Children[2], ":first-child"));
    }

    [Fact]
    public void LastChild_Still_Works()
    {
        var parent = BuildSiblings(3);
        Assert.False(SelectorMatcher.Matches(parent.Children[0], ":last-child"));
        Assert.False(SelectorMatcher.Matches(parent.Children[1], ":last-child"));
        Assert.True(SelectorMatcher.Matches(parent.Children[2], ":last-child"));
    }

    // ── An+B parser edge cases ──

    [Fact]
    public void ParseAnPlusB_Handles_Edge_Cases()
    {
        Assert.Equal((2, 1), SelectorMatcher.ParseAnPlusB("odd"));
        Assert.Equal((2, 0), SelectorMatcher.ParseAnPlusB("even"));
        Assert.Equal((0, 3), SelectorMatcher.ParseAnPlusB("3"));
        Assert.Equal((2, 0), SelectorMatcher.ParseAnPlusB("2n"));
        Assert.Equal((2, 1), SelectorMatcher.ParseAnPlusB("2n+1"));
        Assert.Equal((-1, 3), SelectorMatcher.ParseAnPlusB("-n+3"));
        Assert.Equal((3, -2), SelectorMatcher.ParseAnPlusB("3n-2"));
        Assert.Equal((1, 0), SelectorMatcher.ParseAnPlusB("n"));
        Assert.Equal((1, 2), SelectorMatcher.ParseAnPlusB("n+2"));
    }

    // ── Compound selectors with pseudo-classes ──

    [Fact]
    public void NthChild_Works_In_Compound_Selector()
    {
        var parent = BuildSiblings(3);
        // li:nth-child(2) should match only the 2nd li
        Assert.False(SelectorMatcher.Matches(parent.Children[0], "li:nth-child(2)"));
        Assert.True(SelectorMatcher.Matches(parent.Children[1], "li:nth-child(2)"));
        Assert.False(SelectorMatcher.Matches(parent.Children[2], "li:nth-child(2)"));
    }
}
