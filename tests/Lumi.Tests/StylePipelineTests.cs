using ExCSS;
using Lumi.Core;
using Lumi.Styling;
using Xunit;

namespace Lumi.Tests;

/// <summary>
/// End-to-end tests that verify CSS properties survive the full pipeline:
/// CSS → ExCSS parse → PropertyApplier → StyleResolver.ApplyToComputedStyle → element.ComputedStyle
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
    public void ExCSS_DumpDeclarations_Diagnostic()
    {
        var parser = new StylesheetParser();
        var sheet = parser.Parse(".test { text-decoration: underline; overflow: hidden; border-style: solid; }");
        int count = 0;
        foreach (var rule in sheet.StyleRules)
            foreach (var _ in rule.Style.Declarations)
                count++;

        Assert.True(count > 0, "ExCSS should parse declarations");
    }
}
