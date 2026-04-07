using Element = Lumi.Core.Element;

namespace Lumi.Styling;

public record PseudoClassState(
    bool IsHovered = false,
    bool IsFocused = false,
    bool IsActive = false,
    bool IsDisabled = false);

/// <summary>
/// Matches CSS selectors against Lumi elements.
/// Supports type, class, ID, universal, compound, descendant/child combinators,
/// comma-separated groups, and structural pseudo-classes.
/// </summary>
public static class SelectorMatcher
{
    public static bool Matches(Element element, string selectorText, PseudoClassState? pseudoState = null)
    {
        // Comma-separated selectors: match if ANY group matches
        var groups = SplitSelectorGroups(selectorText);
        foreach (var group in groups)
        {
            if (MatchesSingleSelector(element, group.Trim(), pseudoState))
                return true;
        }
        return false;
    }

    private static bool MatchesSingleSelector(Element element, string selector, PseudoClassState? pseudoState)
    {
        var parts = TokenizeCombinators(selector);
        if (parts.Count == 0) return false;

        // Start from the rightmost (subject) part
        int i = parts.Count - 1;
        if (!MatchesCompound(element, parts[i], pseudoState))
            return false;

        Element? current = element;
        i--;

        while (i >= 0)
        {
            var combinator = parts[i];
            i--;
            if (i < 0) return false;
            var part = parts[i];
            i--;

            if (combinator == ">")
            {
                // Child combinator: parent must match
                current = current?.Parent;
                if (current == null || !MatchesCompound(current, part, pseudoState))
                    return false;
            }
            else if (combinator == " ")
            {
                // Descendant combinator: any ancestor must match
                var found = false;
                current = current?.Parent;
                while (current != null)
                {
                    if (MatchesCompound(current, part, pseudoState))
                    {
                        found = true;
                        break;
                    }
                    current = current.Parent;;
                }
                if (!found) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Tokenizes a single selector (no commas) into alternating [compound, combinator, compound, ...] parts.
    /// The result list has an odd number of elements: compound (combinator compound)*.
    /// </summary>
    private static List<string> TokenizeCombinators(string selector)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int i = 0;

        while (i < selector.Length)
        {
            char c = selector[i];

            if (c == '>')
            {
                var compound = current.ToString().Trim();
                if (compound.Length > 0) result.Add(compound);
                current.Clear();
                result.Add(">");
                i++;
            }
            else if (c == ' ')
            {
                // Could be a descendant combinator or whitespace around '>'
                var compound = current.ToString().Trim();
                // Skip whitespace
                while (i < selector.Length && selector[i] == ' ') i++;

                if (i < selector.Length && selector[i] == '>')
                {
                    // Whitespace before '>', not a descendant combinator
                    if (compound.Length > 0) result.Add(compound);
                    current.Clear();
                    result.Add(">");
                    i++; // skip '>'
                    // Skip whitespace after '>'
                    while (i < selector.Length && selector[i] == ' ') i++;
                }
                else
                {
                    // Descendant combinator
                    if (compound.Length > 0)
                    {
                        result.Add(compound);
                        current.Clear();
                        result.Add(" ");
                    }
                }
            }
            else
            {
                current.Append(c);
                i++;
            }
        }

        var last = current.ToString().Trim();
        if (last.Length > 0) result.Add(last);

        return result;
    }

    /// <summary>
    /// Matches a compound selector (no combinators) like "div.class#id:hover".
    /// </summary>
    private static bool MatchesCompound(Element element, string compound, PseudoClassState? pseudoState)
    {
        var segments = ParseCompound(compound);

        foreach (var seg in segments)
        {
            if (!MatchesSimple(element, seg, pseudoState))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Splits a compound selector into its simple selector parts.
    /// e.g. "div.foo#bar:hover" → ["div", ".foo", "#bar", ":hover"]
    /// </summary>
    private static List<string> ParseCompound(string compound)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < compound.Length; i++)
        {
            char c = compound[i];

            if ((c == '.' || c == '#' || c == ':') && current.Length > 0)
            {
                parts.Add(current.ToString());
                current.Clear();
            }

            current.Append(c);

            // Handle pseudo-class with parentheses like :nth-child(2n+1)
            if (c == '(' && current.Length > 0)
            {
                int depth = 1;
                i++;
                while (i < compound.Length && depth > 0)
                {
                    if (compound[i] == '(') depth++;
                    else if (compound[i] == ')') depth--;
                    current.Append(compound[i]);
                    i++;
                }
                i--; // compensate for loop increment
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts;
    }

    private static bool MatchesSimple(Element element, string simple, PseudoClassState? pseudoState)
    {
        if (simple == "*")
            return true;

        if (simple.StartsWith('#'))
            return element.Id == simple[1..];

        if (simple.StartsWith('.'))
            return element.Classes.Contains(simple[1..]);

        if (simple.StartsWith(':'))
            return MatchesPseudoClass(element, simple, pseudoState);

        // Type selector
        return string.Equals(element.TagName, simple, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPseudoClass(Element element, string pseudo, PseudoClassState? pseudoState)
    {
        var state = pseudoState ?? new PseudoClassState();

        // Extract pseudo-class name (without args)
        var name = pseudo;
        var argStart = pseudo.IndexOf('(');
        if (argStart >= 0)
            name = pseudo[..argStart];

        return name switch
        {
            ":hover" => state.IsHovered,
            ":focus" => state.IsFocused,
            ":active" => state.IsActive,
            ":disabled" => state.IsDisabled,
            ":root" => element.Parent == null,
            ":first-child" => IsFirstChild(element),
            ":last-child" => IsLastChild(element),
            ":nth-child" => false, // TODO: parse An+B syntax
            _ => false
        };
    }

    private static bool IsFirstChild(Element element) =>
        element.Parent != null && element.Parent.Children.Count > 0 && element.Parent.Children[0] == element;

    private static bool IsLastChild(Element element) =>
        element.Parent != null && element.Parent.Children.Count > 0 && element.Parent.Children[^1] == element;

    /// <summary>
    /// Splits comma-separated selector groups, respecting parentheses.
    /// </summary>
    private static List<string> SplitSelectorGroups(string selectorText)
    {
        var groups = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;

        foreach (char c in selectorText)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0)
            {
                groups.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(c);
        }

        if (current.Length > 0)
            groups.Add(current.ToString());

        return groups;
    }
}
