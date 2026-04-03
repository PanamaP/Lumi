using Lumi.Core;

namespace StressTest.Scenarios;

/// <summary>
/// Rapid CSS class toggling on 200 elements.
/// On each update, toggles 50 random elements between two style states.
/// Measures: style recalculation + layout invalidation time.
/// </summary>
public class StyleChurn : IStressScenario
{
    public string Name => "Style Churn";
    public string Description => "200 elements, 50 style toggles/frame — stress style recalc + layout";

    private readonly List<Element> _boxes = [];
    private readonly bool[] _stateA;
    private readonly Random _rng = new(42);

    public StyleChurn()
    {
        _stateA = new bool[200];
    }

    public void Setup(StressWindow window, Element container)
    {
        var grid = new BoxElement("div");
        grid.ComputedStyle.Display = DisplayMode.Flex;
        grid.ComputedStyle.FlexDirection = FlexDirection.Row;
        grid.ComputedStyle.FlexWrap = FlexWrap.Wrap;
        grid.ComputedStyle.Padding = new EdgeValues(8, 8, 8, 8);

        for (int i = 0; i < 200; i++)
        {
            var box = new BoxElement("div");
            _stateA[i] = i % 2 == 0;
            ApplyStyle(box, _stateA[i]);

            var text = new TextElement($"#{i}");
            text.ComputedStyle.Color = Color.FromHex("#F8FAFC");
            text.ComputedStyle.FontSize = 11;
            text.ComputedStyle.TextAlign = TextAlign.Center;

            box.AddChild(text);
            grid.AddChild(box);
            _boxes.Add(box);
        }

        container.AddChild(grid);
    }

    public void Update(int frameNumber)
    {
        // Toggle 50 random elements per frame
        for (int i = 0; i < 50; i++)
        {
            int index = _rng.Next(_boxes.Count);
            _stateA[index] = !_stateA[index];
            ApplyStyle(_boxes[index], _stateA[index]);
            _boxes[index].MarkDirty();
        }
    }

    private static void ApplyStyle(Element box, bool isStateA)
    {
        if (isStateA)
        {
            // State A: small, blue, tight margins
            box.ComputedStyle.Width = 40;
            box.ComputedStyle.Height = 40;
            box.ComputedStyle.Margin = new EdgeValues(4, 4, 4, 4);
            box.ComputedStyle.BackgroundColor = Color.FromHex("#3B82F6");
            box.ComputedStyle.BorderRadius = 4;
            box.ComputedStyle.Opacity = 1.0f;
        }
        else
        {
            // State B: larger, orange, wider margins
            box.ComputedStyle.Width = 56;
            box.ComputedStyle.Height = 56;
            box.ComputedStyle.Margin = new EdgeValues(8, 8, 8, 8);
            box.ComputedStyle.BackgroundColor = Color.FromHex("#F97316");
            box.ComputedStyle.BorderRadius = 8;
            box.ComputedStyle.Opacity = 0.8f;
        }
    }
}
