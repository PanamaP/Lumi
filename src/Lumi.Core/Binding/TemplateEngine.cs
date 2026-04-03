using System.Collections;
using System.Text.RegularExpressions;

namespace Lumi.Core.Binding;

/// <summary>
/// Walks an element tree and activates template directives (&lt;template for=""&gt;
/// and &lt;template if=""&gt;) by resolving their bindings against a data context.
/// </summary>
public static class TemplateEngine
{
    /// <summary>
    /// Parser function used to convert HTML strings into element trees.
    /// Must be set before calling Apply(). The umbrella Lumi library sets this
    /// to HtmlTemplateParser.Parse during initialization.
    /// </summary>
    public static Func<string, Element>? HtmlParser { get; set; }

    /// <summary>
    /// Apply template directives throughout the element tree using the given data context.
    /// Sets the DataContext on the root and resolves all template-for and template-if elements.
    /// </summary>
    public static void Apply(Element root, object dataContext)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(dataContext);

        if (HtmlParser == null)
            throw new InvalidOperationException(
                "TemplateEngine.HtmlParser must be set before calling Apply(). " +
                "This is normally done by LumiApp or by setting it to HtmlTemplateParser.Parse.");

        root.DataContext = dataContext;
        ResolveDirectives(root, dataContext);
    }

    private static void ResolveDirectives(Element element, object effectiveContext)
    {
        // Snapshot children since template activation may modify the list
        var children = element.Children.ToList();

        foreach (var child in children)
        {
            var childContext = child.DataContext ?? effectiveContext;

            switch (child)
            {
                case TemplateForElement templateFor:
                    ActivateFor(templateFor, childContext);
                    break;

                case TemplateIfElement templateIf:
                    ActivateIf(templateIf, childContext);
                    break;

                default:
                    ResolveDirectives(child, childContext);
                    break;
            }
        }
    }

    private static void ActivateFor(TemplateForElement templateFor, object context)
    {
        var collection = BindingEngine.ResolvePath(context, templateFor.CollectionPath);
        if (collection is not IEnumerable enumerable) return;

        templateFor.ClearChildren();

        Element CreateInstance(object item)
        {
            var parsed = HtmlParser!(templateFor.TemplateHtml);
            InterpolateTree(parsed, templateFor.ItemAlias, item);
            parsed.DataContext = item;

            // Wrap: the parsed root is a <body> wrapper; transplant its children
            // into a transparent container
            var container = new BoxElement("template-item");
            foreach (var child in parsed.Children.ToList())
            {
                child.Parent = null;
                container.AddChild(child);
            }
            container.DataContext = item;

            // Recursively resolve any nested directives
            ResolveDirectives(container, item);

            return container;
        }

        foreach (var item in enumerable)
        {
            var instance = CreateInstance(item);
            templateFor.AddChild(instance);
        }

        templateFor.BindCollection(enumerable, CreateInstance);
    }

    private static void ActivateIf(TemplateIfElement templateIf, object context)
    {
        var value = BindingEngine.ResolvePath(context, templateIf.ConditionPath);
        bool condition = TemplateIfElement.IsTruthy(value);

        Element CreateContent()
        {
            var parsed = HtmlParser!(templateIf.TemplateHtml);
            ResolveDirectives(parsed, context);
            return parsed;
        }

        templateIf.BindCondition(context, CreateContent);
        templateIf.SetRendered(condition);
    }

    /// <summary>
    /// Walk an element tree and replace {alias.PropertyPath} patterns in text content.
    /// </summary>
    internal static void InterpolateTree(Element root, string alias, object item)
    {
        InterpolateElement(root, alias, item);

        foreach (var child in root.Children)
        {
            InterpolateTree(child, alias, item);
        }
    }

    private static void InterpolateElement(Element element, string alias, object item)
    {
        if (element is TextElement textEl && !string.IsNullOrEmpty(textEl.Text))
        {
            textEl.Text = InterpolateText(textEl.Text, alias, item);
        }

        // Also interpolate relevant attributes
        var keysToUpdate = new List<(string key, string newValue)>();
        foreach (var kvp in element.Attributes)
        {
            if (kvp.Value.Contains('{'))
            {
                var interpolated = InterpolateText(kvp.Value, alias, item);
                if (interpolated != kvp.Value)
                    keysToUpdate.Add((kvp.Key, interpolated));
            }
        }
        foreach (var (key, newValue) in keysToUpdate)
        {
            element.Attributes[key] = newValue;
        }
    }

    /// <summary>
    /// Replace {alias.PropertyPath} patterns in a text string with resolved values.
    /// Supports mixed text like "Hello, {item.Name}! Count: {item.Count}".
    /// </summary>
    public static string InterpolateText(string text, string alias, object item)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{'))
            return text;

        // Match {alias.PropertyPath} or {alias} patterns
        var pattern = $@"\{{{Regex.Escape(alias)}(?:\.([^}}]+))?\}}";
        return Regex.Replace(text, pattern, match =>
        {
            var propertyPath = match.Groups[1].Success ? match.Groups[1].Value : null;
            if (propertyPath == null)
            {
                // Just {alias} — use the item itself
                return item?.ToString() ?? "";
            }
            var value = BindingEngine.ResolvePath(item, propertyPath);
            return value?.ToString() ?? "";
        });
    }
}
