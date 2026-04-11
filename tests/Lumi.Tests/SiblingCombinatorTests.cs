using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class SiblingCombinatorTests
{
    [Fact]
    public void AdjacentSibling_MatchesImmediateNextSibling()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var p = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(p);

        Assert.True(SelectorMatcher.Matches(p, "h1 + p"));
    }

    [Fact]
    public void AdjacentSibling_DoesNotMatchNonImmediateSibling()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var span = new BoxElement("span");
        var p = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(span);
        parent.AddChild(p);

        // p is not immediately after h1 (span is in between)
        Assert.False(SelectorMatcher.Matches(p, "h1 + p"));
    }

    [Fact]
    public void GeneralSibling_MatchesAnySubsequentSibling()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var span = new BoxElement("span");
        var p = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(span);
        parent.AddChild(p);

        // p is a subsequent sibling of h1, even though not immediate
        Assert.True(SelectorMatcher.Matches(p, "h1 ~ p"));
    }

    [Fact]
    public void GeneralSibling_DoesNotMatchPrecedingSibling()
    {
        var parent = new BoxElement("div");
        var p = new BoxElement("p");
        var h1 = new BoxElement("h1");
        parent.AddChild(p);
        parent.AddChild(h1);

        // p is before h1, not after
        Assert.False(SelectorMatcher.Matches(p, "h1 ~ p"));
    }

    [Fact]
    public void AdjacentSibling_WithClassSelectors()
    {
        var parent = new BoxElement("div");
        var a = new BoxElement("span");
        a.Classes.Add("a");
        var b = new BoxElement("span");
        b.Classes.Add("b");
        parent.AddChild(a);
        parent.AddChild(b);

        Assert.True(SelectorMatcher.Matches(b, ".a + .b"));
        Assert.False(SelectorMatcher.Matches(a, ".a + .b"));
    }

    [Fact]
    public void AdjacentSibling_CombinedWithChildCombinator()
    {
        var root = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var p = new BoxElement("p");
        root.AddChild(h1);
        root.AddChild(p);

        Assert.True(SelectorMatcher.Matches(p, "div > h1 + p"));
    }

    [Fact]
    public void AdjacentSibling_NoSiblings()
    {
        var parent = new BoxElement("div");
        var p = new BoxElement("p");
        parent.AddChild(p);

        // No siblings at all
        Assert.False(SelectorMatcher.Matches(p, "h1 + p"));
    }

    [Fact]
    public void GeneralSibling_NoSiblings()
    {
        var parent = new BoxElement("div");
        var p = new BoxElement("p");
        parent.AddChild(p);

        Assert.False(SelectorMatcher.Matches(p, "h1 ~ p"));
    }

    [Fact]
    public void AdjacentSibling_NoParent()
    {
        var p = new BoxElement("p");

        Assert.False(SelectorMatcher.Matches(p, "h1 + p"));
    }

    [Fact]
    public void GeneralSibling_MultipleSiblings_MatchesCorrectOnes()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var p1 = new BoxElement("p");
        var span = new BoxElement("span");
        var p2 = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(p1);
        parent.AddChild(span);
        parent.AddChild(p2);

        // Both p elements are after h1
        Assert.True(SelectorMatcher.Matches(p1, "h1 ~ p"));
        Assert.True(SelectorMatcher.Matches(p2, "h1 ~ p"));
        // span is after h1 but doesn't match "p"
        Assert.False(SelectorMatcher.Matches(span, "h1 ~ p"));
        // h1 itself doesn't match
        Assert.False(SelectorMatcher.Matches(h1, "h1 ~ p"));
    }

    [Fact]
    public void AdjacentSibling_ImmediateAfterMatchesSecondOfThree()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var p1 = new BoxElement("p");
        var p2 = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(p1);
        parent.AddChild(p2);

        // p1 is immediately after h1 → matches
        Assert.True(SelectorMatcher.Matches(p1, "h1 + p"));
        // p2 is immediately after p1, not h1 → does not match
        Assert.False(SelectorMatcher.Matches(p2, "h1 + p"));
    }

    [Fact]
    public void AdjacentSibling_WhitespaceVariations()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var p = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(p);

        // Various whitespace around +
        Assert.True(SelectorMatcher.Matches(p, "h1+p"));
        Assert.True(SelectorMatcher.Matches(p, "h1 +p"));
        Assert.True(SelectorMatcher.Matches(p, "h1+ p"));
        Assert.True(SelectorMatcher.Matches(p, "h1 + p"));
    }

    [Fact]
    public void GeneralSibling_WhitespaceVariations()
    {
        var parent = new BoxElement("div");
        var h1 = new BoxElement("h1");
        var span = new BoxElement("span");
        var p = new BoxElement("p");
        parent.AddChild(h1);
        parent.AddChild(span);
        parent.AddChild(p);

        Assert.True(SelectorMatcher.Matches(p, "h1~p"));
        Assert.True(SelectorMatcher.Matches(p, "h1 ~p"));
        Assert.True(SelectorMatcher.Matches(p, "h1~ p"));
        Assert.True(SelectorMatcher.Matches(p, "h1 ~ p"));
    }

    [Fact]
    public void GeneralSibling_CombinedWithDescendantCombinator()
    {
        var root = new BoxElement("section");
        var container = new BoxElement("div");
        root.AddChild(container);

        var h1 = new BoxElement("h1");
        var p = new BoxElement("p");
        container.AddChild(h1);
        container.AddChild(p);

        // "section div h1 ~ p" — descendant then general sibling
        Assert.True(SelectorMatcher.Matches(p, "section h1 ~ p"));
    }
}
