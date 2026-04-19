using System.Globalization;
using System.Text;
using Lumi.Core;
using Lumi.Tests.Helpers;

namespace Lumi.Tests;

/// <summary>
/// Deterministic layout snapshot regression tests. Each test runs a small HTML/CSS
/// fragment through <see cref="HeadlessPipeline.StyleAndLayout"/> and compares the
/// serialized post-layout box tree against an embedded raw-string expected value.
/// Yoga-based layout is fully deterministic so these snapshots are stable across runs.
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
    public void flex_row_equal_share()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(flex_row_equal_share));
    }

    [Fact]
    public void flex_row_with_gap_20()
    {
        const string html = """
            <div id="row">
                <div id="c1"></div>
                <div id="c2"></div>
                <div id="c3"></div>
            </div>
            """;
        const string css = """
            #row { display: flex; flex-direction: row; gap: 20px; width: 300px; height: 100px; }
            #c1, #c2, #c3 { flex-grow: 1; height: 100px; }
            """;
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        // 300 - 2*20 gap = 260 / 3 ~= 86.67 → rounding may vary
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#row @ 0,0 300x100
                box#c1 @ 0,0 87x100
                box#c2 @ 107,0 86x100
                box#c3 @ 213,0 87x100
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(flex_row_with_gap_20));
    }

    [Fact]
    public void flex_column_align_items_center()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(flex_column_align_items_center));
    }

    [Fact]
    public void nested_row_in_column()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(nested_row_in_column));
    }

    [Fact]
    public void percentage_width_50()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(percentage_width_50));
    }

    [Fact]
    public void flex_basis_50_grow_1()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(flex_basis_50_grow_1));
    }

    [Fact]
    public void margin_padding_combo()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(margin_padding_combo));
    }

    [Fact]
    public void absolute_positioned_corner()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(absolute_positioned_corner));
    }

    [Fact]
    public void multi_child_with_text()
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
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#row @ 0,0 300x50
                box#a @ 0,0 100x50
                  text#anon1 @ 0,0 100x26
                box#b @ 100,0 100x50
                  text#anon2 @ 100,0 100x26
                box#c @ 200,0 100x50
                  text#anon3 @ 200,0 100x26
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(multi_child_with_text));
    }

    [Fact]
    public void deep_nesting_5_levels()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(deep_nesting_5_levels));
    }

    [Fact]
    public void wrap_flex_row()
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
        AssertSnapshot(expected, Snapshot(p.Root), nameof(wrap_flex_row));
    }

    [Fact]
    public void zero_size_no_content()
    {
        const string html = """<div id="empty"></div>""";
        const string css = "";
        using var p = HeadlessPipeline.StyleAndLayout(html, css, 800, 600);
        const string expected = """
            box#anon0 @ 0,0 800x600
              box#empty @ 0,0 800x0
            """;
        AssertSnapshot(expected, Snapshot(p.Root), nameof(zero_size_no_content));
    }
}
