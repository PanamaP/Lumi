using System.Collections;

namespace Lumi.Core.Binding;

/// <summary>
/// Walks an element tree and activates template directives (&lt;template for=""&gt;
/// and &lt;template if=""&gt;) by resolving their bindings against a data context.
/// Uses prototype caching and element cloning for efficient rendering.
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

        templateFor.Unbind();
        templateFor.ClearChildren();

        // Parse prototype once and cache it
        if (templateFor._prototype == null)
        {
            templateFor._prototype = HtmlParser!(templateFor.TemplateHtml);
        }

        Element CreateInstance(object item)
        {
            // Clone from cached prototype instead of re-parsing HTML
            var container = new BoxElement("template-item");
            foreach (var protoChild in templateFor._prototype.Children)
            {
                var cloned = protoChild.DeepClone();
                container.AddChild(cloned);
            }
            container.DataContext = item;

            // Create reactive bindings for {alias.xxx} patterns
            CreateBindings(container, templateFor.ItemAlias, item, templateFor._bindings);

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

        // Parse prototype once and cache it
        if (templateIf._prototype == null)
        {
            templateIf._prototype = HtmlParser!(templateIf.TemplateHtml);
        }

        Element CreateContent()
        {
            // Clone from cached prototype
            var container = new BoxElement("template-if-content");
            foreach (var protoChild in templateIf._prototype.Children)
            {
                var cloned = protoChild.DeepClone();
                container.AddChild(cloned);
            }

            // Create reactive bindings for any interpolation patterns
            CreateBindings(container, null, context, templateIf._bindings);

            ResolveDirectives(container, context);
            return container;
        }

        templateIf.BindCondition(context, CreateContent);
        templateIf.SetRendered(condition);
    }

    /// <summary>
    /// Walk an element tree and create TemplateBindings for {alias.PropertyPath} patterns
    /// in text content and attributes. Each binding is reactive — it updates when
    /// the source's properties change.
    /// </summary>
    internal static void CreateBindings(
        Element root, string? alias, object source, List<TemplateBinding> bindings)
    {
        if (alias == null) return;

        CreateElementBindings(root, alias, source, bindings);

        foreach (var child in root.Children)
        {
            // Don't descend into nested template directives — they manage their own bindings
            if (child is TemplateForElement or TemplateIfElement)
                continue;
            CreateBindings(child, alias, source, bindings);
        }
    }

    private static void CreateElementBindings(
        Element element, string alias, object source, List<TemplateBinding> bindings)
    {
        // Bind text content
        if (element is TextElement textEl && !string.IsNullOrEmpty(textEl.Text))
        {
            var binding = TemplateBinding.TryCreate(
                textEl, "Text", isAttribute: false, textEl.Text, alias, source);
            if (binding != null)
                bindings.Add(binding);
        }

        // Bind attributes containing interpolation patterns
        foreach (var kvp in element.Attributes.ToList())
        {
            if (kvp.Value.Contains('{'))
            {
                var binding = TemplateBinding.TryCreate(
                    element, kvp.Key, isAttribute: true, kvp.Value, alias, source);
                if (binding != null)
                    bindings.Add(binding);
            }
        }
    }
}
