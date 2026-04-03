using Lumi.Core;

namespace StressTest.Scenarios;

/// <summary>
/// Creates 100 paragraphs of wrapped lorem ipsum text.
/// On each update, changes 5 random paragraph texts.
/// Measures: text measurement + layout time.
/// </summary>
public class TextStress : IStressScenario
{
    public string Name => "Text Stress";
    public string Description => "100 paragraphs of wrapped text — stress text measurement";

    private readonly List<TextElement> _paragraphs = [];
    private readonly Random _rng = new(42);

    private static readonly string[] LoremParts =
    [
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
        "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
        "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
        "Curabitur pretium tincidunt lacus. Nulla gravida orci a odio. Nullam varius, turpis et commodo pharetra.",
        "Praesent dapibus, neque id cursus faucibus, tortor neque egestas augue, eu vulputate magna eros eu erat.",
        "Aliquam erat volutpat. Nam dui mi, tincidunt quis, accumsan porttitor, facilisis luctus, metus phasellus.",
        "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas vestibulum.",
        "Fusce vulputate eleifend sapien. Vestibulum purus quam, scelerisque ut, mollis sed, nonummy id, metus.",
        "Morbi lectus risus, iaculis vel, suscipit quis, luctus non, massa. Fusce ac turpis quis ligula lacinia."
    ];

    private static readonly float[] FontSizes = [12, 13, 14, 15, 16, 18, 20, 22, 24];

    public void Setup(StressWindow window, Element container)
    {
        for (int i = 0; i < 100; i++)
        {
            var wrapper = new BoxElement("div");
            wrapper.ComputedStyle.Margin = new EdgeValues(4, 0, 4, 0);
            wrapper.ComputedStyle.Padding = new EdgeValues(8, 12, 8, 12);
            wrapper.ComputedStyle.BackgroundColor = Color.FromHex("#1E293B");
            wrapper.ComputedStyle.BorderRadius = 4;

            var text = new TextElement(GenerateParagraph());
            text.ComputedStyle.Color = Color.FromHex("#CBD5E1");
            text.ComputedStyle.FontSize = FontSizes[i % FontSizes.Length];
            text.ComputedStyle.LineHeight = 1.5f;
            text.ComputedStyle.WhiteSpace = WhiteSpace.Normal;
            text.ComputedStyle.WordBreak = WordBreak.Normal;

            wrapper.AddChild(text);
            container.AddChild(wrapper);
            _paragraphs.Add(text);
        }
    }

    public void Update(int frameNumber)
    {
        // Change 5 random paragraph texts per frame
        for (int i = 0; i < 5; i++)
        {
            var para = _paragraphs[_rng.Next(_paragraphs.Count)];
            para.Text = GenerateParagraph();
            para.MarkDirty();
        }
    }

    private string GenerateParagraph()
    {
        // Combine 2-3 lorem fragments for 200+ chars
        int count = _rng.Next(2, 4);
        var parts = new string[count];
        for (int i = 0; i < count; i++)
        {
            parts[i] = LoremParts[_rng.Next(LoremParts.Length)];
        }
        return string.Join(" ", parts);
    }
}
