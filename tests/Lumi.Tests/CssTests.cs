using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssTests
{
    [Fact]
    public void CssParser_ParsesBasicRule()
    {
        var sheet = CssParser.Parse(".container { background-color: #FF0000; padding: 10px; }");

        Assert.Single(sheet.Rules);
        var rule = sheet.Rules[0];
        Assert.Equal(".container", rule.SelectorText);
        Assert.True(rule.Declarations.Count >= 2);
    }

    [Fact]
    public void CssParser_ParsesMultipleRules()
    {
        var sheet = CssParser.Parse(@"
            .header { background-color: blue; }
            .content { padding: 20px; }
            #main { width: 100px; }
        ");

        Assert.Equal(3, sheet.Rules.Count);
    }

    [Fact]
    public void SelectorMatcher_MatchesClass()
    {
        var element = new BoxElement("div");
        element.Classes.Add("container");

        Assert.True(SelectorMatcher.Matches(element, ".container"));
        Assert.False(SelectorMatcher.Matches(element, ".other"));
    }

    [Fact]
    public void SelectorMatcher_MatchesId()
    {
        var element = new BoxElement("div") { Id = "main" };

        Assert.True(SelectorMatcher.Matches(element, "#main"));
        Assert.False(SelectorMatcher.Matches(element, "#other"));
    }

    [Fact]
    public void SelectorMatcher_MatchesTagName()
    {
        var element = new BoxElement("div");

        Assert.True(SelectorMatcher.Matches(element, "div"));
        Assert.False(SelectorMatcher.Matches(element, "span"));
    }

    [Fact]
    public void SelectorMatcher_MatchesCompound()
    {
        var element = new BoxElement("div");
        element.Classes.Add("container");

        Assert.True(SelectorMatcher.Matches(element, "div.container"));
        Assert.False(SelectorMatcher.Matches(element, "span.container"));
    }

    [Fact]
    public void SelectorMatcher_MatchesUniversal()
    {
        var element = new BoxElement("anything");
        Assert.True(SelectorMatcher.Matches(element, "*"));
    }

    [Fact]
    public void SelectorMatcher_MatchesCommaGroup()
    {
        var div = new BoxElement("div");
        var span = new TextElement();

        Assert.True(SelectorMatcher.Matches(div, "div, span"));
        Assert.True(SelectorMatcher.Matches(span, "div, span"));
    }

    [Fact]
    public void PropertyApplier_BackgroundColor()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "background-color", "#3B82F6");

        Assert.Equal(0x3B, style.BackgroundColor.R);
        Assert.Equal(0x82, style.BackgroundColor.G);
        Assert.Equal(0xF6, style.BackgroundColor.B);
    }

    [Fact]
    public void PropertyApplier_Dimensions()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "width", "200px");
        PropertyApplier.Apply(style, "height", "100px");

        Assert.Equal(200, style.Width);
        Assert.Equal(100, style.Height);
    }

    [Fact]
    public void PropertyApplier_Display()
    {
        var style = new ComputedStyle();

        PropertyApplier.Apply(style, "display", "flex");
        Assert.Equal(DisplayMode.Flex, style.Display);

        PropertyApplier.Apply(style, "display", "none");
        Assert.Equal(DisplayMode.None, style.Display);
    }

    [Fact]
    public void PropertyApplier_MarginShorthand()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "margin", "10px 20px");

        Assert.Equal(10, style.Margin.Top);
        Assert.Equal(20, style.Margin.Right);
        Assert.Equal(10, style.Margin.Bottom);
        Assert.Equal(20, style.Margin.Left);
    }

    [Fact]
    public void PropertyApplier_FontProperties()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "font-size", "24px");
        PropertyApplier.Apply(style, "font-weight", "bold");

        Assert.Equal(24, style.FontSize);
        Assert.Equal(700, style.FontWeight);
    }

    [Fact]
    public void StyleResolver_AppliesClassSelector()
    {
        var sheet = CssParser.Parse(".highlight { background-color: #FF0000; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        var child = new BoxElement("div");
        child.Classes.Add("highlight");
        root.AddChild(child);

        resolver.ResolveStyles(root);

        Assert.Equal(255, child.ComputedStyle.BackgroundColor.R);
        Assert.Equal(0, child.ComputedStyle.BackgroundColor.G);
    }

    [Fact]
    public void StyleResolver_CascadeOrder_IdBeatsClass()
    {
        var sheet = CssParser.Parse(@"
            .blue { background-color: #0000FF; }
            #special { background-color: #FF0000; }
        ");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var element = new BoxElement("div") { Id = "special" };
        element.Classes.Add("blue");
        var root = new BoxElement("body");
        root.AddChild(element);

        resolver.ResolveStyles(root);

        // ID (#special) has higher specificity than class (.blue)
        Assert.Equal(255, element.ComputedStyle.BackgroundColor.R);
        Assert.Equal(0, element.ComputedStyle.BackgroundColor.B);
    }

    [Fact]
    public void StyleResolver_InheritsColor()
    {
        var sheet = CssParser.Parse("body { color: #FF0000; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("body");
        var child = new BoxElement("div");
        root.AddChild(child);

        resolver.ResolveStyles(root);

        // Color should inherit from parent
        Assert.Equal(255, child.ComputedStyle.Color.R);
    }

    [Fact]
    public void Specificity_ComparisonWorks()
    {
        var idSpec = new SelectorSpecificity(1, 0, 0);
        var classSpec = new SelectorSpecificity(0, 1, 0);
        var typeSpec = new SelectorSpecificity(0, 0, 1);

        Assert.True(idSpec > classSpec);
        Assert.True(classSpec > typeSpec);
        Assert.True(idSpec > typeSpec);
    }

    // --- Phase 5A: CSS Text Properties ---

    [Fact]
    public void PropertyApplier_WhiteSpaceNoWrap()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "white-space", "nowrap");
        Assert.Equal(WhiteSpace.NoWrap, style.WhiteSpace);
    }

    [Fact]
    public void PropertyApplier_TextOverflowEllipsis()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "text-overflow", "ellipsis");
        Assert.Equal(TextOverflow.Ellipsis, style.TextOverflow);
    }

    [Fact]
    public void PropertyApplier_WordBreakBreakAll()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "word-break", "break-all");
        Assert.Equal(WordBreak.BreakAll, style.WordBreak);
    }

    [Fact]
    public void PropertyApplier_TextDecorationUnderline()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "text-decoration", "underline");
        Assert.Equal(TextDecoration.Underline, style.TextDecoration);
    }

    [Fact]
    public void PropertyApplier_TextDecorationLineThrough()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "text-decoration", "line-through");
        Assert.Equal(TextDecoration.LineThrough, style.TextDecoration);
    }

    [Fact]
    public void PropertyApplier_TextTransformUppercase()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "text-transform", "uppercase");
        Assert.Equal(TextTransform.Uppercase, style.TextTransform);
    }

    // --- Phase 5D: Box Shadow ---

    [Fact]
    public void PropertyApplier_BoxShadow_ParsesCorrectly()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "box-shadow", "2px 4px 8px 0px rgba(0,0,0,0.3)");

        Assert.Equal(2, style.BoxShadow.OffsetX);
        Assert.Equal(4, style.BoxShadow.OffsetY);
        Assert.Equal(8, style.BoxShadow.BlurRadius);
        Assert.Equal(0, style.BoxShadow.SpreadRadius);
        Assert.False(style.BoxShadow.Inset);
    }

    [Fact]
    public void PropertyApplier_BoxShadow_ParsesInsetKeyword()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "box-shadow", "inset 2px 4px 8px 0px rgba(0,0,0,0.5)");

        Assert.True(style.BoxShadow.Inset);
        Assert.Equal(2, style.BoxShadow.OffsetX);
        Assert.Equal(4, style.BoxShadow.OffsetY);
    }

    [Fact]
    public void PropertyApplier_BoxShadow_NoneIsNone()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "box-shadow", "none");

        Assert.True(style.BoxShadow.IsNone);
    }

    // --- CSS Variables ---

    [Fact]
    public void PropertyApplier_CustomProperty_StoredInCustomProperties()
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "--custom-color", "#FF0000");

        Assert.True(style.CustomProperties.ContainsKey("--custom-color"));
        Assert.Equal("#FF0000", style.CustomProperties["--custom-color"]);
    }

    [Fact]
    public void PropertyApplier_VarFunction_ResolvesFromCustomProperties()
    {
        var style = new ComputedStyle();
        style.CustomProperties["--main-bg"] = "#00FF00";

        PropertyApplier.Apply(style, "background-color", "var(--main-bg)");

        Assert.Equal(0, style.BackgroundColor.R);
        Assert.Equal(255, style.BackgroundColor.G);
        Assert.Equal(0, style.BackgroundColor.B);
    }

    [Fact]
    public void PropertyApplier_VarFunction_UsesFallbackWhenMissing()
    {
        var style = new ComputedStyle();

        PropertyApplier.Apply(style, "background-color", "var(--missing, red)");

        Assert.Equal(255, style.BackgroundColor.R);
        Assert.Equal(0, style.BackgroundColor.G);
        Assert.Equal(0, style.BackgroundColor.B);
    }
}
