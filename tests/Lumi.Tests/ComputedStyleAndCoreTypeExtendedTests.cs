using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests;

public class ComputedStyleTests
{
    // --- Default values ---

    [Fact]
    public void Defaults_BoxModel()
    {
        var style = new ComputedStyle();

        Assert.True(float.IsNaN(style.Width));
        Assert.True(float.IsNaN(style.Height));
        Assert.Equal(0, style.MinWidth);
        Assert.Equal(float.PositiveInfinity, style.MaxWidth);
        Assert.Equal(0, style.MinHeight);
        Assert.Equal(float.PositiveInfinity, style.MaxHeight);
        Assert.Equal(default(EdgeValues), style.Margin);
        Assert.Equal(default(EdgeValues), style.Padding);
        Assert.Equal(BoxSizing.BorderBox, style.BoxSizing);
    }

    [Fact]
    public void Defaults_Layout()
    {
        var style = new ComputedStyle();

        Assert.Equal(DisplayMode.Block, style.Display);
        Assert.Equal(Position.Relative, style.Position);
        Assert.Equal(FlexDirection.Column, style.FlexDirection);
        Assert.Equal(FlexWrap.NoWrap, style.FlexWrap);
        Assert.Equal(JustifyContent.FlexStart, style.JustifyContent);
        Assert.Equal(AlignItems.Stretch, style.AlignItems);
        Assert.Equal(0f, style.FlexGrow);
        Assert.Equal(1f, style.FlexShrink);
        Assert.True(float.IsNaN(style.FlexBasis));
        Assert.Equal(0, style.ZIndex);
        Assert.Equal(Overflow.Visible, style.Overflow);
    }

    [Fact]
    public void Defaults_Visual()
    {
        var style = new ComputedStyle();

        Assert.Equal(Color.Transparent, style.BackgroundColor);
        Assert.Equal(Color.Transparent, style.BorderColor);
        Assert.Equal(0f, style.BorderRadius);
        Assert.Equal(1f, style.Opacity);
        Assert.Equal(Visibility.Visible, style.Visibility);
        Assert.Equal("default", style.Cursor);
        Assert.Null(style.BackgroundImage);
    }

    [Fact]
    public void Defaults_Text()
    {
        var style = new ComputedStyle();

        Assert.Equal(new Color(0, 0, 0, 255), style.Color);
        Assert.Equal("sans-serif", style.FontFamily);
        Assert.Equal(16f, style.FontSize);
        Assert.Equal(400, style.FontWeight);
        Assert.Equal(FontStyle.Normal, style.FontStyle);
        Assert.Equal(1.2f, style.LineHeight);
        Assert.Equal(TextAlign.Left, style.TextAlign);
        Assert.Equal(0f, style.LetterSpacing);
    }

    [Fact]
    public void Defaults_Transition()
    {
        var style = new ComputedStyle();

        Assert.Null(style.TransitionProperty);
        Assert.Equal(0f, style.TransitionDuration);
        Assert.Null(style.TransitionTimingFunction);
    }

    // --- Reset ---

    [Fact]
    public void Reset_RestoresBoxModelDefaults()
    {
        var style = new ComputedStyle
        {
            Width = 100, Height = 200,
            MinWidth = 50, MaxWidth = 300,
            Margin = new EdgeValues(10),
            Padding = new EdgeValues(20)
        };

        style.Reset();

        Assert.True(float.IsNaN(style.Width));
        Assert.True(float.IsNaN(style.Height));
        Assert.Equal(0, style.MinWidth);
        Assert.Equal(float.PositiveInfinity, style.MaxWidth);
        Assert.Equal(default(EdgeValues), style.Margin);
        Assert.Equal(default(EdgeValues), style.Padding);
    }

    [Fact]
    public void Reset_RestoresVisualDefaults()
    {
        var style = new ComputedStyle
        {
            BackgroundColor = new Color(255, 0, 0, 255),
            Opacity = 0.5f,
            BorderRadius = 8f,
            Cursor = "pointer",
            Visibility = Visibility.Hidden
        };

        style.Reset();

        Assert.Equal(Color.Transparent, style.BackgroundColor);
        Assert.Equal(1f, style.Opacity);
        Assert.Equal(0f, style.BorderRadius);
        Assert.Equal("default", style.Cursor);
        Assert.Equal(Visibility.Visible, style.Visibility);
    }

    [Fact]
    public void Reset_RestoresTextDefaults()
    {
        var style = new ComputedStyle
        {
            Color = new Color(100, 100, 100, 255),
            FontFamily = "Roboto",
            FontSize = 24f,
            FontWeight = 700,
            FontStyle = FontStyle.Italic,
            TextAlign = TextAlign.Center
        };

        style.Reset();

        Assert.Equal(new Color(0, 0, 0, 255), style.Color);
        Assert.Equal("sans-serif", style.FontFamily);
        Assert.Equal(16f, style.FontSize);
        Assert.Equal(400, style.FontWeight);
        Assert.Equal(FontStyle.Normal, style.FontStyle);
        Assert.Equal(TextAlign.Left, style.TextAlign);
    }

    [Fact]
    public void Reset_RestoresLayoutDefaults()
    {
        var style = new ComputedStyle
        {
            Display = DisplayMode.Flex,
            FlexDirection = FlexDirection.Row,
            FlexGrow = 1,
            Gap = 10,
            ZIndex = 5
        };

        style.Reset();

        Assert.Equal(DisplayMode.Block, style.Display);
        Assert.Equal(FlexDirection.Column, style.FlexDirection);
        Assert.Equal(0f, style.FlexGrow);
        Assert.Equal(0f, style.Gap);
        Assert.Equal(0, style.ZIndex);
    }

    [Fact]
    public void Reset_ClearsCustomProperties()
    {
        var style = new ComputedStyle();
        style.CustomProperties["--color"] = "red";
        style.CustomProperties["--size"] = "10px";

        style.Reset();

        Assert.False(style.HasCustomProperties);
    }

    [Fact]
    public void Reset_RestoresTransitionDefaults()
    {
        var style = new ComputedStyle
        {
            TransitionProperty = "opacity",
            TransitionDuration = 0.5f,
            TransitionTimingFunction = "ease"
        };

        style.Reset();

        Assert.Null(style.TransitionProperty);
        Assert.Equal(0f, style.TransitionDuration);
        Assert.Null(style.TransitionTimingFunction);
    }

    [Fact]
    public void Reset_RestoresAnimationDefaults()
    {
        var style = new ComputedStyle
        {
            AnimationName = "fadeIn",
            AnimationDuration = 1f,
            AnimationIterationCount = 3,
            AnimationDirection = AnimationDirection.Reverse
        };

        style.Reset();

        Assert.Null(style.AnimationName);
        Assert.Equal(0f, style.AnimationDuration);
        Assert.Equal(1, style.AnimationIterationCount);
        Assert.Equal(AnimationDirection.Normal, style.AnimationDirection);
    }

    [Fact]
    public void Reset_RestoresPointerEvents()
    {
        var style = new ComputedStyle { PointerEvents = false };

        style.Reset();

        Assert.True(style.PointerEvents);
    }

    // --- CustomProperties ---

    [Fact]
    public void CustomProperties_LazyInitialized()
    {
        var style = new ComputedStyle();
        Assert.False(style.HasCustomProperties);

        // Accessing CustomProperties forces initialization
        style.CustomProperties["--x"] = "1";
        Assert.True(style.HasCustomProperties);
    }

    [Fact]
    public void HasCustomProperties_FalseWhenEmpty()
    {
        var style = new ComputedStyle();
        // Access getter but don't add anything
        var _ = style.CustomProperties;
        style.CustomProperties.Clear();
        Assert.False(style.HasCustomProperties);
    }
}

public class CornerRadiusTests
{
    [Fact]
    public void Uniform_AllSame()
    {
        var cr = new CornerRadius(10);
        Assert.Equal(10, cr.TopLeft);
        Assert.Equal(10, cr.TopRight);
        Assert.Equal(10, cr.BottomRight);
        Assert.Equal(10, cr.BottomLeft);
        Assert.False(cr.HasPerCorner);
        Assert.Equal(10, cr.Uniform);
    }

    [Fact]
    public void PerCorner_DifferentValues()
    {
        var cr = new CornerRadius(1, 2, 3, 4);
        Assert.Equal(1, cr.TopLeft);
        Assert.Equal(2, cr.TopRight);
        Assert.Equal(3, cr.BottomRight);
        Assert.Equal(4, cr.BottomLeft);
        Assert.True(cr.HasPerCorner);
    }

    [Fact]
    public void HasPerCorner_False_WhenAllSame()
    {
        var cr = new CornerRadius(5, 5, 5, 5);
        Assert.False(cr.HasPerCorner);
    }

    [Fact]
    public void Default_AllZeros()
    {
        var cr = default(CornerRadius);
        Assert.Equal(0, cr.TopLeft);
        Assert.Equal(0, cr.TopRight);
        Assert.False(cr.HasPerCorner);
    }
}

public class BoxShadowTests
{
    [Fact]
    public void None_IsDefault()
    {
        var shadow = BoxShadow.None;
        Assert.True(shadow.IsNone);
    }

    [Fact]
    public void IsNone_True_WhenAllZeros()
    {
        var shadow = new BoxShadow(0, 0, 0, 0, Color.Transparent, false);
        Assert.True(shadow.IsNone);
    }

    [Fact]
    public void IsNone_False_WhenHasColor()
    {
        var shadow = new BoxShadow(0, 0, 0, 0, new Color(0, 0, 0, 128), false);
        Assert.False(shadow.IsNone);
    }

    [Fact]
    public void IsNone_False_WhenHasOffset()
    {
        var shadow = new BoxShadow(5, 5, 0, 0, Color.Transparent, false);
        Assert.False(shadow.IsNone);
    }

    [Fact]
    public void IsNone_False_WhenHasBlur()
    {
        var shadow = new BoxShadow(0, 0, 10, 0, Color.Transparent, false);
        Assert.False(shadow.IsNone);
    }

    [Fact]
    public void Constructor_SetsAllValues()
    {
        var shadow = new BoxShadow(2, 4, 6, 8, new Color(10, 20, 30, 40), true);

        Assert.Equal(2f, shadow.OffsetX);
        Assert.Equal(4f, shadow.OffsetY);
        Assert.Equal(6f, shadow.BlurRadius);
        Assert.Equal(8f, shadow.SpreadRadius);
        Assert.Equal(new Color(10, 20, 30, 40), shadow.Color);
        Assert.True(shadow.Inset);
    }
}

public class DirtyRegionTrackerTests
{
    [Fact]
    public void Initially_HasNoDirtyRegions()
    {
        var tracker = new DirtyRegionTracker();
        Assert.False(tracker.HasDirtyRegions);
        Assert.Empty(tracker.DirtyRects);
    }

    [Fact]
    public void Add_ValidRect_TracksDirtyRegion()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(10, 20, 100, 50));

        Assert.True(tracker.HasDirtyRegions);
        Assert.Single(tracker.DirtyRects);
    }

    [Fact]
    public void Add_ZeroWidthRect_Ignored()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(10, 20, 0, 50));

        Assert.False(tracker.HasDirtyRegions);
    }

    [Fact]
    public void Add_ZeroHeightRect_Ignored()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(10, 20, 100, 0));

        Assert.False(tracker.HasDirtyRegions);
    }

    [Fact]
    public void Add_NegativeSize_Ignored()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(10, 20, -10, 50));

        Assert.False(tracker.HasDirtyRegions);
    }

    [Fact]
    public void Clear_RemovesAllDirtyRegions()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(0, 0, 100, 100));
        tracker.Add(new LayoutBox(50, 50, 200, 200));

        tracker.Clear();

        Assert.False(tracker.HasDirtyRegions);
        Assert.Empty(tracker.DirtyRects);
    }

    [Fact]
    public void CoverageRatio_SingleFullSurfaceRect()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(0, 0, 800, 600));

        float ratio = tracker.CoverageRatio(800, 600);
        Assert.Equal(1.0f, ratio, 4);
    }

    [Fact]
    public void CoverageRatio_HalfSurface()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(0, 0, 400, 600));

        float ratio = tracker.CoverageRatio(800, 600);
        Assert.Equal(0.5f, ratio, 4);
    }

    [Fact]
    public void CoverageRatio_NoDirtyRegions_ReturnsZero()
    {
        var tracker = new DirtyRegionTracker();
        float ratio = tracker.CoverageRatio(800, 600);
        Assert.Equal(0f, ratio);
    }

    [Fact]
    public void CoverageRatio_ZeroSurface_ReturnsZero()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(0, 0, 100, 100));

        Assert.Equal(0f, tracker.CoverageRatio(0, 600));
        Assert.Equal(0f, tracker.CoverageRatio(800, 0));
    }

    [Fact]
    public void CoverageRatio_MultipleRects_SumsAreas()
    {
        var tracker = new DirtyRegionTracker();
        tracker.Add(new LayoutBox(0, 0, 100, 100));    // 10000
        tracker.Add(new LayoutBox(200, 200, 100, 100)); // 10000

        float ratio = tracker.CoverageRatio(1000, 1000); // total = 1000000
        Assert.Equal(0.02f, ratio, 4); // 20000 / 1000000
    }
}

public class LayoutBoxExtendedTests
{
    [Fact]
    public void Right_IsXPlusWidth()
    {
        var box = new LayoutBox(10, 20, 100, 50);
        Assert.Equal(110, box.Right);
    }

    [Fact]
    public void Bottom_IsYPlusHeight()
    {
        var box = new LayoutBox(10, 20, 100, 50);
        Assert.Equal(70, box.Bottom);
    }

    [Fact]
    public void Empty_IsAllZeros()
    {
        Assert.Equal(0, LayoutBox.Empty.X);
        Assert.Equal(0, LayoutBox.Empty.Y);
        Assert.Equal(0, LayoutBox.Empty.Width);
        Assert.Equal(0, LayoutBox.Empty.Height);
    }

    [Fact]
    public void Contains_InsidePoint_ReturnsTrue()
    {
        var box = new LayoutBox(0, 0, 100, 100);
        Assert.True(box.Contains(50, 50));
    }

    [Fact]
    public void Contains_OutsidePoint_ReturnsFalse()
    {
        var box = new LayoutBox(0, 0, 100, 100);
        Assert.False(box.Contains(150, 50));
        Assert.False(box.Contains(50, 150));
        Assert.False(box.Contains(-1, 50));
        Assert.False(box.Contains(50, -1));
    }

    [Fact]
    public void Contains_EdgePoints_ReturnsTrue()
    {
        var box = new LayoutBox(10, 10, 100, 100);
        Assert.True(box.Contains(10, 10));   // top-left
        Assert.True(box.Contains(110, 10));  // top-right
        Assert.True(box.Contains(10, 110));  // bottom-left
        Assert.True(box.Contains(110, 110)); // bottom-right
    }
}

public class ColorExtendedTests
{
    [Fact]
    public void Transparent_IsAllZeros()
    {
        Assert.Equal(0, Color.Transparent.R);
        Assert.Equal(0, Color.Transparent.G);
        Assert.Equal(0, Color.Transparent.B);
        Assert.Equal(0, Color.Transparent.A);
    }

    [Fact]
    public void Black_Is0_0_0_255()
    {
        Assert.Equal(0, Color.Black.R);
        Assert.Equal(0, Color.Black.G);
        Assert.Equal(0, Color.Black.B);
        Assert.Equal(255, Color.Black.A);
    }

    [Fact]
    public void White_Is255_255_255_255()
    {
        Assert.Equal(255, Color.White.R);
        Assert.Equal(255, Color.White.G);
        Assert.Equal(255, Color.White.B);
        Assert.Equal(255, Color.White.A);
    }

    [Fact]
    public void FromHex_InvalidLength_ReturnsBlack()
    {
        var color = Color.FromHex("12345"); // 5 chars, not valid
        Assert.Equal(Color.Black, color);
    }

    [Fact]
    public void FromHex_SingleChar_ReturnsBlack()
    {
        var color = Color.FromHex("F");
        Assert.Equal(Color.Black, color);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Color(10, 20, 30, 40);
        var b = new Color(10, 20, 30, 40);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Color(10, 20, 30, 40);
        var b = new Color(10, 20, 30, 41);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Constructor_SetsAllComponents()
    {
        var color = new Color(100, 150, 200, 128);
        Assert.Equal(100, color.R);
        Assert.Equal(150, color.G);
        Assert.Equal(200, color.B);
        Assert.Equal(128, color.A);
    }
}

public class EdgeValuesExtendedTests
{
    [Fact]
    public void Zero_IsAllZeros()
    {
        Assert.Equal(0, EdgeValues.Zero.Top);
        Assert.Equal(0, EdgeValues.Zero.Right);
        Assert.Equal(0, EdgeValues.Zero.Bottom);
        Assert.Equal(0, EdgeValues.Zero.Left);
    }

    [Fact]
    public void FourValues_SetsAll()
    {
        var edge = new EdgeValues(1, 2, 3, 4);
        Assert.Equal(1, edge.Top);
        Assert.Equal(2, edge.Right);
        Assert.Equal(3, edge.Bottom);
        Assert.Equal(4, edge.Left);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new EdgeValues(10, 20, 30, 40);
        var b = new EdgeValues(10, 20, 30, 40);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new EdgeValues(10, 20, 30, 40);
        var b = new EdgeValues(10, 20, 30, 41);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void WithSyntax_ModifiesSingleEdge()
    {
        var edge = new EdgeValues(10);
        var modified = edge with { Top = 20 };

        Assert.Equal(20, modified.Top);
        Assert.Equal(10, modified.Right);
        Assert.Equal(10, modified.Bottom);
        Assert.Equal(10, modified.Left);
    }
}
