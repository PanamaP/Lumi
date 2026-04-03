using ExCSS;

namespace Lumi.Styling;

public class ParsedStyleSheet
{
    public List<ParsedStyleRule> Rules { get; } = [];
}

public class ParsedStyleRule
{
    public string SelectorText { get; init; } = "";
    public List<ParsedDeclaration> Declarations { get; } = [];
    public SelectorSpecificity Specificity { get; init; }
}

public record struct ParsedDeclaration(string Property, string Value);

public record struct SelectorSpecificity(int A, int B, int C) : IComparable<SelectorSpecificity>
{
    public int CompareTo(SelectorSpecificity other)
    {
        if (A != other.A) return A.CompareTo(other.A);
        if (B != other.B) return B.CompareTo(other.B);
        return C.CompareTo(other.C);
    }

    public static bool operator >(SelectorSpecificity left, SelectorSpecificity right) => left.CompareTo(right) > 0;
    public static bool operator <(SelectorSpecificity left, SelectorSpecificity right) => left.CompareTo(right) < 0;
    public static bool operator >=(SelectorSpecificity left, SelectorSpecificity right) => left.CompareTo(right) >= 0;
    public static bool operator <=(SelectorSpecificity left, SelectorSpecificity right) => left.CompareTo(right) <= 0;
}

/// <summary>
/// Parses CSS text into a <see cref="ParsedStyleSheet"/> using ExCSS.
/// </summary>
public static class CssParser
{
    public static ParsedStyleSheet Parse(string css)
    {
        // Pre-process CSS variables before ExCSS parsing,
        // since ExCSS 4.x silently drops --custom-property declarations and var() values.
        css = CssVariablePreProcessor.Process(css);

        var parser = new StylesheetParser();
        var stylesheet = parser.Parse(css);
        var result = new ParsedStyleSheet();

        foreach (var rule in stylesheet.StyleRules)
        {
            if (rule is StyleRule styleRule)
            {
                var parsed = new ParsedStyleRule
                {
                    SelectorText = styleRule.SelectorText,
                    Specificity = ConvertSpecificity(styleRule.Selector.Specificity)
                };

                foreach (var prop in styleRule.Style.Declarations)
                {
                    var value = prop.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                        parsed.Declarations.Add(new ParsedDeclaration(prop.Name, value));
                }

                result.Rules.Add(parsed);
            }
        }

        return result;
    }

    public static ParsedStyleSheet ParseFile(string filePath)
    {
        var css = File.ReadAllText(filePath);
        return Parse(css);
    }

    /// <summary>
    /// Parses inline style declarations (no selector) into a list of declarations.
    /// </summary>
    // Cache parsed inline styles to avoid re-parsing unchanged values every frame.
    // Capped at 512 entries to prevent unbounded growth with dynamic styles.
    private static readonly Dictionary<string, List<ParsedDeclaration>> _inlineStyleCache = new(64);
    private const int MaxInlineCacheSize = 512;

    public static List<ParsedDeclaration> ParseInlineStyle(string inlineStyle)
    {
        if (_inlineStyleCache.TryGetValue(inlineStyle, out var cached))
            return cached;

        // Evict oldest entries if cache is full
        if (_inlineStyleCache.Count >= MaxInlineCacheSize)
            _inlineStyleCache.Clear();

        // Wrap in a dummy rule so ExCSS can parse it
        var css = $"__inline__ {{ {inlineStyle} }}";
        var parser = new StylesheetParser();
        var stylesheet = parser.Parse(css);
        var declarations = new List<ParsedDeclaration>();

        foreach (var rule in stylesheet.StyleRules)
        {
            if (rule is StyleRule styleRule)
            {
                foreach (var prop in styleRule.Style.Declarations)
                {
                    var value = prop.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                        declarations.Add(new ParsedDeclaration(prop.Name, value));
                }
            }
        }

        _inlineStyleCache[inlineStyle] = declarations;
        return declarations;
    }

    private static SelectorSpecificity ConvertSpecificity(Priority priority) =>
        new(priority.Ids, priority.Classes, priority.Tags);
}
