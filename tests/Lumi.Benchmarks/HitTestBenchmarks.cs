using BenchmarkDotNet.Attributes;
using Lumi.Core;
using Lumi.Input;
using Lumi.Layout;

namespace Lumi.Benchmarks;

/// <summary>
/// Measures HitTester.HitTest performance against a deep tree, performing
/// 1000 hit tests at varied coordinates per benchmark iteration.
/// </summary>
[MemoryDiagnoser]
public class HitTestBenchmarks
{
    private Element _root = null!;
    private (float X, float Y)[] _points = null!;
    private const int Width = 1280;
    private const int Height = 800;

    [GlobalSetup]
    public void Setup()
    {
        _root = BuildDeepTree(depth: 20, breadth: 4);

        using var engine = new YogaLayoutEngine();
        engine.CalculateLayout(_root, Width, Height);

        var rng = new Random(42);
        _points = new (float, float)[1000];
        for (int i = 0; i < _points.Length; i++)
            _points[i] = ((float)(rng.NextDouble() * Width), (float)(rng.NextDouble() * Height));
    }

    [Benchmark(OperationsPerInvoke = 1000)]
    public int HitTest_1000Points()
    {
        int hits = 0;
        for (int i = 0; i < _points.Length; i++)
        {
            var (x, y) = _points[i];
            if (HitTester.HitTest(_root, x, y) is not null) hits++;
        }
        return hits;
    }

    private static Element BuildDeepTree(int depth, int breadth)
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Flex;
        root.ComputedStyle.FlexDirection = FlexDirection.Column;
        Populate(root, depth, breadth);
        return root;
    }

    private static void Populate(Element parent, int depth, int breadth)
    {
        if (depth <= 0) return;
        for (int i = 0; i < breadth; i++)
        {
            var child = new BoxElement("div");
            child.ComputedStyle.FlexGrow = 1;
            child.ComputedStyle.Display = DisplayMode.Flex;
            child.ComputedStyle.FlexDirection = (depth % 2 == 0) ? FlexDirection.Row : FlexDirection.Column;
            parent.AddChild(child);
            if (i == 0)
                Populate(child, depth - 1, breadth);
        }
    }
}
