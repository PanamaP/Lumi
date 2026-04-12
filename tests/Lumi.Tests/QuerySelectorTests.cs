using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class QuerySelectorTests
{
    [Fact]
    public void QuerySelector_TypeSelector_FindsFirstMatch()
    {
        var root = new BoxElement("div");
        var span = new BoxElement("span");
        root.AddChild(span);

        var result = root.QuerySelector("span");

        Assert.Same(span, result);
    }

    [Fact]
    public void QuerySelector_ClassSelector_FindsMatch()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        child.Classes.Add("active");
        root.AddChild(child);

        var result = root.QuerySelector(".active");

        Assert.Same(child, result);
    }

    [Fact]
    public void QuerySelector_IdSelector_FindsMatch()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("div") { Id = "header" };
        root.AddChild(child);

        var result = root.QuerySelector("#header");

        Assert.Same(child, result);
    }

    [Fact]
    public void QuerySelector_CompoundSelector_FindsMatch()
    {
        var root = new BoxElement("div");
        var match = new BoxElement("div");
        match.Classes.Add("active");
        var nonMatch = new BoxElement("span");
        nonMatch.Classes.Add("active");
        root.AddChild(nonMatch);
        root.AddChild(match);

        var result = root.QuerySelector("div.active");

        Assert.Same(match, result);
    }

    [Fact]
    public void QuerySelector_ReturnsNull_WhenNoMatch()
    {
        var root = new BoxElement("div");
        root.AddChild(new BoxElement("span"));

        var result = root.QuerySelector(".nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void QuerySelector_DoesNotMatchSelf()
    {
        var root = new BoxElement("div");
        root.Classes.Add("target");

        var result = root.QuerySelector("div.target");

        Assert.Null(result);
    }

    [Fact]
    public void QuerySelector_FindsDeepDescendant()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        var grandchild = new BoxElement("span") { Id = "deep" };
        root.AddChild(child);
        child.AddChild(grandchild);

        var result = root.QuerySelector("#deep");

        Assert.Same(grandchild, result);
    }

    [Fact]
    public void QuerySelector_ReturnsFirstInTreeOrder()
    {
        var root = new BoxElement("div");
        var first = new BoxElement("span");
        var second = new BoxElement("span");
        root.AddChild(first);
        root.AddChild(second);

        var result = root.QuerySelector("span");

        Assert.Same(first, result);
    }

    [Fact]
    public void QuerySelectorAll_TypeSelector_FindsAll()
    {
        var root = new BoxElement("div");
        var p1 = new BoxElement("p");
        var p2 = new BoxElement("p");
        var span = new BoxElement("span");
        root.AddChild(p1);
        root.AddChild(span);
        root.AddChild(p2);

        var results = root.QuerySelectorAll("p");

        Assert.Equal(2, results.Count);
        Assert.Same(p1, results[0]);
        Assert.Same(p2, results[1]);
    }

    [Fact]
    public void QuerySelectorAll_ClassSelector_FindsAll()
    {
        var root = new BoxElement("div");
        var a = new BoxElement("div");
        a.Classes.Add("active");
        var b = new BoxElement("span");
        b.Classes.Add("active");
        var c = new BoxElement("div");
        c.Classes.Add("inactive");
        root.AddChild(a);
        root.AddChild(b);
        root.AddChild(c);

        var results = root.QuerySelectorAll(".active");

        Assert.Equal(2, results.Count);
        Assert.Contains(a, results);
        Assert.Contains(b, results);
    }

    [Fact]
    public void QuerySelectorAll_ReturnsEmpty_WhenNoMatch()
    {
        var root = new BoxElement("div");
        root.AddChild(new BoxElement("span"));

        var results = root.QuerySelectorAll(".nothing");

        Assert.Empty(results);
    }

    [Fact]
    public void QuerySelectorAll_DescendantCombinator_FindsMatches()
    {
        // Structure: div > section > p, div > p
        var root = new BoxElement("div");
        var section = new BoxElement("section");
        var deepP = new BoxElement("p");
        var directP = new BoxElement("p");
        root.AddChild(section);
        section.AddChild(deepP);
        root.AddChild(directP);

        var results = root.QuerySelectorAll("div p");

        Assert.Equal(2, results.Count);
        Assert.Same(deepP, results[0]);
        Assert.Same(directP, results[1]);
    }

    [Fact]
    public void QuerySelectorAll_ChildCombinator_FindsDirectChildrenOnly()
    {
        var root = new BoxElement("div");
        var directSpan = new BoxElement("span");
        var nested = new BoxElement("section");
        var deepSpan = new BoxElement("span");
        root.AddChild(directSpan);
        root.AddChild(nested);
        nested.AddChild(deepSpan);

        var results = root.QuerySelectorAll("div > span");

        Assert.Single(results);
        Assert.Same(directSpan, results[0]);
    }

    [Fact]
    public void QuerySelectorAll_ResultsInTreeOrder()
    {
        //   root(div)
        //   ├── a(p)
        //   │   └── b(p)
        //   └── c(p)
        var root = new BoxElement("div");
        var a = new BoxElement("p") { Id = "a" };
        var b = new BoxElement("p") { Id = "b" };
        var c = new BoxElement("p") { Id = "c" };
        root.AddChild(a);
        a.AddChild(b);
        root.AddChild(c);

        var results = root.QuerySelectorAll("p");

        Assert.Equal(3, results.Count);
        Assert.Same(a, results[0]);
        Assert.Same(b, results[1]);
        Assert.Same(c, results[2]);
    }

    [Fact]
    public void QuerySelectorAll_DoesNotIncludeSelf()
    {
        var root = new BoxElement("div");
        root.Classes.Add("target");
        var child = new BoxElement("div");
        child.Classes.Add("target");
        root.AddChild(child);

        var results = root.QuerySelectorAll("div.target");

        Assert.Single(results);
        Assert.Same(child, results[0]);
    }
}
