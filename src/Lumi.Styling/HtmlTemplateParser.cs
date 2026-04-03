using AngleSharp;
using AngleSharp.Dom;
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
        var body = document.Body;

        if (body == null)
            return new BoxElement("body");

        var root = new BoxElement("body");
        ParseChildren(body, root);
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
}
