using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class InheritablePropertiesTests
{
    // --- IsInheritable ---

    [Theory]
    [InlineData("color", true)]
    [InlineData("font-family", true)]
    [InlineData("font-size", true)]
    [InlineData("font-weight", true)]
    [InlineData("font-style", true)]
    [InlineData("line-height", true)]
    [InlineData("text-align", true)]
    [InlineData("letter-spacing", true)]
    [InlineData("cursor", true)]
    [InlineData("visibility", true)]
    [InlineData("background-color", false)]
    [InlineData("width", false)]
    [InlineData("height", false)]
    [InlineData("margin", false)]
    [InlineData("padding", false)]
    [InlineData("display", false)]
    [InlineData("opacity", false)]
    [InlineData("border-radius", false)]
    public void IsInheritable_ReturnsCorrectResult(string property, bool expected)
    {
        Assert.Equal(expected, InheritableProperties.IsInheritable(property));
    }

    // --- InheritFrom: basic inheritance ---

    [Fact]
    public void InheritFrom_CopiesColor()
    {
        var parent = new ComputedStyle { Color = new Color(255, 0, 0, 255) };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(new Color(255, 0, 0, 255), child.Color);
    }

    [Fact]
    public void InheritFrom_CopiesFontFamily()
    {
        var parent = new ComputedStyle { FontFamily = "Roboto" };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal("Roboto", child.FontFamily);
    }

    [Fact]
    public void InheritFrom_CopiesFontSize()
    {
        var parent = new ComputedStyle { FontSize = 24f };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(24f, child.FontSize);
    }

    [Fact]
    public void InheritFrom_CopiesFontWeight()
    {
        var parent = new ComputedStyle { FontWeight = 700 };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(700, child.FontWeight);
    }

    [Fact]
    public void InheritFrom_CopiesFontStyle()
    {
        var parent = new ComputedStyle { FontStyle = FontStyle.Italic };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(FontStyle.Italic, child.FontStyle);
    }

    [Fact]
    public void InheritFrom_CopiesLineHeight()
    {
        var parent = new ComputedStyle { LineHeight = 1.6f };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(1.6f, child.LineHeight);
    }

    [Fact]
    public void InheritFrom_CopiesTextAlign()
    {
        var parent = new ComputedStyle { TextAlign = TextAlign.Center };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(TextAlign.Center, child.TextAlign);
    }

    [Fact]
    public void InheritFrom_CopiesLetterSpacing()
    {
        var parent = new ComputedStyle { LetterSpacing = 2.5f };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(2.5f, child.LetterSpacing);
    }

    [Fact]
    public void InheritFrom_CopiesCursor()
    {
        var parent = new ComputedStyle { Cursor = "pointer" };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal("pointer", child.Cursor);
    }

    [Fact]
    public void InheritFrom_CopiesVisibility()
    {
        var parent = new ComputedStyle { Visibility = Visibility.Hidden };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal(Visibility.Hidden, child.Visibility);
    }

    // --- ExplicitlySet prevents inheritance ---

    [Fact]
    public void InheritFrom_ExplicitlySet_DoesNotOverride()
    {
        var parent = new ComputedStyle { Color = new Color(255, 0, 0, 255) };
        var child = new ComputedStyle { Color = new Color(0, 255, 0, 255) };
        var explicitlySet = new HashSet<string> { "color" };

        InheritableProperties.InheritFrom(child, parent, explicitlySet);

        // Child's color should remain green
        Assert.Equal(new Color(0, 255, 0, 255), child.Color);
    }

    [Fact]
    public void InheritFrom_PartialExplicitlySet_InheritsUnsetProperties()
    {
        var parent = new ComputedStyle
        {
            Color = new Color(255, 0, 0, 255),
            FontFamily = "Roboto",
            FontSize = 24f
        };
        var child = new ComputedStyle
        {
            FontFamily = "Arial"  // Explicitly set
        };
        var explicitlySet = new HashSet<string> { "font-family" };

        InheritableProperties.InheritFrom(child, parent, explicitlySet);

        // Color and FontSize should inherit
        Assert.Equal(new Color(255, 0, 0, 255), child.Color);
        Assert.Equal(24f, child.FontSize);
        // FontFamily should stay as child's value
        Assert.Equal("Arial", child.FontFamily);
    }

    [Fact]
    public void InheritFrom_NullExplicitlySet_InheritsAll()
    {
        var parent = new ComputedStyle
        {
            Color = new Color(100, 100, 100, 255),
            FontFamily = "Roboto",
            FontSize = 20f,
            FontWeight = 600
        };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent, null);

        Assert.Equal(parent.Color, child.Color);
        Assert.Equal(parent.FontFamily, child.FontFamily);
        Assert.Equal(parent.FontSize, child.FontSize);
        Assert.Equal(parent.FontWeight, child.FontWeight);
    }

    // --- Custom properties inheritance ---

    [Fact]
    public void InheritFrom_CustomProperties_AlwaysInherit()
    {
        var parent = new ComputedStyle();
        parent.CustomProperties["--primary"] = "#3B82F6";
        parent.CustomProperties["--spacing"] = "8px";

        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal("#3B82F6", child.CustomProperties["--primary"]);
        Assert.Equal("8px", child.CustomProperties["--spacing"]);
    }

    [Fact]
    public void InheritFrom_CustomProperties_ChildOverridesParent()
    {
        var parent = new ComputedStyle();
        parent.CustomProperties["--color"] = "red";

        var child = new ComputedStyle();
        child.CustomProperties["--color"] = "blue";

        InheritableProperties.InheritFrom(child, parent);

        // Child's value should be preserved
        Assert.Equal("blue", child.CustomProperties["--color"]);
    }

    [Fact]
    public void InheritFrom_CustomProperties_ChildDoesNotOverwriteExisting()
    {
        var parent = new ComputedStyle();
        parent.CustomProperties["--a"] = "parent-a";
        parent.CustomProperties["--b"] = "parent-b";

        var child = new ComputedStyle();
        child.CustomProperties["--a"] = "child-a";

        InheritableProperties.InheritFrom(child, parent);

        Assert.Equal("child-a", child.CustomProperties["--a"]);
        Assert.Equal("parent-b", child.CustomProperties["--b"]);
    }

    // --- Non-inheritable properties are NOT copied ---

    [Fact]
    public void InheritFrom_DoesNotCopyNonInheritable()
    {
        var parent = new ComputedStyle
        {
            BackgroundColor = new Color(255, 0, 0, 255),
            Width = 200f,
            Opacity = 0.5f,
            Padding = new EdgeValues(10)
        };
        var child = new ComputedStyle();

        InheritableProperties.InheritFrom(child, parent);

        // Non-inheritable properties should remain at defaults
        Assert.Equal(Color.Transparent, child.BackgroundColor);
        Assert.True(float.IsNaN(child.Width)); // default is NaN
        Assert.Equal(1f, child.Opacity); // default is 1
        Assert.Equal(default(EdgeValues), child.Padding);
    }
}
