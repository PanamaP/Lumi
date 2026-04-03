using Lumi.Core;

namespace StressTest.Scenarios;

/// <summary>
/// Creates a scrollable container with 500 styled items.
/// Auto-scrolls by 2px per frame to simulate user scrolling.
/// Measures: scroll paint performance (clipping + many elements).
/// </summary>
public class ScrollStress : IStressScenario
{
    public string Name => "Scroll Stress";
    public string Description => "500 items in overflow:scroll — stress scroll + clip painting";

    private Element? _scrollContainer;

    private static readonly Color[] AccentColors =
    [
        Color.FromHex("#EF4444"), Color.FromHex("#F97316"), Color.FromHex("#EAB308"),
        Color.FromHex("#22C55E"), Color.FromHex("#06B6D4"), Color.FromHex("#3B82F6"),
        Color.FromHex("#8B5CF6"), Color.FromHex("#EC4899")
    ];

    public void Setup(StressWindow window, Element container)
    {
        _scrollContainer = new BoxElement("div");
        _scrollContainer.ComputedStyle.Overflow = Overflow.Scroll;
        _scrollContainer.ComputedStyle.Height = 400;
        _scrollContainer.ComputedStyle.FlexGrow = 1;
        _scrollContainer.ComputedStyle.Display = DisplayMode.Flex;
        _scrollContainer.ComputedStyle.FlexDirection = FlexDirection.Column;

        for (int i = 0; i < 500; i++)
        {
            var row = new BoxElement("div");
            row.ComputedStyle.Display = DisplayMode.Flex;
            row.ComputedStyle.FlexDirection = FlexDirection.Row;
            row.ComputedStyle.AlignItems = AlignItems.Center;
            row.ComputedStyle.Padding = new EdgeValues(8, 12, 8, 12);
            row.ComputedStyle.Margin = new EdgeValues(2, 0, 2, 0);
            row.ComputedStyle.BackgroundColor = Color.FromHex("#1E293B");
            row.ComputedStyle.BorderRadius = 4;

            // Colored accent bar
            var accent = new BoxElement("div");
            accent.ComputedStyle.Width = 4;
            accent.ComputedStyle.Height = 24;
            accent.ComputedStyle.BorderRadius = 2;
            accent.ComputedStyle.BackgroundColor = AccentColors[i % AccentColors.Length];
            accent.ComputedStyle.Margin = new EdgeValues(0, 12, 0, 0);

            var text = new TextElement($"Item #{i + 1} — Scrollable list stress test row with enough text to measure layout");
            text.ComputedStyle.Color = Color.FromHex("#CBD5E1");
            text.ComputedStyle.FontSize = 13;

            row.AddChild(accent);
            row.AddChild(text);
            _scrollContainer.AddChild(row);
        }

        container.AddChild(_scrollContainer);
    }

    public void Update(int frameNumber)
    {
        // Auto-scroll by 2px each frame to simulate continuous scrolling
        _scrollContainer?.ScrollBy(0, 2);
    }
}
