namespace Lumi.Core;

/// <summary>
/// Registry for custom element types mapped to tag names.
/// </summary>
public static class ElementRegistry
{
    private static readonly Dictionary<string, Func<Element>> _registry = new(StringComparer.OrdinalIgnoreCase);

    static ElementRegistry()
    {
        // Built-in elements
        Register("div", () => new BoxElement("div"));
        Register("section", () => new BoxElement("section"));
        Register("nav", () => new BoxElement("nav"));
        Register("header", () => new BoxElement("header"));
        Register("footer", () => new BoxElement("footer"));
        Register("main", () => new BoxElement("main"));
        Register("aside", () => new BoxElement("aside"));
        Register("article", () => new BoxElement("article"));
        Register("ul", () => new BoxElement("ul"));
        Register("ol", () => new BoxElement("ol"));
        Register("li", () => new BoxElement("li"));
        Register("h1", () => new BoxElement("h1"));
        Register("h2", () => new BoxElement("h2"));
        Register("h3", () => new BoxElement("h3"));
        Register("h4", () => new BoxElement("h4"));
        Register("h5", () => new BoxElement("h5"));
        Register("h6", () => new BoxElement("h6"));
        Register("p", () => new BoxElement("p"));
        Register("a", () => new BoxElement("a"));
        Register("button", () => new BoxElement("button"));
        Register("span", () => new TextElement());
        Register("img", () => new ImageElement());
        Register("input", () => new InputElement());
    }

    public static void Register(string tagName, Func<Element> factory) =>
        _registry[tagName] = factory;

    public static void Register<T>(string tagName) where T : Element, new() =>
        _registry[tagName] = () => new T();

    public static Element Create(string tagName) =>
        _registry.TryGetValue(tagName, out var factory)
            ? factory()
            : new BoxElement(tagName);

    public static bool IsRegistered(string tagName) =>
        _registry.ContainsKey(tagName);
}
