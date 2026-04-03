using Lumi.Core;

namespace StressTest.Scenarios;

/// <summary>
/// Creates 1000+ nested div elements in a deep tree (10×10×10).
/// On each update, changes background color of 100 random elements.
/// Measures: layout calculation time, paint time.
/// </summary>
public class ElementFlood : IStressScenario
{
    public string Name => "Element Flood";
    public string Description => "1000 nested elements — stress layout + paint";

    private readonly List<Element> _leafElements = [];
    private readonly Random _rng = new(42);

    private static readonly Color[] Palette =
    [
        Color.FromHex("#EF4444"), Color.FromHex("#F97316"), Color.FromHex("#EAB308"),
        Color.FromHex("#22C55E"), Color.FromHex("#06B6D4"), Color.FromHex("#3B82F6"),
        Color.FromHex("#8B5CF6"), Color.FromHex("#EC4899"), Color.FromHex("#14B8A6"),
        Color.FromHex("#F43F5E")
    ];

    public void Setup(StressWindow window, Element container)
    {
        // 10 rows × 10 columns × 10 depth = 1000 leaf elements
        for (int row = 0; row < 10; row++)
        {
            var rowBox = new BoxElement("div");
            rowBox.ComputedStyle.Display = DisplayMode.Flex;
            rowBox.ComputedStyle.FlexDirection = FlexDirection.Row;
            rowBox.ComputedStyle.Margin = new EdgeValues(2, 0, 2, 0);

            for (int col = 0; col < 10; col++)
            {
                var colBox = new BoxElement("div");
                colBox.ComputedStyle.Display = DisplayMode.Flex;
                colBox.ComputedStyle.FlexDirection = FlexDirection.Column;
                colBox.ComputedStyle.FlexGrow = 1;
                colBox.ComputedStyle.Margin = new EdgeValues(0, 2, 0, 2);

                Element current = colBox;
                for (int depth = 0; depth < 10; depth++)
                {
                    var nested = new BoxElement("div");
                    nested.ComputedStyle.Padding = new EdgeValues(1, 1, 1, 1);
                    nested.ComputedStyle.BackgroundColor = Palette[(row + col + depth) % Palette.Length];
                    nested.ComputedStyle.BorderRadius = 2;

                    if (depth == 9)
                    {
                        var text = new TextElement($"{row}.{col}");
                        text.ComputedStyle.Color = Color.FromHex("#F8FAFC");
                        text.ComputedStyle.FontSize = 9;
                        nested.AddChild(text);
                        _leafElements.Add(nested);
                    }

                    current.AddChild(nested);
                    current = nested;
                }

                rowBox.AddChild(colBox);
            }

            container.AddChild(rowBox);
        }
    }

    public void Update(int frameNumber)
    {
        if (_leafElements.Count == 0) return;

        // Change 100 random leaf background colors per frame
        for (int i = 0; i < 100; i++)
        {
            var leaf = _leafElements[_rng.Next(_leafElements.Count)];
            leaf.ComputedStyle.BackgroundColor = Palette[_rng.Next(Palette.Length)];
            leaf.MarkDirty();
        }
    }
}
