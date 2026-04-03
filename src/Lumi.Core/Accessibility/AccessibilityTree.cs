namespace Lumi.Core.Accessibility;

/// <summary>
/// Builds a flattened accessibility tree from the element tree,
/// filtering out decorative/invisible elements.
/// </summary>
public static class AccessibilityTree
{
    private static readonly HashSet<string> FocusableRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "textbox", "checkbox", "slider", "link", "menuitem", "tab", "switch"
    };

    /// <summary>
    /// Build an accessibility node tree from an element root.
    /// Elements with aria-hidden="true" are excluded along with their subtrees.
    /// </summary>
    public static AccessibilityNode Build(Element root)
    {
        return BuildNode(root) ?? new AccessibilityNode
        {
            Role = "none",
            Name = "",
            Element = root
        };
    }

    private static AccessibilityNode? BuildNode(Element element)
    {
        var info = element.Accessibility;

        // Skip elements marked as aria-hidden
        if (info.IsHidden)
            return null;

        // Skip invisible elements (display:none)
        if (element.ComputedStyle.Display == DisplayMode.None)
            return null;

        var node = new AccessibilityNode
        {
            Role = info.Role ?? "none",
            Name = ComputeName(element),
            Element = element
        };

        foreach (var child in element.Children)
        {
            var childNode = BuildNode(child);
            if (childNode != null)
                node.Children.Add(childNode);
        }

        return node;
    }

    private static string ComputeName(Element element)
    {
        var info = element.Accessibility;

        // 1. aria-label takes highest priority
        if (!string.IsNullOrEmpty(info.Label))
            return info.Label;

        // 2. title attribute
        if (element.Attributes.TryGetValue("title", out var title) && !string.IsNullOrEmpty(title))
            return title;

        // 3. Text content for text elements
        if (element is TextElement textElement && !string.IsNullOrEmpty(textElement.Text))
            return textElement.Text;

        // 4. Value/placeholder for input elements
        if (element is InputElement input)
        {
            if (!string.IsNullOrEmpty(input.Placeholder))
                return input.Placeholder;
        }

        return "";
    }

    /// <summary>
    /// Returns all focusable nodes in document order for keyboard navigation.
    /// </summary>
    public static List<AccessibilityNode> GetFocusableNodes(AccessibilityNode root)
    {
        var result = new List<AccessibilityNode>();
        CollectFocusable(root, result);
        return result;
    }

    private static void CollectFocusable(AccessibilityNode node, List<AccessibilityNode> result)
    {
        if (node.Element.IsFocusable || FocusableRoles.Contains(node.Role))
            result.Add(node);

        foreach (var child in node.Children)
            CollectFocusable(child, result);
    }
}
