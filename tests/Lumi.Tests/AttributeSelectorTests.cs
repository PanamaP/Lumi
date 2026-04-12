using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class AttributeSelectorTests
{
    [Fact]
    public void Presence_Id_MatchesElementWithId()
    {
        var el = new BoxElement("div") { Id = "main" };
        Assert.True(SelectorMatcher.Matches(el, "[id]"));
    }

    [Fact]
    public void Presence_Id_DoesNotMatchElementWithoutId()
    {
        var el = new BoxElement("div");
        Assert.False(SelectorMatcher.Matches(el, "[id]"));
    }

    [Fact]
    public void Exact_Type_Checkbox_MatchesInputCheckbox()
    {
        var input = new InputElement { InputType = "checkbox" };
        Assert.True(SelectorMatcher.Matches(input, "[type=\"checkbox\"]"));
    }

    [Fact]
    public void Exact_Type_Text_MatchesInputText()
    {
        var input = new InputElement { InputType = "text" };
        Assert.True(SelectorMatcher.Matches(input, "[type=\"text\"]"));
    }

    [Fact]
    public void Exact_Type_Checkbox_DoesNotMatchText()
    {
        var input = new InputElement { InputType = "text" };
        Assert.False(SelectorMatcher.Matches(input, "[type=\"checkbox\"]"));
    }

    [Fact]
    public void Presence_Disabled_MatchesDisabledInput()
    {
        var input = new InputElement { IsDisabled = true };
        Assert.True(SelectorMatcher.Matches(input, "[disabled]"));
    }

    [Fact]
    public void Presence_Disabled_DoesNotMatchEnabledInput()
    {
        var input = new InputElement { IsDisabled = false };
        Assert.False(SelectorMatcher.Matches(input, "[disabled]"));
    }

    [Fact]
    public void WordMatch_Class_MatchesActiveInClassList()
    {
        var el = new BoxElement("div")
        {
            Classes = new ClassList(["btn", "active", "large"])
        };
        Assert.True(SelectorMatcher.Matches(el, "[class~=\"active\"]"));
    }

    [Fact]
    public void WordMatch_Class_DoesNotMatchPartialWord()
    {
        var el = new BoxElement("div")
        {
            Classes = new ClassList(["inactive"])
        };
        Assert.False(SelectorMatcher.Matches(el, "[class~=\"active\"]"));
    }

    [Fact]
    public void Exact_DataAttribute_MatchesDataRole()
    {
        var el = new BoxElement("nav");
        el.Attributes["data-role"] = "nav";
        Assert.True(SelectorMatcher.Matches(el, "[data-role=\"nav\"]"));
    }

    [Fact]
    public void Exact_DataAttribute_DoesNotMatchWrongValue()
    {
        var el = new BoxElement("nav");
        el.Attributes["data-role"] = "sidebar";
        Assert.False(SelectorMatcher.Matches(el, "[data-role=\"nav\"]"));
    }

    [Fact]
    public void StartsWith_Type_MatchesCheckPrefix()
    {
        var input = new InputElement { InputType = "checkbox" };
        Assert.True(SelectorMatcher.Matches(input, "[type^=\"check\"]"));
    }

    [Fact]
    public void StartsWith_DoesNotMatchWrongPrefix()
    {
        var input = new InputElement { InputType = "text" };
        Assert.False(SelectorMatcher.Matches(input, "[type^=\"check\"]"));
    }

    [Fact]
    public void EndsWith_Type_MatchesBoxSuffix()
    {
        var input = new InputElement { InputType = "checkbox" };
        Assert.True(SelectorMatcher.Matches(input, "[type$=\"box\"]"));
    }

    [Fact]
    public void EndsWith_DoesNotMatchWrongSuffix()
    {
        var input = new InputElement { InputType = "text" };
        Assert.False(SelectorMatcher.Matches(input, "[type$=\"box\"]"));
    }

    [Fact]
    public void Contains_Type_MatchesSubstring()
    {
        var input = new InputElement { InputType = "checkbox" };
        Assert.True(SelectorMatcher.Matches(input, "[type*=\"eck\"]"));
    }

    [Fact]
    public void Contains_DoesNotMatchAbsentSubstring()
    {
        var input = new InputElement { InputType = "text" };
        Assert.False(SelectorMatcher.Matches(input, "[type*=\"eck\"]"));
    }

    [Fact]
    public void Combined_TypeAndAttribute_MatchesInputCheckbox()
    {
        var input = new InputElement { InputType = "checkbox" };
        Assert.True(SelectorMatcher.Matches(input, @"input[type=""checkbox""]"));
    }

    [Fact]
    public void Combined_TypeAndAttribute_DoesNotMatchWrongTag()
    {
        var el = new BoxElement("div");
        el.Attributes["type"] = "checkbox";
        Assert.False(SelectorMatcher.Matches(el, "input[type=\"checkbox\"]"));
    }

    [Fact]
    public void DashMatch_MatchesExactValue()
    {
        var el = new BoxElement("div");
        el.Attributes["lang"] = "en";
        Assert.True(SelectorMatcher.Matches(el, "[lang|=\"en\"]"));
    }

    [Fact]
    public void DashMatch_MatchesValueWithHyphenPrefix()
    {
        var el = new BoxElement("div");
        el.Attributes["lang"] = "en-US";
        Assert.True(SelectorMatcher.Matches(el, "[lang|=\"en\"]"));
    }

    [Fact]
    public void DashMatch_DoesNotMatchDifferentValue()
    {
        var el = new BoxElement("div");
        el.Attributes["lang"] = "fr";
        Assert.False(SelectorMatcher.Matches(el, "[lang|=\"en\"]"));
    }

    [Fact]
    public void NoMatch_MissingAttribute_ReturnsFalse()
    {
        var el = new BoxElement("div");
        Assert.False(SelectorMatcher.Matches(el, "[data-missing]"));
    }

    [Fact]
    public void NoMatch_WrongValue_ReturnsFalse()
    {
        var input = new InputElement { InputType = "radio" };
        Assert.False(SelectorMatcher.Matches(input, "[type=\"checkbox\"]"));
    }
}
