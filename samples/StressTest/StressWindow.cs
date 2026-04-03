using Lumi;
using Lumi.Core;
using StressTest.Scenarios;

namespace StressTest;

public class StressWindow : Window
{
    private readonly string _scenario;
    private IStressScenario? _activeScenario;
    private TextElement? _metricsText;
    private TextElement? _scenarioName;
    private int _frameCount;
    private const int WarmupFrames = 60;
    private const int MeasureFrames = 300;

    // "all" mode state
    private static readonly string[] AllScenarios = ["elements", "text", "animation", "scroll", "binding", "styles"];
    private int _allIndex;
    private readonly List<ScenarioResult> _allResults = [];
    private bool _isAllMode;

    public StressWindow(string scenario)
    {
        _scenario = scenario.ToLowerInvariant();
        _isAllMode = _scenario == "all";

        Title = "Lumi — Stress Test";
        Width = 1024;
        Height = 768;

        var dir = AppContext.BaseDirectory;
        LoadTemplate(Path.Combine(dir, "StressWindow.html"));
        LoadStyleSheet(Path.Combine(dir, "StressWindow.css"));
    }

    public override void OnReady()
    {
        _metricsText = FindById("metrics") as TextElement;
        _scenarioName = FindById("scenario-name") as TextElement;

        if (_isAllMode)
        {
            StartScenario(AllScenarios[0]);
        }
        else
        {
            StartScenario(_scenario);
        }
    }

    public override void OnUpdate()
    {
        _frameCount++;
        _activeScenario?.Update(_frameCount);

        if (_frameCount > WarmupFrames && _frameCount % 30 == 0)
        {
            UpdateMetricsDisplay();
        }

        if (_frameCount == WarmupFrames + MeasureFrames)
        {
            if (_isAllMode)
            {
                RecordAndAdvance();
            }
            else
            {
                PrintResults();
            }
        }
    }

    private void StartScenario(string name)
    {
        var container = FindById("content");
        container?.ClearChildren();

        _frameCount = 0;
        _activeScenario = CreateScenario(name);

        if (_scenarioName != null)
        {
            _scenarioName.Text = _activeScenario?.Name ?? name;
            _scenarioName.MarkDirty();
        }

        if (_activeScenario != null && container != null)
        {
            Console.WriteLine($"\n▶ Starting: {_activeScenario.Name} — {_activeScenario.Description}");
            _activeScenario.Setup(this, container);
        }
        else
        {
            Console.WriteLine($"Unknown scenario: {name}");
            Console.WriteLine("Valid scenarios: elements, text, animation, scroll, binding, styles, all");
        }
    }

    private IStressScenario? CreateScenario(string name) => name switch
    {
        "elements" => new ElementFlood(),
        "text" => new TextStress(),
        "animation" => new AnimationStress(),
        "scroll" => new ScrollStress(),
        "binding" => new BindingStress(),
        "styles" => new StyleChurn(),
        _ => null
    };

    private void UpdateMetricsDisplay()
    {
        if (_metricsText == null) return;

        var m = FrameMetrics;
        _metricsText.Text = $"FPS: {m.AverageFps:F0} | Frame: {m.TotalFrameTimeMs:F1}ms | " +
                            $"Layout: {m.LayoutTimeMs:F1}ms | Paint: {m.PaintTimeMs:F1}ms | " +
                            $"Style: {m.StyleTimeMs:F1}ms";
        _metricsText.MarkDirty();
    }

    private void RecordAndAdvance()
    {
        var m = FrameMetrics;
        _allResults.Add(new ScenarioResult
        {
            Name = _activeScenario?.Name ?? AllScenarios[_allIndex],
            AvgFps = m.AverageFps,
            AvgFrame = m.TotalFrameTimeMs,
            Layout = m.LayoutTimeMs,
            Paint = m.PaintTimeMs
        });

        PrintResults();

        _allIndex++;
        if (_allIndex < AllScenarios.Length)
        {
            StartScenario(AllScenarios[_allIndex]);
        }
        else
        {
            PrintComparisonTable();
        }
    }

    private void PrintResults()
    {
        var m = FrameMetrics;
        Console.WriteLine($"\n--- Results ({MeasureFrames} frames) ---");
        Console.WriteLine($"  Avg FPS:    {m.AverageFps:F1}");
        Console.WriteLine($"  Avg Frame:  {m.TotalFrameTimeMs:F2}ms");
        Console.WriteLine($"  Layout:     {m.LayoutTimeMs:F2}ms");
        Console.WriteLine($"  Paint:      {m.PaintTimeMs:F2}ms");
        Console.WriteLine($"  Style:      {m.StyleTimeMs:F2}ms");
        Console.WriteLine($"  Update:     {m.UpdateTimeMs:F2}ms");
        Console.WriteLine($"  Present:    {m.PresentTimeMs:F2}ms");
    }

    private void PrintComparisonTable()
    {
        Console.WriteLine("\n" + new string('═', 65));
        Console.WriteLine("  ALL SCENARIOS — COMPARISON TABLE");
        Console.WriteLine(new string('═', 65));
        Console.WriteLine($"{"Scenario",-15}| {"Avg FPS",8} | {"Avg Frame",10} | {"Layout",7} | {"Paint",7}");
        Console.WriteLine($"{new string('─', 15)}┼{new string('─', 10)}┼{new string('─', 12)}┼{new string('─', 9)}┼{new string('─', 9)}");

        foreach (var r in _allResults)
        {
            Console.WriteLine($"{r.Name,-15}| {r.AvgFps,7:F1} | {r.AvgFrame,8:F1}ms | {r.Layout,5:F1}ms | {r.Paint,5:F1}ms");
        }

        Console.WriteLine(new string('═', 65));
    }

    private record ScenarioResult
    {
        public required string Name;
        public double AvgFps;
        public double AvgFrame;
        public double Layout;
        public double Paint;
    }
}
