using Lumi.Core;
using Lumi.Layout;
using Lumi.Rendering;
using Lumi.Styling;

namespace Lumi;

/// <summary>
/// Base class for application windows. Loads an HTML template and CSS stylesheet,
/// and provides the element tree with automatic style and layout resolution.
/// </summary>
public class Window
{
    public string Title { get; set; } = "Lumi";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;

    /// <summary>
    /// Path to the HTML template file, if loaded from disk.
    /// </summary>
    public string? HtmlPath { get; set; }

    /// <summary>
    /// Path to the CSS stylesheet file, if loaded from disk.
    /// </summary>
    public string? CssPath { get; set; }

    /// <summary>
    /// Enable hot reload of HTML and CSS files during development.
    /// </summary>
    public bool EnableHotReload { get; set; } = false;

    public Element Root { get; internal set; } = new BoxElement("body");
    public StyleResolver StyleResolver { get; } = new();
    internal YogaLayoutEngine LayoutEngine { get; } = new();

    /// <summary>
    /// Per-frame timing metrics. Updated each frame by the application loop.
    /// </summary>
    public FrameMetrics FrameMetrics { get; internal set; } = new();

    /// <summary>
    /// Reference to the renderer, set by LumiApp during initialization.
    /// </summary>
    internal SkiaRenderer? Renderer { get; set; }

    /// <summary>
    /// Save a PNG screenshot of the current rendered frame.
    /// </summary>
    public bool SaveScreenshot(string filePath)
    {
        return Renderer?.ExportPng(filePath) ?? false;
    }

    /// <summary>
    /// Loads an HTML template file and builds the element tree.
    /// </summary>
    public void LoadTemplate(string path)
    {
        HtmlPath = Path.GetFullPath(path);
        Root = HtmlTemplateParser.ParseFile(path);
    }

    /// <summary>
    /// Loads an HTML string and builds the element tree.
    /// </summary>
    public void LoadTemplateString(string html)
    {
        Root = HtmlTemplateParser.Parse(html);
    }

    /// <summary>
    /// Loads a CSS stylesheet file.
    /// </summary>
    public void LoadStyleSheet(string path)
    {
        CssPath = Path.GetFullPath(path);
        StyleResolver.AddStyleSheet(CssParser.ParseFile(path));
    }

    /// <summary>
    /// Loads a CSS string as a stylesheet.
    /// </summary>
    public void LoadStyleSheetString(string css)
    {
        StyleResolver.AddStyleSheet(CssParser.Parse(css));
    }

    /// <summary>
    /// Called once after template and styles are loaded. Wire up events and logic here.
    /// </summary>
    public virtual void OnReady() { }

    /// <summary>
    /// Called after hot reload replaces the HTML template.
    /// Override to re-register event handlers on the new element tree.
    /// Default implementation calls OnReady().
    /// </summary>
    public virtual void OnHtmlReloaded() => OnReady();

    /// <summary>
    /// Called each frame before painting.
    /// </summary>
    public virtual void OnUpdate() { }

    /// <summary>
    /// Find an element by its ID.
    /// </summary>
    public Element? FindById(string id) => FindById(Root, id);

    /// <summary>
    /// Find all elements with the given CSS class.
    /// </summary>
    public List<Element> FindByClass(string className)
    {
        var results = new List<Element>();
        FindByClass(Root, className, results);
        return results;
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
        if (element.Classes.Contains(className))
            results.Add(element);
        foreach (var child in element.Children)
            FindByClass(child, className, results);
    }
}
