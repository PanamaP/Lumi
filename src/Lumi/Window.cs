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

    private Element _root = new BoxElement("body");
    private readonly ElementIndex _index = new();
    private bool _indexAttached;

    /// <summary>
    /// Theme manager for light/dark mode switching.
    /// Theme CSS variables are applied to the root element and cascade to all descendants.
    /// </summary>
    public ThemeManager Theme { get; } = new();

    public Element Root
    {
        get => _root;
        internal set
        {
            _root = value;
            _index.AttachTo(_root);
            _indexAttached = true;
        }
    }

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
    /// Window manager for opening and managing secondary windows.
    /// Set by LumiApp during initialization.
    /// </summary>
    public WindowManager? Windows { get; internal set; }

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
    /// Register a custom font file under the given family name.
    /// The family name can then be used in CSS font-family declarations.
    /// </summary>
    public void RegisterFont(string familyName, string filePath)
    {
        FontManager.RegisterFont(familyName, filePath);
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
    /// Find an element by its ID. O(1) indexed lookup.
    /// </summary>
    public Element? FindById(string id)
    {
        EnsureIndexAttached();
        return _index.FindById(id);
    }

    /// <summary>
    /// Find all elements with the given CSS class. O(1) indexed lookup.
    /// </summary>
    public List<Element> FindByClass(string className)
    {
        EnsureIndexAttached();
        return _index.FindByClass(className);
    }

    private void EnsureIndexAttached()
    {
        if (!_indexAttached)
        {
            _index.AttachTo(_root);
            _indexAttached = true;
        }
    }
}
