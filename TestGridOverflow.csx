using Lumi.Core;
using Lumi.Layout;

var root = new BoxElement("div");
root.ComputedStyle.Display = DisplayMode.Grid;
root.ComputedStyle.GridTemplateColumns = "200px 200px";  // 400px needed
root.ComputedStyle.GridGap = 20;  // +20 gap = 420px total

for (int i = 0; i < 4; i++)
    root.AddChild(new BoxElement("div"));

using var engine = new YogaLayoutEngine();
engine.CalculateLayout(root, 300, 400);  // Only 300px available!

Console.WriteLine($"Child 0: X={root.Children[0].LayoutBox.X}, Width={root.Children[0].LayoutBox.Width}");
Console.WriteLine($"Child 1: X={root.Children[1].LayoutBox.X}, Width={root.Children[1].LayoutBox.Width}");
Console.WriteLine($"Child 2: X={root.Children[2].LayoutBox.X}, Width={root.Children[2].LayoutBox.Width}");
Console.WriteLine($"Child 3: X={root.Children[3].LayoutBox.X}, Width={root.Children[3].LayoutBox.Width}");
Console.WriteLine("Test complete");
