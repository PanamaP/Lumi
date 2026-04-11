using Lumi.Core;
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
            else if (combinator == "+")
            {
                // Adjacent sibling combinator: immediately preceding sibling must match
                current = GetPreviousSibling(current);
                if (current == null || !MatchesCompound(current, part, pseudoState))
                    return false;
            }
            else if (combinator == "~")
            {
                // General sibling combinator: any preceding sibling must match
                if (current?.Parent == null) return false;
                var siblings = current.Parent.Children;
                int idx = IndexOfChild(siblings, current);
                var found = false;
                for (int j = idx - 1; j >= 0; j--)
                {
                    if (MatchesCompound(siblings[j], part, pseudoState))
                    {
                        current = siblings[j];
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
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

        int parenDepth = 0;
        int bracketDepth = 0;

        while (i < selector.Length)
        {
            char c = selector[i];

            // Track parenthesis depth so spaces inside pseudo-class args
            // (e.g. ":is(.a, .b)") are not treated as combinators.
            if (c == '(') { parenDepth++; current.Append(c); i++; continue; }
            if (c == ')') { parenDepth--; current.Append(c); i++; continue; }

            // Track bracket depth so characters inside attribute selectors
            // (e.g. "[class~=\"active\"]") are not treated as combinators.
            if (c == '[') { bracketDepth++; current.Append(c); i++; continue; }
            if (c == ']') { bracketDepth--; current.Append(c); i++; continue; }

            if (parenDepth > 0 || bracketDepth > 0)
            {
                current.Append(c);
                i++;
                continue;
            }

            if (c == '>' || c == '+' || c == '~')
            {
                var compound = current.ToString().Trim();
                if (compound.Length > 0) result.Add(compound);
                current.Clear();
                result.Add(c.ToString());
                i++;
                // Skip whitespace after combinator
                while (i < selector.Length && selector[i] == ' ') i++;
            }
            else if (c == ' ')
            {
                // Could be a descendant combinator or whitespace around an explicit combinator
                var compound = current.ToString().Trim();
                // Skip whitespace
                while (i < selector.Length && selector[i] == ' ') i++;

                if (i < selector.Length && (selector[i] == '>' || selector[i] == '+' || selector[i] == '~'))
                {
                    // Whitespace before an explicit combinator
                    if (compound.Length > 0) result.Add(compound);
                    current.Clear();
                    char comb = selector[i];
                    result.Add(comb.ToString());
                    i++;
                    // Skip whitespace after combinator
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

            // Attribute selector: consume entire [...] block as one part
            if (c == '[')
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                current.Append(c);
                i++;
                int depth = 1;
                while (i < compound.Length && depth > 0)
                {
                    if (compound[i] == '[') depth++;
                    else if (compound[i] == ']') depth--;
                    current.Append(compound[i]);
                    i++;
                }
                i--; // compensate for loop increment
                continue;
            }

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

        if (simple.StartsWith('['))
            return MatchesAttributeSelector(element, simple);

        // Type selector
        return string.Equals(element.TagName, simple, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPseudoClass(Element element, string pseudo, PseudoClassState? pseudoState)
    {
        var state = pseudoState ?? new PseudoClassState();

        // Extract pseudo-class name and argument
        var name = pseudo;
        string? arg = null;
        var argStart = pseudo.IndexOf('(');
        if (argStart >= 0)
        {
            name = pseudo[..argStart];
            var argEnd = pseudo.LastIndexOf(')');
            arg = argEnd > argStart
                ? pseudo[(argStart + 1)..argEnd].Trim()
                : pseudo[(argStart + 1)..].Trim();
        }

        return name switch
        {
            ":hover" => state.IsHovered,
            ":focus" => state.IsFocused,
            ":active" => state.IsActive,
            ":disabled" => state.IsDisabled,
            ":root" => element.Parent == null,
            ":first-child" => IsFirstChild(element),
            ":last-child" => IsLastChild(element),
            ":nth-child" => MatchesNthChild(element, arg, fromEnd: false),
            ":nth-last-child" => MatchesNthChild(element, arg, fromEnd: true),
            ":not" => arg != null && !MatchesCompound(element, arg, pseudoState),
            ":is" => arg != null && MatchesIs(element, arg, pseudoState),
            _ => false
        };
    }

    /// <summary>
    /// Matches a CSS attribute selector like [attr], [attr="value"], [attr~="value"], etc.
    /// </summary>
    private static bool MatchesAttributeSelector(Element element, string selector)
    {
        // Remove surrounding brackets: [attr op "value"] → attr op "value"
        if (selector.Length < 2 || selector[^1] != ']') return false;
        var inner = selector[1..^1].Trim();
        if (inner.Length == 0) return false;

        // Find the operator position
        int opIndex = -1;
        string? op = null;
        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];
            if (c == '=')
            {
                op = "=";
                opIndex = i;
                break;
            }
            if ((c == '~' || c == '|' || c == '^' || c == '$' || c == '*') &&
                i + 1 < inner.Length && inner[i + 1] == '=')
            {
                op = inner.Substring(i, 2);
                opIndex = i;
                break;
            }
        }

        if (op == null)
        {
            // Presence check: [attr]
            return ResolveAttributeValue(element, inner.Trim()) != null;
        }

        var name = inner[..opIndex].Trim();
        var valueStr = inner[(opIndex + op.Length)..].Trim();

        // Strip quotes from value
        if (valueStr.Length >= 2 &&
            ((valueStr[0] == '"' && valueStr[^1] == '"') ||
             (valueStr[0] == '\'' && valueStr[^1] == '\'')))
        {
            valueStr = valueStr[1..^1];
        }

        var attrValue = ResolveAttributeValue(element, name);
        if (attrValue == null) return false;

        return op switch
        {
            "=" => string.Equals(attrValue, valueStr, StringComparison.OrdinalIgnoreCase),
            "~=" => attrValue.Split(' ').Any(w => string.Equals(w, valueStr, StringComparison.OrdinalIgnoreCase)),
            "|=" => string.Equals(attrValue, valueStr, StringComparison.OrdinalIgnoreCase) ||
                    attrValue.StartsWith(valueStr + "-", StringComparison.OrdinalIgnoreCase),
            "^=" => attrValue.StartsWith(valueStr, StringComparison.OrdinalIgnoreCase),
            "$=" => attrValue.EndsWith(valueStr, StringComparison.OrdinalIgnoreCase),
            "*=" => attrValue.Contains(valueStr, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    /// <summary>
    /// Resolves the effective value of a named attribute on an element.
    /// Maps standard HTML attributes to element properties and falls back to the Attributes dictionary.
    /// Returns null if the attribute is not present.
    /// </summary>
    private static string? ResolveAttributeValue(Element element, string name)
    {
        if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
            return element.Id;

        if (string.Equals(name, "class", StringComparison.OrdinalIgnoreCase))
            return element.Classes.Count > 0 ? string.Join(" ", element.Classes) : null;

        if (element is InputElement input)
        {
            if (string.Equals(name, "type", StringComparison.OrdinalIgnoreCase))
                return input.InputType;
            if (string.Equals(name, "value", StringComparison.OrdinalIgnoreCase))
                return input.Value;
            if (string.Equals(name, "placeholder", StringComparison.OrdinalIgnoreCase))
                return input.Placeholder;
            if (string.Equals(name, "disabled", StringComparison.OrdinalIgnoreCase))
                return input.IsDisabled ? "" : null;
            if (string.Equals(name, "checked", StringComparison.OrdinalIgnoreCase))
                return input.IsChecked ? "" : null;
        }

        return element.Attributes.TryGetValue(name, out var val) ? val : null;
    }

    /// <summary>
    /// Matches :nth-child(An+B) or :nth-last-child(An+B).
    /// </summary>
    private static bool MatchesNthChild(Element element, string? arg, bool fromEnd)
    {
        if (arg == null || element.Parent == null) return false;

        var (a, b) = ParseAnPlusB(arg);
        int index = -1;
        var siblings = element.Parent.Children;
        for (int j = 0; j < siblings.Count; j++)
        {
            if (siblings[j] == element) { index = j; break; }
        }
        if (index < 0) return false;

        int pos = fromEnd ? siblings.Count - index : index + 1;
        return MatchesAnPlusB(a, b, pos);
    }

    /// <summary>
    /// Returns true if there exists a non-negative integer k such that A*k + B == pos.
    /// </summary>
    private static bool MatchesAnPlusB(int a, int b, int pos)
    {
        if (a == 0)
            return pos == b;

        int diff = pos - b;
        if (diff % a != 0) return false;
        return diff / a >= 0;
    }

    /// <summary>
    /// Parses CSS An+B microsyntax: odd, even, 3, 2n, 2n+1, -n+3, 3n-2, etc.
    /// Returns (A, B) tuple.
    /// </summary>
    internal static (int A, int B) ParseAnPlusB(string input)
    {
        var s = input.Trim().ToLowerInvariant().Replace(" ", "");

        if (s == "odd") return (2, 1);
        if (s == "even") return (2, 0);

        int nIndex = s.IndexOf('n');
        if (nIndex < 0)
        {
            // Pure integer: "3" → (0, 3)
            return (0, int.Parse(s));
        }

        // Parse A (coefficient of n)
        int a;
        var aPart = s[..nIndex];
        if (aPart is "" or "+")
            a = 1;
        else if (aPart == "-")
            a = -1;
        else
            a = int.Parse(aPart);

        // Parse B (constant term after n)
        int b = 0;
        var rest = s[(nIndex + 1)..];
        if (rest.Length > 0)
            b = int.Parse(rest);

        return (a, b);
    }

    /// <summary>
    /// Matches :is(.a, .b) — any of the comma-separated selectors.
    /// </summary>
    private static bool MatchesIs(Element element, string arg, PseudoClassState? pseudoState)
    {
        var selectors = SplitSelectorGroups(arg);
        foreach (var sel in selectors)
        {
            if (MatchesCompound(element, sel.Trim(), pseudoState))
                return true;
        }
        return false;
    }

    private static bool IsFirstChild(Element element) =>
        element.Parent != null && element.Parent.Children.Count > 0 && element.Parent.Children[0] == element;

    private static bool IsLastChild(Element element) =>
        element.Parent != null && element.Parent.Children.Count > 0 && element.Parent.Children[^1] == element;

    private static Element? GetPreviousSibling(Element? element)
    {
        if (element?.Parent == null) return null;
        var siblings = element.Parent.Children;
        int index = IndexOfChild(siblings, element);
        return index > 0 ? siblings[index - 1] : null;
    }

    private static int IndexOfChild(IReadOnlyList<Element> siblings, Element element)
    {
        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i] == element) return i;
        }
        return -1;
    }

    /// <summary>
    /// Splits comma-separated selector groups, respecting parentheses and brackets.
    /// </summary>
    private static List<string> SplitSelectorGroups(string selectorText)
    {
        var groups = new List<string>();
        var current = new System.Text.StringBuilder();
        int parenDepth = 0;
        int bracketDepth = 0;

        foreach (char c in selectorText)
        {
            if (c == '(') parenDepth++;
            else if (c == ')') parenDepth--;
            else if (c == '[') bracketDepth++;
            else if (c == ']') bracketDepth--;
            else if (c == ',' && parenDepth == 0 && bracketDepth == 0)
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
