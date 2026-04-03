using Lumi.Core;
using Lumi.Styling;
using Xunit;

namespace Lumi.Tests;

/// <summary>
/// End-to-end tests that verify CSS properties survive the full pipeline:
/// CSS → CssParser → PropertyApplier → StyleResolver.ApplyToComputedStyle → element.ComputedStyle
/// </summary>
public class StylePipelineTests
{
    [Fact]
    public void TextDecoration_Underline_AppliedThroughFullPipeline()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><span class=\"u\">Underlined</span><span class=\"s\">Struck</span></div>");
        var css = CssParser.Parse(".u { text-decoration: underline; } .s { text-decoration: line-through; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var uSpan = root.Children[0].Children[0];
        var sSpan = root.Children[0].Children[1];

        Assert.Equal(TextDecoration.Underline, uSpan.ComputedStyle.TextDecoration);
        Assert.Equal(TextDecoration.LineThrough, sSpan.ComputedStyle.TextDecoration);
    }

    [Fact]
    public void OverflowHidden_AppliedThroughFullPipeline()
    {
        var root = HtmlTemplateParser.Parse("<div class=\"clip\"><div>child</div></div>");
        var css = CssParser.Parse(".clip { overflow: hidden; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var clipDiv = root.Children[0];
        Assert.Equal(Lumi.Core.Overflow.Hidden, clipDiv.ComputedStyle.Overflow);
    }

    [Fact]
    public void LightweightParser_ParsesDeclarations()
    {
        var sheet = CssParser.Parse(".test { text-decoration: underline; overflow: hidden; border-style: solid; }");
        Assert.Single(sheet.Rules);
        Assert.Equal(3, sheet.Rules[0].Declarations.Count);
        Assert.Equal(".test", sheet.Rules[0].SelectorText);
    }

    [Fact]
    public void LightweightParser_SpecificityCalculation()
    {
        var sheet = CssParser.Parse(
            "#id { color: red; } .cls { color: blue; } div { color: green; } div.cls#id { color: black; }");

        Assert.Equal(4, sheet.Rules.Count);
        Assert.Equal(new SelectorSpecificity(1, 0, 0), sheet.Rules[0].Specificity); // #id
        Assert.Equal(new SelectorSpecificity(0, 1, 0), sheet.Rules[1].Specificity); // .cls
        Assert.Equal(new SelectorSpecificity(0, 0, 1), sheet.Rules[2].Specificity); // div
        Assert.Equal(new SelectorSpecificity(1, 1, 1), sheet.Rules[3].Specificity); // div.cls#id
    }

    [Fact]
    public void LightweightParser_CommaSeparatedSelectors()
    {
        var sheet = CssParser.Parse(".a, .b, .c { color: red; }");
        Assert.Equal(3, sheet.Rules.Count);
        Assert.Equal(".a", sheet.Rules[0].SelectorText);
        Assert.Equal(".b", sheet.Rules[1].SelectorText);
        Assert.Equal(".c", sheet.Rules[2].SelectorText);
    }

    [Fact]
    public void LightweightParser_InlineStyleParsing()
    {
        var decls = CssParser.ParseInlineStyle("color: red; font-size: 14px; background-color: #fff");
        Assert.Equal(3, decls.Count);
        Assert.Equal("color", decls[0].Property);
        Assert.Equal("red", decls[0].Value);
        Assert.Equal("font-size", decls[1].Property);
        Assert.Equal("14px", decls[1].Value);
    }

    [Fact]
    public void LightweightParser_CommentsStripped()
    {
        var sheet = CssParser.Parse("/* comment */ .test { /* inside */ color: red; /* end */ }");
        Assert.Single(sheet.Rules);
        Assert.Single(sheet.Rules[0].Declarations);
        Assert.Equal("color", sheet.Rules[0].Declarations[0].Property);
    }

    [Fact]
    public void LightweightParser_PseudoClassSpecificity()
    {
        var sheet = CssParser.Parse(".btn:hover { color: red; }");
        Assert.Equal(new SelectorSpecificity(0, 2, 0), sheet.Rules[0].Specificity); // .btn + :hover
    }
}
