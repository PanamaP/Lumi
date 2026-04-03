using Lumi.Core;
using Lumi.Core.Accessibility;
using Lumi.Platform;
using Lumi.Styling;

namespace Lumi.Tests;

public class AccessibilityTests
{
    // --- AriaParser Tests ---

    [Fact]
    public void AriaParser_RoleAttribute_SetsRole()
    {
        var element = new BoxElement("div");
        element.Attributes["role"] = "button";
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal("button", element.Accessibility.Role);
    }

    [Fact]
    public void AriaParser_AriaLabel_SetsLabel()
    {
        var element = new BoxElement("div");
        element.Attributes["aria-label"] = "Submit";
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal("Submit", element.Accessibility.Label);
    }

    [Fact]
    public void AriaParser_ButtonTag_InfersButtonRole()
    {
        var element = new BoxElement("button");
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal("button", element.Accessibility.Role);
    }

    [Fact]
    public void AriaParser_NavTag_InfersNavigationRole()
    {
        var element = new BoxElement("nav");
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal("navigation", element.Accessibility.Role);
    }

    [Fact]
    public void AriaParser_H1Tag_InfersHeadingRole()
    {
        var element = new BoxElement("h1");
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal("heading", element.Accessibility.Role);
    }

    [Fact]
    public void AriaParser_AriaHidden_MarksElementHidden()
    {
        var element = new BoxElement("div");
        element.Attributes["aria-hidden"] = "true";
        AriaParser.ApplyAriaAttributes(element);
        Assert.True(element.Accessibility.IsHidden);
    }

    [Fact]
    public void AriaParser_ExplicitRole_OverridesTagInference()
    {
        var element = new BoxElement("nav");
        element.Attributes["role"] = "menu";
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal("menu", element.Accessibility.Role);
    }

    [Fact]
    public void AriaParser_AriaLive_SetsLiveProperties()
    {
        var element = new BoxElement("div");
        element.Attributes["aria-live"] = "assertive";
        AriaParser.ApplyAriaAttributes(element);
        Assert.True(element.Accessibility.IsLive);
        Assert.Equal("assertive", element.Accessibility.LiveMode);
    }

    [Fact]
    public void AriaParser_AriaValues_SetsValueProperties()
    {
        var element = new BoxElement("div");
        element.Attributes["role"] = "slider";
        element.Attributes["aria-valuenow"] = "50";
        element.Attributes["aria-valuemin"] = "0";
        element.Attributes["aria-valuemax"] = "100";
        AriaParser.ApplyAriaAttributes(element);
        Assert.Equal(50f, element.Accessibility.ValueNow);
        Assert.Equal(0f, element.Accessibility.ValueMin);
        Assert.Equal(100f, element.Accessibility.ValueMax);
    }

    // --- AccessibilityTree Tests ---

    [Fact]
    public void AccessibilityTree_Build_CreatesCorrectStructure()
    {
        var root = new BoxElement("main");
        AriaParser.ApplyAriaAttributes(root);

        var nav = new BoxElement("nav");
        AriaParser.ApplyAriaAttributes(nav);
        root.AddChild(nav);

        var button = new BoxElement("button");
        AriaParser.ApplyAriaAttributes(button);
        nav.AddChild(button);

        var tree = AccessibilityTree.Build(root);

        Assert.Equal("main", tree.Role);
        Assert.Single(tree.Children);
        Assert.Equal("navigation", tree.Children[0].Role);
        Assert.Single(tree.Children[0].Children);
        Assert.Equal("button", tree.Children[0].Children[0].Role);
    }

    [Fact]
    public void AccessibilityTree_Build_FiltersAriaHiddenElements()
    {
        var root = new BoxElement("div");
        AriaParser.ApplyAriaAttributes(root);

        var visible = new BoxElement("button");
        AriaParser.ApplyAriaAttributes(visible);
        root.AddChild(visible);

        var hidden = new BoxElement("div");
        hidden.Attributes["aria-hidden"] = "true";
        AriaParser.ApplyAriaAttributes(hidden);
        root.AddChild(hidden);

        // Add a child to the hidden element — it should also be excluded
        var hiddenChild = new BoxElement("button");
        AriaParser.ApplyAriaAttributes(hiddenChild);
        hidden.AddChild(hiddenChild);

        var tree = AccessibilityTree.Build(root);

        Assert.Single(tree.Children);
        Assert.Equal("button", tree.Children[0].Role);
    }

    [Fact]
    public void AccessibilityTree_GetFocusableNodes_ReturnsOnlyFocusable()
    {
        var root = new BoxElement("div");
        AriaParser.ApplyAriaAttributes(root);

        var heading = new BoxElement("h1");
        AriaParser.ApplyAriaAttributes(heading);
        root.AddChild(heading);

        var button = new BoxElement("button");
        AriaParser.ApplyAriaAttributes(button);
        root.AddChild(button);

        var input = new InputElement();
        AriaParser.ApplyAriaAttributes(input);
        root.AddChild(input);

        var div = new BoxElement("div");
        AriaParser.ApplyAriaAttributes(div);
        root.AddChild(div);

        var tree = AccessibilityTree.Build(root);
        var focusable = AccessibilityTree.GetFocusableNodes(tree);

        // button (focusable + role), input (focusable + role)
        Assert.Equal(2, focusable.Count);
        Assert.Equal("button", focusable[0].Role);
        Assert.Equal("textbox", focusable[1].Role);
    }

    [Fact]
    public void AccessibilityTree_Name_ComputedFromLabel()
    {
        var element = new BoxElement("button");
        element.Attributes["aria-label"] = "Close dialog";
        AriaParser.ApplyAriaAttributes(element);

        var tree = AccessibilityTree.Build(element);
        Assert.Equal("Close dialog", tree.Name);
    }

    // --- HighContrastTheme Tests ---

    [Fact]
    public void HighContrastTheme_Apply_OverridesStyles()
    {
        var root = new BoxElement("div");
        root.ComputedStyle.BackgroundColor = new Color(200, 200, 200, 255);
        root.ComputedStyle.Color = new Color(50, 50, 50, 255);

        var button = new BoxElement("button");
        AriaParser.ApplyAriaAttributes(button);
        button.ComputedStyle.BackgroundColor = new Color(100, 100, 255, 255);
        root.AddChild(button);

        HighContrastTheme.Apply(root);

        Assert.Equal(HighContrastTheme.Background, root.ComputedStyle.BackgroundColor);
        Assert.Equal(HighContrastTheme.Foreground, root.ComputedStyle.Color);
        Assert.Equal(HighContrastTheme.ButtonFace, button.ComputedStyle.BackgroundColor);
        Assert.Equal(HighContrastTheme.ButtonText, button.ComputedStyle.Color);
    }

    [Fact]
    public void HighContrastTheme_GetHighContrastStyle_ReturnsDictionary()
    {
        var style = HighContrastTheme.GetHighContrastStyle();
        Assert.True(style.ContainsKey("background-color"));
        Assert.True(style.ContainsKey("color"));
        Assert.True(style.ContainsKey("border-color"));
    }

    // --- SystemPreferences Tests ---

    [Fact]
    public void SystemPreferences_Detect_DoesNotCrash()
    {
        var prefs = new SystemPreferences();
        prefs.Detect();
        // Should not throw, and should return valid defaults
        Assert.IsType<bool>(prefs.IsDarkMode);
        Assert.IsType<bool>(prefs.IsHighContrast);
    }

    // --- HtmlParser Integration ---

    [Fact]
    public void HtmlParser_SetsAccessibilityInfo_FromAttributes()
    {
        var root = HtmlTemplateParser.Parse(
            "<button aria-label=\"Save\" role=\"button\">Save</button>");

        var btn = root.Children[0];
        Assert.Equal("button", btn.Accessibility.Role);
        Assert.Equal("Save", btn.Accessibility.Label);
    }

    [Fact]
    public void HtmlParser_InfersRole_FromTagName()
    {
        var root = HtmlTemplateParser.Parse("<nav><main></main></nav>");

        var nav = root.Children[0];
        Assert.Equal("navigation", nav.Accessibility.Role);
        Assert.Single(nav.Children);
        Assert.Equal("main", nav.Children[0].Accessibility.Role);
    }
}
