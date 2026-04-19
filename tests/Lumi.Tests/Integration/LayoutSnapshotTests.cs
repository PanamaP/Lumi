using System.Globalization;
using System.Text;
using Lumi.Core;
using Lumi.Tests.Helpers;

namespace Lumi.Tests.Integration;

/// <summary>
/// Layout snapshot regression tests. Each test runs a small HTML/CSS fragment
/// through <see cref="HeadlessPipeline.StyleAndLayout"/> and compares the
/// serialized post-layout box tree against an embedded raw-string expected value.
/// Yoga-based layout itself is deterministic, so non-text geometry should be stable
/// across runs. Snapshots that include text measurement are stable when the test
/// environment uses consistent font registration/resolution and the same shaping
/// stack (for example, Skia/HarfBuzz versions and platform font availability);
/// tests in this file therefore avoid asserting exact text-measurement geometry.
/// </summary>
[Collection("Integration")]
public class LayoutSnapshotTests
{
    private static string Snapshot(Element root)
    {
        var sb = new StringBuilder();
        Walk(root, 0, sb);
        return sb.ToString().TrimEnd('\n', '\r');
    }

    private static int s_anonCounter;

    private static void Walk(Element el, int depth, StringBuilder sb)
    {
        if (depth == 0) s_anonCounter = 0;

        var typeName = el.GetType().Name;
        var tag = typeName.EndsWith("Element", StringComparison.Ordinal)
            ? typeName[..^"Element".Length]
            : typeName;
        tag = tag.ToLowerInvariant();

        var id = string.IsNullOrEmpty(el.Id) ? "anon" + s_anonCounter++ : el.Id;

        var classes = "";
        if (el.Classes.Count > 0)
        {
            var arr = new List<string>();
            foreach (var c in el.Classes) arr.Add(c);
            classes = " [" + string.Join(".", arr) + "]";
        }

        var b = el.LayoutBox;
        var x = (int)Math.Round(b.X, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(b.Y, MidpointRounding.AwayFromZero);
        var w = (int)Math.Round(b.Width, MidpointRounding.AwayFromZero);
        var h = (int)Math.Round(b.Height, MidpointRounding.AwayFromZero);

        sb.Append(new string(' ', depth * 2));
        sb.Append(tag).Append('#').Append(id).Append(classes);
        sb.Append(" @ ")
          .Append(x.ToString(CultureInfo.InvariantCulture)).Append(',')
          .Append(y.ToString(CultureInfo.InvariantCulture)).Append(' ')
          .Append(w.ToString(CultureInfo.InvariantCulture)).Append('x')
          .Append(h.ToString(CultureInfo.InvariantCulture));
        sb.Append('\n');

        foreach (var child in el.Children)
            Walk(child, depth + 1, sb);
    }

    private static void AssertSnapshot(string expected, string actual, string testName)
    {
        var e = expected.Trim().Replace("\r\n", "\n");
        var a = actual.Trim().Replace("\r\n", "\n");
        if (e != a)
        {
            // Write the actual snapshot to disk for easy bake-in on first run.
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "lumi-snapshot-actuals");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, testName + ".txt"), actual);
            }
            catch { /* best-effort */ }
        }
        Assert.Equal(e, a);
    }

    [Fact]
    public void FlexRow_EqualShare_DividesEvenly()
    {
        const string html = """
            <div id="row">
                <div id="c1"></div>
                <div id="c2"></div>
                <div id="c3"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; width: 300px; height: 100px; }
            #c1, #c2, #c3 { flex-grow: 1; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#row @ 0,0 300x100
                box#c1 @ 0,0 100x100
                box#c2 @ 100,0 100x100
                box#c3 @ 200,0 100x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(FlexRow_EqualShare_DividesEvenly));
    }

    [Fact]
    public void FlexRow_WithGap20_DistributesExactly()
    {
        const string html = """
            <div id="row">
                <div id="c1"></div>
                <div id="c2"></div>
                <div id="c3"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; gap: 20px; width: 310px; height: 100px; }
            #c1, #c2, #c3 { flex-grow: 1; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        // 310 - 2*20 gap = 270 / 3 = 90, so each item receives an exact width.
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#row @ 0,0 310x100
                box#c1 @ 0,0 90x100
                box#c2 @ 110,0 90x100
                box#c3 @ 220,0 90x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(FlexRow_WithGap20_DistributesExactly));
    }

    [Fact]
    public void FlexColumn_AlignItemsCenter_CentersChildren()
    {
        const string html = """
            <div id="col">
                <div id="c1"></div>
                <div id="c2"></div>
            </div>
            """;
        const string css = """
            #col { display: flex; flex-direction: column; align-items: center; width: 200px; height: 200px; }
            #c1 { width: 50px; height: 50px; }
            #c2 { width: 100px; height: 50px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#col @ 0,0 200x200
                box#c1 @ 75,0 50x50
                box#c2 @ 50,50 100x50
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(FlexColumn_AlignItemsCenter_CentersChildren));
    }

    [Fact]
    public void NestedRow_InColumn_LaysOutCorrectly()
    {
        const string html = """
            <div id="col">
                <div id="row">
                    <div id="g1"></div>
                    <div id="g2"></div>
                </div>
            </div>
            """;
        const string css = """
            #col { display: flex; flex-direction: column; width: 400px; height: 200px; }
            #row { display: flex; flex-direction: row; width: 400px; height: 100px; }
            #g1, #g2 { flex-grow: 1; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#col @ 0,0 400x200
                box#row @ 0,0 400x100
                  box#g1 @ 0,0 200x100
                  box#g2 @ 200,0 200x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(NestedRow_InColumn_LaysOutCorrectly));
    }

    [Fact]
    public void PercentageWidth_50_ResolvesAgainstParent()
    {
        const string html = """
            <div id="parent">
                <div id="child"></div>
            </div>
            """;
        const string css = """
            #parent { width: 400px; height: 100px; }
            #child { width: 50%; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#parent @ 0,0 400x100
                box#child @ 0,0 200x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(PercentageWidth_50_ResolvesAgainstParent));
    }

    [Fact]
    public void FlexBasis50_Grow1_DividesRemainder()
    {
        const string html = """
            <div id="row">
                <div id="c1"></div>
                <div id="c2"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; width: 300px; height: 100px; }
            #c1, #c2 { flex-basis: 50px; flex-grow: 1; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#row @ 0,0 300x100
                box#c1 @ 0,0 150x100
                box#c2 @ 150,0 150x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(FlexBasis50_Grow1_DividesRemainder));
    }

    [Fact]
    public void MarginPaddingCombo_OffsetsByMargin()
    {
        const string html = """<div id="box"></div>""";
        const string css = """
            #box { margin: 10px; padding: 20px; width: 100px; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        // box content area is 100x100; padding adds 20 inside; margin pushes by 10
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#box @ 10,10 100x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(MarginPaddingCombo_OffsetsByMargin));
    }

    [Fact]
    public void AbsolutePositionedCorner_AlignsToBottomRight()
    {
        const string html = """
            <div id="parent">
                <div id="child"></div>
            </div>
            """;
        const string css = """
            #parent { position: relative; width: 200px; height: 200px; }
            #child { position: absolute; right: 0px; bottom: 0px; width: 50px; height: 50px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#parent @ 0,0 200x200
                box#child @ 150,150 50x50
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(AbsolutePositionedCorner_AlignsToBottomRight));
    }

    [Fact]
    public void MultiChildWithText_PlacesBoxesAndProducesNonZeroTextSize()
    {
        const string html = """
            <div id="row">
                <div id="a"><span>A</span></div>
                <div id="b"><span>B</span></div>
                <div id="c"><span>C</span></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; width: 300px; height: 50px; }
            #a, #b, #c { width: 100px; height: 50px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);

        // Box geometry is fully determined by Yoga layout and is asserted exactly.
        // Text geometry is intentionally NOT snapshotted here because text
        // measurement depends on platform font availability and Skia/HarfBuzz
        // versions; instead we assert structural invariants on the text nodes.
        var row = p.FindById("row");
        Assert.NotNull(row);
        Assert.Equal(3, row!.Children.Count);

        var ids = new[] { "a", "b", "c" };
        for (int i = 0; i < ids.Length; i++)
        {
            var box = p.FindById(ids[i]);
            Assert.NotNull(box);
            Assert.Equal(i * 100f, box!.LayoutBox.X);
            Assert.Equal(0f, box.LayoutBox.Y);
            Assert.Equal(100f, box.LayoutBox.Width);
            Assert.Equal(50f, box.LayoutBox.Height);

            // Each box wraps a single text element with non-zero measured size.
            Assert.Single(box.Children);
            var text = box.Children[0];
            Assert.True(text.LayoutBox.Width > 0, $"text in #{ids[i]} should have non-zero width");
            Assert.True(text.LayoutBox.Height > 0, $"text in #{ids[i]} should have non-zero height");
        }
    }

    [Fact]
    public void DeepNesting_5Levels_AccumulatesPadding()
    {
        const string html = """
            <div id="l1">
                <div id="l2">
                    <div id="l3">
                        <div id="l4">
                            <div id="l5"></div>
                        </div>
                    </div>
                </div>
            </div>
            """;
        const string css = """
            #l1, #l2, #l3, #l4, #l5 { padding: 5px; width: 200px; height: 200px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#l1 @ 0,0 200x200
                box#l2 @ 5,5 200x190
                  box#l3 @ 10,10 200x180
                    box#l4 @ 15,15 200x170
                      box#l5 @ 20,20 200x160
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(DeepNesting_5Levels_AccumulatesPadding));
    }

    [Fact]
    public void WrapFlexRow_OverflowingChildren_WrapToNewLine()
    {
        const string html = """
            <div id="row">
                <div id="c1"></div>
                <div id="c2"></div>
                <div id="c3"></div>
                <div id="c4"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; flex-wrap: wrap; width: 300px; height: 200px; }
            #c1, #c2, #c3, #c4 { width: 200px; height: 50px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#row @ 0,0 300x200
                box#c1 @ 0,0 200x50
                box#c2 @ 0,50 200x50
                box#c3 @ 0,100 200x50
                box#c4 @ 0,150 200x50
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(WrapFlexRow_OverflowingChildren_WrapToNewLine));
    }

    [Fact]
    public void ZeroSize_NoContent_CollapsesHeight()
    {
        const string html = """<div id="empty"></div>""";
        const string css = "";
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#empty @ 0,0 800x0
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(ZeroSize_NoContent_CollapsesHeight));
    }
}
