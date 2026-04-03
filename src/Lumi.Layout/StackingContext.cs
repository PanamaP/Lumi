namespace Lumi.Layout;

using Lumi.Core;

public static class StackingContext
{
    /// <summary>
    /// Returns elements in paint order (back to front), respecting z-index.
    /// Elements with higher z-index paint later (on top).
    /// </summary>
    public static List<Element> GetPaintOrder(Element root)
    {
        var result = new List<Element>();
        CollectInPaintOrder(root, result);
        return result;
    }

    private static void CollectInPaintOrder(Element element, List<Element> result)
    {
        if (element.ComputedStyle.Display == DisplayMode.None)
            return;

        result.Add(element);

        var sorted = element.Children
            .Where(c => c.ComputedStyle.Display != DisplayMode.None)
            .OrderBy(c => c.ComputedStyle.ZIndex)
            .ToList();

        foreach (var child in sorted)
            CollectInPaintOrder(child, result);
    }
}
