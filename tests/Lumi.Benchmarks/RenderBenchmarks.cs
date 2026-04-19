using System.Text;
using BenchmarkDotNet.Attributes;
using Lumi.Core;
using Lumi.Layout;
using Lumi.Rendering;
using Lumi.Styling;

namespace Lumi.Benchmarks;

/// <summary>
/// Measures a single SkiaRenderer.Paint pass against a styled, laid-out scene
/// of ~50 elements. Mirrors the integration-test HeadlessPipeline flow.
/// </summary>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    private SkiaRenderer _renderer = null!;
    private YogaLayoutEngine _layout = null!;
    private Element _root = null!;
    private const int Width = 800;
    private const int Height = 600;

    [GlobalSetup]
    public void Setup()
    {
        var html = new StringBuilder();
        html.Append("<div class=\"page\">");
        for (int i = 0; i < 50; i++)
        {
            html.Append($"<div class=\"card c{i % 5}\"><span>Card {i}</span></div>");
        }
        html.Append("</div>");

        const string css = """
            .page { display: flex; flex-direction: column; padding: 8px; background: #f8f8f8; }
            .card { padding: 6px; margin: 2px; background: #ffffff; border: 1px solid #cccccc; }
            .c0 { background: #ffeeee; }
            .c1 { background: #eeffee; }
            .c2 { background: #eeeeff; }
            .c3 { background: #ffffee; }
            .c4 { background: #ffeeff; }
            span { color: #333333; }
            """;

        _root = HtmlTemplateParser.Parse(html.ToString());
        var resolver = new StyleResolver();
        resolver.AddStyleSheet(CssParser.Parse(css));
        resolver.ResolveStyles(_root);

        _layout = new YogaLayoutEngine();
        _layout.CalculateLayout(_root, Width, Height);

        _renderer = new SkiaRenderer();
        _renderer.EnsureSize(Width, Height);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _renderer.Dispose();
        _layout.Dispose();
    }

    [Benchmark]
    public void Paint_50ElementScene()
    {
        _renderer.Paint(_root);
    }
}
