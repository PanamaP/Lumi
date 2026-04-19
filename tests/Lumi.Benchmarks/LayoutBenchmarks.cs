using BenchmarkDotNet.Attributes;
using Lumi.Core;
using Lumi.Layout;

namespace Lumi.Benchmarks;

/// <summary>
/// Measures end-to-end Yoga layout of a moderately-sized flex tree (~1000 nodes).
/// </summary>
[MemoryDiagnoser]
public class LayoutBenchmarks
{
    private YogaLayoutEngine _engine = null!;
    private Element _root = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new YogaLayoutEngine();
        _root = BuildTree(nodeCount: 1000);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void CalculateLayout_FlexTree_1000Nodes()
    {
        _engine.CalculateLayout(_root, 1280, 800);
    }

    private static Element BuildTree(int nodeCount)
    {
        var root = new BoxElement("div");
        root.ComputedStyle.Display = DisplayMode.Flex;
        root.ComputedStyle.FlexDirection = FlexDirection.Column;

        int created = 1;
        const int columns = 10;
        const int rowsPerColumn = 10;
        const int cellsPerRow = 10;

        for (int c = 0; c < columns && created < nodeCount; c++)
        {
            var column = new BoxElement("div");
            column.ComputedStyle.Display = DisplayMode.Flex;
            column.ComputedStyle.FlexDirection = FlexDirection.Column;
            column.ComputedStyle.FlexGrow = 1;
            root.AddChild(column);
            created++;

            for (int r = 0; r < rowsPerColumn && created < nodeCount; r++)
            {
                var row = new BoxElement("div");
                row.ComputedStyle.Display = DisplayMode.Flex;
                row.ComputedStyle.FlexDirection = FlexDirection.Row;
                row.ComputedStyle.FlexGrow = 1;
                column.AddChild(row);
                created++;

                for (int i = 0; i < cellsPerRow && created < nodeCount; i++)
                {
                    var cell = new BoxElement("div");
                    cell.ComputedStyle.FlexGrow = 1;
                    cell.ComputedStyle.Padding = new EdgeValues(2, 2, 2, 2);
                    row.AddChild(cell);
                    created++;
                }
            }
        }

        return root;
    }
}
