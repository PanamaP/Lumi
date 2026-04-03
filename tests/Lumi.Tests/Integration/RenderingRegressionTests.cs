using Lumi.Core;
using Lumi.Tests.Helpers;
using SkiaSharp;

namespace Lumi.Tests.Integration;

[Collection("Integration")]
public class RenderingRegressionTests
{
    [Fact]
    public void BackgroundColor_RendersRedBoxAtCenter()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""box""></div>",
            "#box { width: 100px; height: 100px; background-color: red; }",
            400, 300);

        var layout = p.GetLayoutOf("box");
        int cx = (int)(layout.X + layout.Width / 2);
        int cy = (int)(layout.Y + layout.Height / 2);

        Assert.True(p.PixelMatches(cx, cy, SKColors.Red, 5),
            $"Expected red at ({cx},{cy}), got {p.GetPixelAt(cx, cy)}");
    }

    [Fact]
    public void NestedElement_ChildRendersOverParent()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""parent""><div id=""child""></div></div>",
            "#parent { width: 200px; height: 200px; background-color: red; } " +
            "#child { width: 100px; height: 100px; background-color: blue; }",
            400, 300);

        var childLayout = p.GetLayoutOf("child");
        int cx = (int)(childLayout.X + childLayout.Width / 2);
        int cy = (int)(childLayout.Y + childLayout.Height / 2);

        Assert.True(p.PixelMatches(cx, cy, SKColors.Blue, 5),
            $"Expected blue (child) at ({cx},{cy}), got {p.GetPixelAt(cx, cy)}");

        // Parent area outside child should still be red
        var parentLayout = p.GetLayoutOf("parent");
        int px = (int)(parentLayout.X + parentLayout.Width - 10);
        int py = (int)(parentLayout.Y + parentLayout.Height - 10);
        Assert.True(p.PixelMatches(px, py, SKColors.Red, 5),
            $"Expected red (parent) at ({px},{py}), got {p.GetPixelAt(px, py)}");
    }

    [Fact]
    public void Border_RendersGreenBorderEdge()
    {
        // ExCSS decomposes border-color into per-edge properties not handled by
        // PropertyApplier, so we set BorderColor programmatically after style resolution.
        using var p = new HeadlessPipeline(400, 300);
        p.Load(@"<div id=""box""></div>",
            "#box { width: 100px; height: 100px; border-width: 4px; background-color: white; }");
        p.ResolveAndLayout();

        var el = p.FindById("box")!;
        el.ComputedStyle.BorderColor = new Color(0, 128, 0, 255);

        p.Renderer.EnsureSize(p.Width, p.Height);
        p.Renderer.Paint(p.Root);

        var layout = p.GetLayoutOf("box");
        // Sample a pixel 2px into the left border at vertical center
        int bx = (int)(layout.X + 2);
        int by = (int)(layout.Y + layout.Height / 2);

        Assert.True(p.PixelMatches(bx, by, new SKColor(0, 128, 0), 10),
            $"Expected greenish border at ({bx},{by}), got {p.GetPixelAt(bx, by)}");
    }

    [Fact]
    public void BorderRadius_CornersAreClipped()
    {
        // ExCSS decomposes border-radius into per-corner properties not handled by
        // PropertyApplier, so we set BorderRadius programmatically after style resolution.
        using var p = new HeadlessPipeline(400, 300);
        p.Load(@"<div id=""box""></div>",
            "#box { width: 100px; height: 100px; background-color: red; }");
        p.ResolveAndLayout();

        var el = p.FindById("box")!;
        el.ComputedStyle.BorderRadius = 50;

        p.Renderer.EnsureSize(p.Width, p.Height);
        p.Renderer.Paint(p.Root);

        var layout = p.GetLayoutOf("box");
        // Top-left corner pixel should be clipped to white
        int cornerX = (int)layout.X;
        int cornerY = (int)layout.Y;
        Assert.True(p.PixelMatches(cornerX, cornerY, SKColors.White, 5),
            $"Expected white (clipped corner) at ({cornerX},{cornerY}), got {p.GetPixelAt(cornerX, cornerY)}");

        // Center should still be red
        int cx = (int)(layout.X + layout.Width / 2);
        int cy = (int)(layout.Y + layout.Height / 2);
        Assert.True(p.PixelMatches(cx, cy, SKColors.Red, 5),
            $"Expected red at center ({cx},{cy}), got {p.GetPixelAt(cx, cy)}");
    }

    [Fact]
    public void OverflowHidden_ClipsChildOutsideParent()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""parent""><div id=""child""></div></div>",
            "#parent { width: 100px; height: 100px; overflow: hidden; } " +
            "#child { width: 200px; height: 200px; background-color: blue; }",
            400, 300);

        var parentLayout = p.GetLayoutOf("parent");

        // Inside parent: child's blue should be visible
        int insideX = (int)(parentLayout.X + 50);
        int insideY = (int)(parentLayout.Y + 50);
        Assert.True(p.PixelMatches(insideX, insideY, SKColors.Blue, 5),
            $"Expected blue inside parent at ({insideX},{insideY}), got {p.GetPixelAt(insideX, insideY)}");

        // Outside parent: should be clipped to white
        int outsideX = (int)(parentLayout.X + 150);
        int outsideY = (int)(parentLayout.Y + 150);
        Assert.True(p.PixelMatches(outsideX, outsideY, SKColors.White, 5),
            $"Expected white outside parent at ({outsideX},{outsideY}), got {p.GetPixelAt(outsideX, outsideY)}");
    }

    [Fact]
    public void Opacity_BlendsWithWhiteBackground()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""box""></div>",
            "#box { width: 100px; height: 100px; background-color: red; opacity: 0.5; }",
            400, 300);

        var layout = p.GetLayoutOf("box");
        int cx = (int)(layout.X + layout.Width / 2);
        int cy = (int)(layout.Y + layout.Height / 2);

        var pixel = p.GetPixelAt(cx, cy);

        // Red (255,0,0) at 50% opacity over white (255,255,255) ≈ (255,128,128)
        Assert.True(p.PixelMatches(cx, cy, new SKColor(255, 128, 128), 20),
            $"Expected blended pink at ({cx},{cy}), got {pixel}");

        // Must NOT be pure red — opacity should have blended it
        Assert.False(p.PixelMatches(cx, cy, SKColors.Red, 5),
            $"Pixel should not be pure red when opacity is 0.5, got {pixel}");
    }

    [Fact]
    public void Text_RendersNonWhitePixelsInTextArea()
    {
        using var p = HeadlessPipeline.Render(
            @"<span id=""text"">Hello</span>",
            "#text { font-size: 24px; color: black; }",
            400, 300);

        var layout = p.GetLayoutOf("text");

        Assert.True(
            p.HasContentInRegion(
                (int)layout.X, (int)layout.Y,
                (int)layout.Width, (int)layout.Height),
            "Text element should contain rendered (non-white) pixels");
    }

    [Fact]
    public void BoxShadow_RendersPixelsOutsideElementBounds()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""box""></div>",
            "#box { width: 100px; height: 100px; background-color: blue; " +
            "box-shadow: 8px 8px 4px 0px rgba(0,0,0,0.5); margin: 20px; }",
            400, 300);

        var layout = p.GetLayoutOf("box");

        // Shadow is offset 8px right and 8px down — look just past the bottom-right corner
        int shadowX = (int)(layout.X + layout.Width + 2);
        int shadowY = (int)(layout.Y + layout.Height + 2);

        Assert.True(p.HasContentInRegion(shadowX, shadowY, 12, 12),
            "Box shadow should produce non-white pixels outside the element's layout bounds");
    }

    [Fact]
    public void MultipleElements_SideBySide_EachHasCorrectColor()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""row""><div id=""left""></div><div id=""right""></div></div>",
            "#row { display: flex; flex-direction: row; } " +
            "#left { width: 100px; height: 100px; background-color: red; } " +
            "#right { width: 100px; height: 100px; background-color: blue; }",
            400, 300);

        var leftLayout = p.GetLayoutOf("left");
        var rightLayout = p.GetLayoutOf("right");

        int lx = (int)(leftLayout.X + leftLayout.Width / 2);
        int ly = (int)(leftLayout.Y + leftLayout.Height / 2);
        int rx = (int)(rightLayout.X + rightLayout.Width / 2);
        int ry = (int)(rightLayout.Y + rightLayout.Height / 2);

        Assert.True(p.PixelMatches(lx, ly, SKColors.Red, 5),
            $"Left box center should be red, got {p.GetPixelAt(lx, ly)}");
        Assert.True(p.PixelMatches(rx, ry, SKColors.Blue, 5),
            $"Right box center should be blue, got {p.GetPixelAt(rx, ry)}");
    }

    [Fact]
    public void VisibilityHidden_DoesNotRenderPixels()
    {
        using var p = HeadlessPipeline.Render(
            @"<div id=""box""></div>",
            "#box { width: 100px; height: 100px; background-color: red; visibility: hidden; }",
            400, 300);

        var layout = p.GetLayoutOf("box");
        int cx = (int)(layout.X + layout.Width / 2);
        int cy = (int)(layout.Y + layout.Height / 2);

        Assert.True(p.PixelMatches(cx, cy, SKColors.White, 5),
            $"Hidden element should not render — expected white at ({cx},{cy}), got {p.GetPixelAt(cx, cy)}");
    }
}
