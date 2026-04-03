using Lumi.Core;
using Lumi.Layout;
using Lumi.Rendering;
using Lumi.Styling;

// Parse command-line args
string htmlPath = args.Length > 0 ? args[0] : "MainWindow.html";
string cssPath = args.Length > 1 ? args[1] : "MainWindow.css";
string outputPath = args.Length > 2 ? args[2] : "screenshot.png";
int width = args.Length > 3 ? int.Parse(args[3]) : 960;
int height = args.Length > 4 ? int.Parse(args[4]) : 720;

Console.WriteLine($"Rendering {htmlPath} + {cssPath} → {outputPath} ({width}×{height})");

var root = HtmlTemplateParser.ParseFile(htmlPath);
var resolver = new StyleResolver();
resolver.AddStyleSheet(CssParser.ParseFile(cssPath));
resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

var layout = new YogaLayoutEngine();
layout.MeasureFunc = (element, availW, availH) =>
{
    if (element is TextElement te && !string.IsNullOrEmpty(te.Text))
        return TextLayout.Measure(te.Text, availW, te.ComputedStyle);
    return (0, 0);
};
layout.CalculateLayout(root, width, height);

bool saved = SkiaRenderer.RenderToPng(root, width, height, outputPath);
Console.WriteLine(saved ? $"Screenshot saved: {Path.GetFullPath(outputPath)}" : "Failed to save screenshot");
