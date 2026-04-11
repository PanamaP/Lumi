using Lumi.Core;

namespace Lumi.Styling;

/// <summary>
/// Provides CSS selector querying (querySelector / querySelectorAll) for Lumi elements.
/// </summary>
public static class ElementQueryExtensions
{
    /// <summary>
    /// Returns the first descendant matching the CSS selector, or null.
    /// </summary>
    public static Element? QuerySelector(this Element element, string selector)
    {
        foreach (var child in element.Children)
        {
            if (SelectorMatcher.Matches(child, selector))
                return child;

            var result = QuerySelector(child, selector);
            if (result is not null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Returns all descendants matching the CSS selector.
    /// </summary>
    public static List<Element> QuerySelectorAll(this Element element, string selector)
    {
        var results = new List<Element>();
        CollectMatches(element, selector, results);
        return results;
    }

    private static void CollectMatches(Element element, string selector, List<Element> results)
    {
        foreach (var child in element.Children)
        {
            if (SelectorMatcher.Matches(child, selector))
                results.Add(child);

            CollectMatches(child, selector, results);
        }
    }
}
