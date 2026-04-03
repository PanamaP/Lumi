using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class HtmlParserTests
{
    [Fact]
    public void Parse_EmptyBody_ReturnsEmptyRoot()
    {
        var root = HtmlTemplateParser.Parse("<html><body></body></html>");
        Assert.Equal("body", root.TagName);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void Parse_DivWithId_MapsId()
    {
        var root = HtmlTemplateParser.Parse("<div id=\"main\"></div>");
        Assert.Single(root.Children);
        Assert.Equal("main", root.Children[0].Id);
    }

    [Fact]
    public void Parse_DivWithClasses_MapsClasses()
    {
        var root = HtmlTemplateParser.Parse("<div class=\"container flex\"></div>");
        var div = root.Children[0];
        Assert.Equal(["container", "flex"], div.Classes);
    }

    [Fact]
    public void Parse_NestedElements_BuildsTree()
    {
        var root = HtmlTemplateParser.Parse(@"
            <div id=""outer"">
                <div id=""inner"">
                    <span>Hello</span>
                </div>
            </div>
        ");

        var outer = root.Children[0];
        Assert.Equal("outer", outer.Id);
        Assert.Single(outer.Children);

        var inner = outer.Children[0];
        Assert.Equal("inner", inner.Id);
        Assert.Single(inner.Children);

        var text = inner.Children[0] as TextElement;
        Assert.NotNull(text);
        Assert.Equal("Hello", text!.Text);
    }

    [Fact]
    public void Parse_InputElement_MapsAttributes()
    {
        var root = HtmlTemplateParser.Parse(
            "<input type=\"text\" value=\"hello\" placeholder=\"Enter...\" disabled />");

        var input = root.Children[0] as InputElement;
        Assert.NotNull(input);
        Assert.Equal("text", input.InputType);
        Assert.Equal("hello", input.Value);
        Assert.Equal("Enter...", input.Placeholder);
        Assert.True(input.IsDisabled);
    }

    [Fact]
    public void Parse_ImageElement_MapsSrc()
    {
        var root = HtmlTemplateParser.Parse("<img src=\"logo.png\" />");

        var img = root.Children[0] as ImageElement;
        Assert.NotNull(img);
        Assert.Equal("logo.png", img.Source);
    }

    [Fact]
    public void Parse_InlineStyle_MapsToProperty()
    {
        var root = HtmlTemplateParser.Parse("<div style=\"color: red;\"></div>");
        Assert.Equal("color: red;", root.Children[0].InlineStyle);
    }
}
