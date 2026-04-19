using BenchmarkDotNet.Attributes;
using Lumi.Core;

namespace Lumi.Benchmarks;

/// <summary>
/// Measures EventDispatcher.Dispatch for a Click that bubbles through 10 ancestor levels.
/// </summary>
[MemoryDiagnoser]
public class EventDispatchBenchmarks
{
    private Element _target = null!;

    [GlobalSetup]
    public void Setup()
    {
        var root = new BoxElement("div");
        Element current = root;
        for (int i = 0; i < 10; i++)
        {
            var child = new BoxElement("div");
            current.AddChild(child);
            current = child;
            current.On("click", static (_, _) => { });
        }
        _target = current;
    }

    [Benchmark]
    public void Dispatch_Click_Bubbles10Levels()
    {
        var ev = new RoutedMouseEvent("click") { X = 0, Y = 0, Button = MouseButton.Left };
        EventDispatcher.Dispatch(ev, _target);
    }
}
