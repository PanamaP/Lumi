# Lumi.Benchmarks

BenchmarkDotNet baseline for performance-sensitive areas of Lumi.

These benchmarks are **not run in CI by default** — BenchmarkDotNet results are
only meaningful on a quiet machine without thermal throttling, virtualization
noise, or background work. Run them locally when investigating a perf change.

## Running

Always use **Release** configuration; BenchmarkDotNet will refuse to run
otherwise.

```bash
# Run everything
dotnet run -c Release --project tests/Lumi.Benchmarks -- --filter * --runtimes net10.0

# Run a single category
dotnet run -c Release --project tests/Lumi.Benchmarks -- --filter *Layout*
dotnet run -c Release --project tests/Lumi.Benchmarks -- --filter *Render*
dotnet run -c Release --project tests/Lumi.Benchmarks -- --filter *HitTest*
dotnet run -c Release --project tests/Lumi.Benchmarks -- --filter *EventDispatch*
dotnet run -c Release --project tests/Lumi.Benchmarks -- --filter *CssParse*
```

## What's measured

| Benchmark                    | Scenario                                                       |
| ---------------------------- | -------------------------------------------------------------- |
| `LayoutBenchmarks`           | `YogaLayoutEngine.CalculateLayout` over a ~1000-node flex tree |
| `RenderBenchmarks`           | `SkiaRenderer.Paint` of a styled 50-element scene              |
| `HitTestBenchmarks`          | 1000 `HitTester.HitTest` calls against a 20-deep tree          |
| `EventDispatchBenchmarks`    | `EventDispatcher.Dispatch` of a Click bubbling 10 levels       |
| `CssParseBenchmarks`         | `CssParser.Parse` of a ~2000-rule stylesheet                   |

All benchmarks use `[MemoryDiagnoser]` so allocations are reported alongside time.

## Interpreting results

Treat the first run on a fresh checkout as the baseline. When investigating
a regression, run the affected benchmark on the suspect commit and on the
parent commit, and compare. Don't compare across machines.
