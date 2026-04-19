using SkiaSharp;

namespace Lumi.Tests.Golden;

/// <summary>
/// Pixel-level regression tests using shape-only baselines (no text in v1 — fonts vary
/// across platforms). Run with LUMI_REGEN_GOLDENS=1 to (re)generate the .png baselines.
/// </summary>
[Trait("Category", "Golden")]
public class GoldenImageTests
{
    [Fact]
    public void SolidRedBox()
    {
        const string html = """<div id="box" class="b"></div>""";
        const string css = """
            html, body { background-color: white; }
            .b { width: 100px; height: 100px; background-color: red; }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        GoldenImageHelper.AssertGolden(bmp, "solid_red_box");
    }

    [Fact]
    public void RoundedCorners()
    {
        const string html = """<div id="box" class="b"></div>""";
        const string css = """
            html, body { background-color: white; }
            .b { width: 120px; height: 120px; background-color: blue; border-radius: 16px; }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        // Slightly higher tolerance to absorb anti-aliasing on the curve.
        GoldenImageHelper.AssertGolden(bmp, "rounded_corners",
            tolerancePerChannel: 4, maxDifferingPixelRatio: 0.01);
    }

    [Fact]
    public void Border1pxBlack()
    {
        const string html = """<div id="box" class="b"></div>""";
        const string css = """
            html, body { background-color: white; }
            .b { width: 100px; height: 100px; background-color: white;
                 border: 1px solid black; }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        GoldenImageHelper.AssertGolden(bmp, "border_1px_black");
    }

    [Fact(Skip = "Linear gradients are not yet supported by Lumi's CSS background pipeline.")]
    public void LinearGradientHorizontal()
    {
        const string html = """<div id="box" class="b"></div>""";
        const string css = """
            html, body { background-color: white; }
            .b { width: 200px; height: 60px;
                 background: linear-gradient(to right, red, blue); }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        GoldenImageHelper.AssertGolden(bmp, "linear_gradient_horizontal",
            tolerancePerChannel: 4, maxDifferingPixelRatio: 0.01);
    }

    [Fact]
    public void AlphaCompositing()
    {
        const string html = """
            <div id="root" class="root">
                <div id="r" class="r"></div>
                <div id="b" class="b"></div>
            </div>
            """;
        const string css = """
            html, body { background-color: white; }
            .root { position: relative; width: 200px; height: 200px; background-color: white; }
            .r { position: absolute; left: 30px; top: 30px; width: 100px; height: 100px;
                 background-color: rgba(255, 0, 0, 0.5); }
            .b { position: absolute; left: 70px; top: 70px; width: 100px; height: 100px;
                 background-color: rgba(0, 0, 255, 0.5); }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        GoldenImageHelper.AssertGolden(bmp, "alpha_compositing",
            tolerancePerChannel: 3, maxDifferingPixelRatio: 0.01);
    }

    [Fact]
    public void FlexRowThreeColors()
    {
        const string html = """
            <div id="row" class="row">
                <div class="c1"></div>
                <div class="c2"></div>
                <div class="c3"></div>
            </div>
            """;
        const string css = """
            html, body { background-color: white; }
            .row { display: flex; flex-direction: row; width: 300px; height: 60px; }
            .c1 { flex-grow: 1; background-color: red; }
            .c2 { flex-grow: 1; background-color: green; }
            .c3 { flex-grow: 1; background-color: blue; }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 300, 60);
        GoldenImageHelper.AssertGolden(bmp, "flex_row_three_colors");
    }

    [Fact]
    public void NestedPaddingBoxes()
    {
        const string html = """
            <div id="outer" class="outer">
                <div id="mid" class="mid">
                    <div id="inner" class="inner"></div>
                </div>
            </div>
            """;
        const string css = """
            html, body { background-color: white; }
            .outer { width: 180px; height: 180px; background-color: gray; padding: 20px; }
            .mid { width: 100px; height: 100px; background-color: white; padding: 20px; }
            .inner { width: 60px; height: 60px; background-color: red; }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        GoldenImageHelper.AssertGolden(bmp, "nested_padding_boxes");
    }

    [Fact]
    public void MarginCollapseOrOffset()
    {
        const string html = """<div id="box" class="b"></div>""";
        const string css = """
            html, body { background-color: white; }
            .b { width: 80px; height: 80px; background-color: red; margin: 30px; }
            """;
        using var bmp = GoldenImageHelper.RenderToBitmap(html, css, 200, 200);
        GoldenImageHelper.AssertGolden(bmp, "margin_collapse_or_offset");
    }
}
