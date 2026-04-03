using Lumi.Core;
using Lumi.Layout;
using Lumi.Rendering;
using Lumi.Styling;
using SkiaSharp;

namespace Lumi.Tests.Helpers;

/// <summary>
/// Runs the full Lumi pipeline headlessly (no SDL3/window) for integration testing.
/// HTML → Parse → Style → Layout → Render → Pixel access.
/// </summary>
public sealed class HeadlessPipeline : IDisposable
{
    public Element Root { get; private set; } = null!;
    public SkiaRenderer Renderer { get; } = new();
    public YogaLayoutEngine LayoutEngine { get; } = new();
    public StyleResolver StyleResolver { get; } = new();
    public int Width { get; }
    public int Height { get; }

    private SKBitmap? _readBitmap;

    public HeadlessPipeline(int width = 800, int height = 600)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Run the full pipeline: parse HTML + CSS, resolve styles, calculate layout, render to bitmap.
    /// </summary>
    public static HeadlessPipeline Render(string html, string css, int width = 800, int height = 600)
    {
        var pipeline = new HeadlessPipeline(width, height);
        pipeline.Load(html, css);
        pipeline.Execute();
        return pipeline;
    }

    /// <summary>
    /// Parse HTML and CSS without rendering (for style/layout-only tests).
    /// </summary>
    public static HeadlessPipeline StyleAndLayout(string html, string css, int width = 800, int height = 600)
    {
        var pipeline = new HeadlessPipeline(width, height);
        pipeline.Load(html, css);
        pipeline.ResolveAndLayout();
        return pipeline;
    }

    /// <summary>
    /// Load HTML and CSS strings.
    /// </summary>
    public void Load(string html, string css)
    {
        Root = HtmlTemplateParser.Parse(html);
        StyleResolver.AddStyleSheet(CssParser.Parse(css));
    }

    /// <summary>
    /// Run style resolution and layout only (no rendering).
    /// </summary>
    public void ResolveAndLayout()
    {
        StyleResolver.ResolveStyles(Root);

        // Wire up text measurement for auto-sizing
        LayoutEngine.MeasureFunc = MeasureElement;
        LayoutEngine.CalculateLayout(Root, Width, Height);
    }

    /// <summary>
    /// Run the full pipeline: style → layout → render.
    /// </summary>
    public void Execute()
    {
        ResolveAndLayout();

        Renderer.EnsureSize(Width, Height);
        Renderer.Paint(Root);
    }

    /// <summary>
    /// Re-run style resolution, layout, and rendering (after modifying elements/styles).
    /// </summary>
    public void Rerender()
    {
        StyleResolver.ResolveStyles(Root);
        LayoutEngine.CalculateLayout(Root, Width, Height);
        Renderer.EnsureSize(Width, Height);
        Renderer.Paint(Root);
        _readBitmap = null; // invalidate cached bitmap
    }

    /// <summary>
    /// Get the pixel color at a specific position on the rendered bitmap.
    /// </summary>
    public SKColor GetPixelAt(int x, int y)
    {
        var bitmap = GetBitmap();
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return SKColors.Transparent;
        return bitmap.GetPixel(x, y);
    }

    /// <summary>
    /// Check if a rectangular region contains any non-white pixels (i.e., something was rendered).
    /// </summary>
    public bool HasContentInRegion(int x, int y, int width, int height)
    {
        var bitmap = GetBitmap();
        for (int py = y; py < y + height && py < Height; py++)
        {
            for (int px = x; px < x + width && px < Width; px++)
            {
                var pixel = bitmap.GetPixel(px, py);
                if (pixel != SKColors.White)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if a pixel matches an expected color (with optional tolerance for anti-aliasing).
    /// </summary>
    public bool PixelMatches(int x, int y, SKColor expected, int tolerance = 5)
    {
        var actual = GetPixelAt(x, y);
        return Math.Abs(actual.Red - expected.Red) <= tolerance
            && Math.Abs(actual.Green - expected.Green) <= tolerance
            && Math.Abs(actual.Blue - expected.Blue) <= tolerance
            && Math.Abs(actual.Alpha - expected.Alpha) <= tolerance;
    }

    /// <summary>
    /// Find an element by ID in the tree.
    /// </summary>
    public Element? FindById(string id) => FindById(Root, id);

    /// <summary>
    /// Find all elements with a given class.
    /// </summary>
    public List<Element> FindByClass(string className)
    {
        var results = new List<Element>();
        FindByClass(Root, className, results);
        return results;
    }

    /// <summary>
    /// Get the LayoutBox of an element found by ID.
    /// </summary>
    public LayoutBox GetLayoutOf(string id)
    {
        var element = FindById(id);
        return element?.LayoutBox ?? LayoutBox.Empty;
    }

    private SKBitmap GetBitmap()
    {
        if (_readBitmap != null) return _readBitmap;

        var pixelPtr = Renderer.GetPixels();
        if (pixelPtr == IntPtr.Zero)
            throw new InvalidOperationException("No rendered bitmap available. Call Execute() first.");

        // Create a bitmap that shares the renderer's pixel data
        _readBitmap = new SKBitmap();
        _readBitmap.InstallPixels(new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul), pixelPtr);
        return _readBitmap;
    }

    private static (float Width, float Height) MeasureElement(Element element, float availableWidth, float availableHeight)
    {
        if (element is TextElement textElement && !string.IsNullOrEmpty(textElement.Text))
        {
            return TextLayout.Measure(textElement.Text, availableWidth, element.ComputedStyle);
        }

        if (element is ImageElement imageElement)
        {
            float natW = imageElement.NaturalWidth > 0 ? imageElement.NaturalWidth : 150;
            float natH = imageElement.NaturalHeight > 0 ? imageElement.NaturalHeight : 150;
            if (availableWidth < natW && availableWidth > 0 && availableWidth < float.MaxValue)
            {
                float scale = availableWidth / natW;
                natW = availableWidth;
                natH *= scale;
            }
            return (natW, natH);
        }

        return (0, 0);
    }

    private static Element? FindById(Element element, string id)
    {
        if (element.Id == id) return element;
        foreach (var child in element.Children)
        {
            var found = FindById(child, id);
            if (found != null) return found;
        }
        return null;
    }

    private static void FindByClass(Element element, string className, List<Element> results)
    {
        if (element.Classes.Contains(className)) results.Add(element);
        foreach (var child in element.Children) FindByClass(child, className, results);
    }

    public void Dispose()
    {
        _readBitmap?.Dispose();
        Renderer.Dispose();
        LayoutEngine.Dispose();
    }
}
