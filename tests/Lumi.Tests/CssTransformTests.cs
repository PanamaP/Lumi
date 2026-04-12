using Lumi.Core;
using Lumi.Styling;

namespace Lumi.Tests;

public class CssTransformTests
{
    private static ComputedStyle Apply(string property, string value)
    {
        var style = new ComputedStyle();
        PropertyApplier.SetFontSizeContext(16);
        PropertyApplier.SetViewportContext(1920, 1080);
        PropertyApplier.Apply(style, property, value);
        return style;
    }

    // ── Transform parsing ───────────────────────────────────────────

    [Fact]
    public void Transform_None()
    {
        var style = Apply("transform", "none");
        Assert.True(style.Transform.IsIdentity);
    }

    [Fact]
    public void Transform_Translate()
    {
        var style = Apply("transform", "translate(10px, 20px)");
        Assert.Equal(10, style.Transform.TranslateX);
        Assert.Equal(20, style.Transform.TranslateY);
    }

    [Fact]
    public void Transform_TranslateX()
    {
        var style = Apply("transform", "translateX(15px)");
        Assert.Equal(15, style.Transform.TranslateX);
        Assert.Equal(0, style.Transform.TranslateY);
    }

    [Fact]
    public void Transform_TranslateY()
    {
        var style = Apply("transform", "translateY(25px)");
        Assert.Equal(0, style.Transform.TranslateX);
        Assert.Equal(25, style.Transform.TranslateY);
    }

    [Fact]
    public void Transform_Scale_Uniform()
    {
        var style = Apply("transform", "scale(2)");
        Assert.Equal(2, style.Transform.ScaleX);
        Assert.Equal(2, style.Transform.ScaleY);
    }

    [Fact]
    public void Transform_Scale_XY()
    {
        var style = Apply("transform", "scale(1.5, 0.5)");
        Assert.Equal(1.5f, style.Transform.ScaleX);
        Assert.Equal(0.5f, style.Transform.ScaleY);
    }

    [Fact]
    public void Transform_ScaleX()
    {
        var style = Apply("transform", "scaleX(3)");
        Assert.Equal(3, style.Transform.ScaleX);
        Assert.Equal(1, style.Transform.ScaleY); // unchanged
    }

    [Fact]
    public void Transform_Rotate_Degrees()
    {
        var style = Apply("transform", "rotate(45deg)");
        Assert.Equal(45, style.Transform.Rotate);
    }

    [Fact]
    public void Transform_Rotate_Radians()
    {
        var style = Apply("transform", "rotate(3.14159rad)");
        Assert.Equal(180, style.Transform.Rotate, 1);
    }

    [Fact]
    public void Transform_Rotate_Turns()
    {
        var style = Apply("transform", "rotate(0.5turn)");
        Assert.Equal(180, style.Transform.Rotate);
    }

    [Fact]
    public void Transform_Skew()
    {
        var style = Apply("transform", "skew(10deg, 20deg)");
        Assert.Equal(10, style.Transform.SkewX);
        Assert.Equal(20, style.Transform.SkewY);
    }

    [Fact]
    public void Transform_Multiple()
    {
        var style = Apply("transform", "translate(10px, 5px) scale(2) rotate(90deg)");
        Assert.Equal(10, style.Transform.TranslateX);
        Assert.Equal(5, style.Transform.TranslateY);
        Assert.Equal(2, style.Transform.ScaleX);
        Assert.Equal(2, style.Transform.ScaleY);
        Assert.Equal(90, style.Transform.Rotate);
    }

    // ── Transform origin ────────────────────────────────────────────

    [Fact]
    public void TransformOrigin_Default()
    {
        var style = new ComputedStyle();
        Assert.Equal(50, style.TransformOriginX);
        Assert.Equal(50, style.TransformOriginY);
    }

    [Fact]
    public void TransformOrigin_Center()
    {
        var style = Apply("transform-origin", "center center");
        Assert.Equal(50, style.TransformOriginX);
        Assert.Equal(50, style.TransformOriginY);
    }

    [Fact]
    public void TransformOrigin_TopLeft()
    {
        var style = Apply("transform-origin", "left top");
        Assert.Equal(0, style.TransformOriginX);
        Assert.Equal(0, style.TransformOriginY);
    }

    [Fact]
    public void TransformOrigin_BottomRight()
    {
        var style = Apply("transform-origin", "right bottom");
        Assert.Equal(100, style.TransformOriginX);
        Assert.Equal(100, style.TransformOriginY);
    }

    [Fact]
    public void TransformOrigin_Percentage()
    {
        var style = Apply("transform-origin", "25% 75%");
        Assert.Equal(25, style.TransformOriginX);
        Assert.Equal(75, style.TransformOriginY);
    }

    // ── Identity check ──────────────────────────────────────────────

    [Fact]
    public void CssTransform_Identity_IsIdentity()
    {
        Assert.True(CssTransform.Identity.IsIdentity);
    }

    [Fact]
    public void CssTransform_NonIdentity()
    {
        var t = new CssTransform(1, 0, 1, 1, 0, 0, 0);
        Assert.False(t.IsIdentity);
    }

    // ── ParseTransform direct ───────────────────────────────────────

    [Fact]
    public void ParseTransform_EmptyStringReturnsDefaults()
    {
        var t = PropertyApplier.ParseTransform("initial");
        Assert.True(t.IsIdentity);
    }
}
