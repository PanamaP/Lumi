using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Lumi.Core;
using Lumi.Core.Accessibility;

namespace Lumi.Styling;

using Element = Lumi.Core.Element;

/// <summary>
/// Parses HTML template files into Lumi Element trees using AngleSharp.
/// Templates are parsed once at startup — the resulting tree is live.
/// </summary>
public static class HtmlTemplateParser
{
    /// <summary>
    /// Parse an HTML string into a Lumi Element tree.
    /// </summary>
    public static Element Parse(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var root = new BoxElement("body");

        // HTML5 spec may place <template> elements in <head> instead of <body>.
        // Check <head> for any template directives that ended up there.
        if (document.Head != null)
        {
            foreach (var child in document.Head.ChildNodes)
            {
                if (child is IElement el && IsTemplateDirective(el))
                {
                    var directive = CreateTemplateDirective(el);
                    root.AddChild(directive);
                }
            }
        }

        if (document.Body != null)
            ParseChildren(document.Body, root);

        return root;
    }

    /// <summary>
    /// Parse an HTML file into a Lumi Element tree.
    /// </summary>
    public static Element ParseFile(string filePath)
    {
        var html = File.ReadAllText(filePath);
        return Parse(html);
    }

    private static void ParseChildren(INode parent, Element lumiParent)
    {
        foreach (var child in parent.ChildNodes)
        {
            switch (child)
            {
                case IElement htmlElement when IsTemplateDirective(htmlElement):
                    var directive = CreateTemplateDirective(htmlElement);
                    lumiParent.AddChild(directive);
                    // Do NOT recurse — template HTML is stored as a string
                    break;

                case IElement htmlElement:
                    var element = CreateElement(htmlElement);
                    lumiParent.AddChild(element);
                    ParseChildren(htmlElement, element);
                    break;

                case IText textNode:
                    var text = textNode.Data?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        // If the parent element is a TextElement, set its text content directly
                        if (lumiParent is TextElement parentText && string.IsNullOrEmpty(parentText.Text))
                        {
                            parentText.Text = text;
                        }
                        else
                        {
                            var textElement = new TextElement(text);
                            lumiParent.AddChild(textElement);
                        }
                    }
                    break;
            }
        }
    }

    private static Element CreateElement(IElement htmlElement)
    {
        var tagName = htmlElement.LocalName.ToLowerInvariant();
        var element = ElementRegistry.Create(tagName);

        // Map id
        var id = htmlElement.GetAttribute("id");
        if (!string.IsNullOrEmpty(id))
            element.Id = id;

        // Map classes
        var classAttr = htmlElement.GetAttribute("class");
        if (!string.IsNullOrEmpty(classAttr))
            element.Classes = new ClassList(classAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        // Map inline style
        var style = htmlElement.GetAttribute("style");
        if (!string.IsNullOrEmpty(style))
            element.InlineStyle = style;

        // Map data-* and other attributes
        foreach (var attr in htmlElement.Attributes)
        {
            element.Attributes[attr.Name] = attr.Value;
        }

        // Special handling for input elements
        if (element is InputElement input)
        {
            var type = htmlElement.GetAttribute("type");
            if (!string.IsNullOrEmpty(type))
                input.InputType = type;

            var value = htmlElement.GetAttribute("value");
            if (value != null)
                input.Value = value;

            var placeholder = htmlElement.GetAttribute("placeholder");
            if (placeholder != null)
                input.Placeholder = placeholder;

            if (htmlElement.HasAttribute("disabled"))
                input.IsDisabled = true;

            if (htmlElement.HasAttribute("checked"))
                input.IsChecked = true;
        }

        // Special handling for image elements
        if (element is ImageElement img)
        {
            var src = htmlElement.GetAttribute("src");
            if (!string.IsNullOrEmpty(src))
                img.Source = src;
        }

        AriaParser.ApplyAriaAttributes(element);
        return element;
    }

    /// <summary>
    /// Returns true if the HTML element is a &lt;template&gt; with a for or if directive.
    /// </summary>
    private static bool IsTemplateDirective(IElement htmlElement)
    {
        return htmlElement.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase)
            && (htmlElement.HasAttribute("for") || htmlElement.HasAttribute("if"));
    }

    /// <summary>
    /// Creates a TemplateForElement or TemplateIfElement from a &lt;template&gt; tag.
    /// </summary>
    private static Element CreateTemplateDirective(IElement htmlElement)
    {
        // HTML5 <template> elements store their content in a DocumentFragment
        // accessible via .Content, not as regular child nodes.
        string innerHtml;
        if (htmlElement is IHtmlTemplateElement templateEl)
        {
            // Serialize the document fragment's children to get the template HTML
            using var sw = new StringWriter();
            foreach (var child in templateEl.Content.ChildNodes)
                child.ToHtml(sw, new AngleSharp.Html.HtmlMarkupFormatter());
            innerHtml = sw.ToString();
        }
        else
        {
            innerHtml = htmlElement.InnerHtml;
        }

        var forAttr = htmlElement.GetAttribute("for");
        if (!string.IsNullOrEmpty(forAttr))
        {
            return new TemplateForElement
            {
                CollectionPath = StripBindingBraces(forAttr),
                ItemAlias = htmlElement.GetAttribute("as") ?? "item",
                TemplateHtml = innerHtml
            };
        }

        var ifAttr = htmlElement.GetAttribute("if");
        if (!string.IsNullOrEmpty(ifAttr))
        {
            return new TemplateIfElement
            {
                ConditionPath = StripBindingBraces(ifAttr),
                TemplateHtml = innerHtml
            };
        }

        // Shouldn't reach here given IsTemplateDirective check
        return new BoxElement("template");
    }

    /// <summary>
    /// Strips outer curly braces from a binding expression like "{Items}" → "Items".
    /// </summary>
    private static string StripBindingBraces(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed[1..^1].Trim();
        return trimmed;
    }
}
