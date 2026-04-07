using System.Text;

namespace Lumi.Styling;

public class ParsedStyleSheet
{
    public List<ParsedStyleRule> Rules { get; } = [];
    public List<MediaRule> MediaRules { get; } = [];
    public Dictionary<string, ParsedKeyframes> Keyframes { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A @media rule containing a condition and nested style rules.
/// </summary>
public class MediaRule
{
    public MediaCondition Condition { get; init; } = new();
    public List<ParsedStyleRule> Rules { get; } = [];
}

/// <summary>
/// Parsed @media condition supporting min-width, max-width, min-height, max-height, orientation.
/// </summary>
public class MediaCondition
{
    public float? MinWidth { get; set; }
    public float? MaxWidth { get; set; }
    public float? MinHeight { get; set; }
    public float? MaxHeight { get; set; }
    public string? Orientation { get; set; }

    public bool Evaluate(float viewportWidth, float viewportHeight)
    {
        if (MinWidth.HasValue && viewportWidth < MinWidth.Value) return false;
        if (MaxWidth.HasValue && viewportWidth > MaxWidth.Value) return false;
        if (MinHeight.HasValue && viewportHeight < MinHeight.Value) return false;
        if (MaxHeight.HasValue && viewportHeight > MaxHeight.Value) return false;
        if (Orientation != null)
        {
            bool isPortrait = viewportHeight >= viewportWidth;
            if (Orientation == "portrait" && !isPortrait) return false;
            if (Orientation == "landscape" && isPortrait) return false;
        }
        return true;
    }
}

/// <summary>
/// Parsed @keyframes definition with named percentage stops.
/// </summary>
public class ParsedKeyframes
{
    public string Name { get; init; } = "";
    public List<ParsedKeyframe> Frames { get; } = [];
}

public class ParsedKeyframe
{
    public float Percent { get; init; }
    public List<ParsedDeclaration> Declarations { get; } = [];
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
/// Lightweight CSS parser — hand-written tokenizer with zero external dependencies.
/// Handles rule extraction, declaration parsing, specificity calculation,
/// and comment stripping. Replaces ExCSS which silently dropped var() and
/// custom properties, and expanded shorthands inconsistently.
/// </summary>
public static class CssParser
{
    private static readonly Dictionary<string, List<ParsedDeclaration>> _inlineStyleCache = new(64);
    private const int MaxInlineCacheSize = 512;

    public static ParsedStyleSheet Parse(string css)
    {
        // Resolve var() references and remove --custom-property declarations
        css = CssVariablePreProcessor.Process(css);
        css = StripComments(css);

        var result = new ParsedStyleSheet();
        ExtractRules(css, result);
        return result;
    }

    public static ParsedStyleSheet ParseFile(string filePath)
    {
        var css = File.ReadAllText(filePath);
        return Parse(css);
    }

    /// <summary>
    /// Parses inline style declarations (no selector) into a list of declarations.
    /// Results are cached to avoid re-parsing unchanged values every frame.
    /// </summary>
    public static List<ParsedDeclaration> ParseInlineStyle(string inlineStyle)
    {
        if (_inlineStyleCache.TryGetValue(inlineStyle, out var cached))
            return cached;

        if (_inlineStyleCache.Count >= MaxInlineCacheSize)
            _inlineStyleCache.Clear();

        var declarations = ParseDeclarationBlock(inlineStyle);
        _inlineStyleCache[inlineStyle] = declarations;
        return declarations;
    }

    // ─── Comment stripping ─────────────────────────────────────────

    private static string StripComments(string css)
    {
        if (css.IndexOf("/*", StringComparison.Ordinal) < 0)
            return css;

        var sb = new StringBuilder(css.Length);
        int i = 0;
        while (i < css.Length)
        {
            if (i + 1 < css.Length && css[i] == '/' && css[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < css.Length)
                {
                    if (css[i] == '*' && css[i + 1] == '/')
                    {
                        i += 2;
                        break;
                    }
                    i++;
                }
            }
            else
            {
                sb.Append(css[i]);
                i++;
            }
        }
        return sb.ToString();
    }

    // ─── Rule extraction ───────────────────────────────────────────

    private static void ExtractRules(string css, ParsedStyleSheet result)
    {
        int i = 0;
        int len = css.Length;

        while (i < len)
        {
            SkipWhitespace(css, ref i);
            if (i >= len) break;

            // @-rule: parse @media and @keyframes, skip others
            if (css[i] == '@')
            {
                ParseAtRule(css, ref i, result);
                continue;
            }

            // Find opening brace
            int bracePos = IndexOfUnquoted(css, '{', i);
            if (bracePos < 0) break;

            string selectorText = css[i..bracePos].Trim();

            // Find matching closing brace
            int closePos = FindMatchingBrace(css, bracePos);
            if (closePos < 0) break;

            string blockText = css[(bracePos + 1)..closePos];

            if (selectorText.Length > 0)
            {
                var declarations = ParseDeclarationBlock(blockText);
                if (declarations.Count > 0)
                {
                    // Handle comma-separated selectors: ".a, .b { }" → 2 rules
                    foreach (var sel in SplitOnComma(selectorText))
                    {
                        var trimmed = sel.Trim();
                        if (trimmed.Length == 0) continue;

                        var rule = new ParsedStyleRule
                        {
                            SelectorText = trimmed,
                            Specificity = CalculateSpecificity(trimmed)
                        };
                        rule.Declarations.AddRange(declarations);
                        result.Rules.Add(rule);
                    }
                }
            }

            i = closePos + 1;
        }
    }

    // ─── Declaration parsing ───────────────────────────────────────

    private static List<ParsedDeclaration> ParseDeclarationBlock(string block)
    {
        var declarations = new List<ParsedDeclaration>();

        foreach (var segment in SplitOnSemicolon(block))
        {
            var trimmed = segment.Trim();
            if (trimmed.Length == 0) continue;

            // Find property:value separator
            int colonIdx = IndexOfPropertyColon(trimmed);
            if (colonIdx <= 0) continue;

            var property = trimmed[..colonIdx].Trim().ToLowerInvariant();
            var value = trimmed[(colonIdx + 1)..].Trim();

            // Strip !important flag
            if (value.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
            {
                value = value[..^"!important".Length].Trim();
                if (value.Length > 0 && value[^1] == '!')
                    value = value[..^1].Trim();
            }

            if (property.Length > 0 && value.Length > 0)
                declarations.Add(new ParsedDeclaration(property, value));
        }

        return declarations;
    }

    // ─── Specificity calculation ───────────────────────────────────

    /// <summary>
    /// Calculates CSS selector specificity (A, B, C) where:
    /// A = ID selectors (#id), B = class/attr/pseudo-class, C = type/pseudo-element.
    /// </summary>
    internal static SelectorSpecificity CalculateSpecificity(string selector)
    {
        int ids = 0, classes = 0, tags = 0;
        int i = 0;
        int len = selector.Length;

        while (i < len)
        {
            char ch = selector[i];

            switch (ch)
            {
                case '#':
                    ids++;
                    i++;
                    SkipIdentChars(selector, ref i);
                    break;

                case '.':
                    classes++;
                    i++;
                    SkipIdentChars(selector, ref i);
                    break;

                case '[':
                    classes++;
                    i++;
                    while (i < len && selector[i] != ']') i++;
                    if (i < len) i++;
                    break;

                case ':':
                    i++;
                    if (i < len && selector[i] == ':')
                    {
                        // Pseudo-element (::before, ::after) → type selector
                        tags++;
                        i++;
                        SkipIdentChars(selector, ref i);
                    }
                    else
                    {
                        // Pseudo-class (:hover, :focus, :root) → class selector
                        classes++;
                        SkipIdentChars(selector, ref i);
                        // Skip parenthesized content (:nth-child(2n+1), :not(.x))
                        if (i < len && selector[i] == '(')
                        {
                            int depth = 1;
                            i++;
                            while (i < len && depth > 0)
                            {
                                if (selector[i] == '(') depth++;
                                else if (selector[i] == ')') depth--;
                                i++;
                            }
                        }
                    }
                    break;

                case '*':
                case '>':
                case '+':
                case '~':
                    i++;
                    break;

                default:
                    if (char.IsWhiteSpace(ch))
                    {
                        i++;
                    }
                    else if (IsIdentStartChar(ch))
                    {
                        tags++;
                        SkipIdentChars(selector, ref i);
                    }
                    else
                    {
                        i++;
                    }
                    break;
            }
        }

        return new SelectorSpecificity(ids, classes, tags);
    }

    // ─── Helpers ───────────────────────────────────────────────────

    private static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    private static void SkipIdentChars(string s, ref int i)
    {
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '-' || s[i] == '_'))
            i++;
    }

    private static bool IsIdentStartChar(char c) =>
        char.IsLetter(c) || c == '_';

    private static int IndexOfUnquoted(string s, char target, int start)
    {
        bool inSingle = false, inDouble = false;

        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];

            if (inSingle) { if (c == '\'' && (i == 0 || s[i - 1] != '\\')) inSingle = false; continue; }
            if (inDouble) { if (c == '"' && (i == 0 || s[i - 1] != '\\')) inDouble = false; continue; }

            if (c == '\'') inSingle = true;
            else if (c == '"') inDouble = true;
            else if (c == target) return i;
        }

        return -1;
    }

    private static int FindMatchingBrace(string s, int openPos)
    {
        int depth = 1;
        bool inSingle = false, inDouble = false;

        for (int i = openPos + 1; i < s.Length; i++)
        {
            char c = s[i];

            if (inSingle) { if (c == '\'' && s[i - 1] != '\\') inSingle = false; continue; }
            if (inDouble) { if (c == '"' && s[i - 1] != '\\') inDouble = false; continue; }

            switch (c)
            {
                case '\'': inSingle = true; break;
                case '"': inDouble = true; break;
                case '{': depth++; break;
                case '}':
                    if (--depth == 0) return i;
                    break;
            }
        }

        return -1;
    }

    private static void ParseAtRule(string s, ref int i, ParsedStyleSheet result)
    {
        i++; // skip '@'

        // Determine which @-rule this is
        int nameStart = i;
        while (i < s.Length && char.IsLetterOrDigit(s[i]) && s[i] != '{' && s[i] != ';')
            i++;
        string ruleName = s[nameStart..i].Trim().ToLowerInvariant();

        if (ruleName == "media")
        {
            ParseMediaRule(s, ref i, result);
            return;
        }

        if (ruleName == "keyframes")
        {
            ParseKeyframesRule(s, ref i, result);
            return;
        }

        // Unknown @-rule: skip it
        SkipAtRuleBody(s, ref i);
    }

    private static void ParseMediaRule(string s, ref int i, ParsedStyleSheet result)
    {
        // Find the opening brace to extract the condition
        int bracePos = s.IndexOf('{', i);
        if (bracePos < 0) { i = s.Length; return; }

        string conditionText = s[i..bracePos].Trim();
        var condition = ParseMediaCondition(conditionText);

        // Find matching closing brace
        int closePos = FindMatchingBrace(s, bracePos);
        if (closePos < 0) { i = s.Length; return; }

        // Parse inner rules
        string innerCss = s[(bracePos + 1)..closePos];
        var innerSheet = new ParsedStyleSheet();
        ExtractRules(innerCss, innerSheet);

        if (innerSheet.Rules.Count > 0)
        {
            var mediaRule = new MediaRule { Condition = condition };
            mediaRule.Rules.AddRange(innerSheet.Rules);
            result.MediaRules.Add(mediaRule);
        }

        i = closePos + 1;
    }

    private static MediaCondition ParseMediaCondition(string text)
    {
        var condition = new MediaCondition();

        // Strip leading parens from entire condition, handle "screen and (...)"
        // Remove media type keywords: "screen", "all", "print"
        text = text.Replace("screen", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("all", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("print", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("and", " ", StringComparison.OrdinalIgnoreCase)
                   .Trim();

        // Parse individual conditions: (min-width: 768px) (max-height: 600px)
        int pos = 0;
        while (pos < text.Length)
        {
            int parenStart = text.IndexOf('(', pos);
            if (parenStart < 0) break;
            int parenEnd = text.IndexOf(')', parenStart);
            if (parenEnd < 0) break;

            string expr = text[(parenStart + 1)..parenEnd].Trim();
            int colonIdx = expr.IndexOf(':');
            if (colonIdx > 0)
            {
                string feature = expr[..colonIdx].Trim().ToLowerInvariant();
                string val = expr[(colonIdx + 1)..].Trim();

                float px = ParseMediaLength(val);

                switch (feature)
                {
                    case "min-width": condition.MinWidth = px; break;
                    case "max-width": condition.MaxWidth = px; break;
                    case "min-height": condition.MinHeight = px; break;
                    case "max-height": condition.MaxHeight = px; break;
                    case "orientation": condition.Orientation = val.ToLowerInvariant(); break;
                }
            }
            else if (expr.Equals("portrait", StringComparison.OrdinalIgnoreCase) ||
                     expr.Equals("landscape", StringComparison.OrdinalIgnoreCase))
            {
                condition.Orientation = expr.ToLowerInvariant();
            }

            pos = parenEnd + 1;
        }

        return condition;
    }

    private static float ParseMediaLength(string value)
    {
        value = value.Trim();
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            value = value[..^2].Trim();
        else if (value.EndsWith("em", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(value[..^2].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var emVal))
                return emVal * 16f; // assume root font size
        }

        return float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var px) ? px : 0;
    }

    private static void ParseKeyframesRule(string s, ref int i, ParsedStyleSheet result)
    {
        SkipWhitespace(s, ref i);

        // Extract animation name
        int nameStart = i;
        while (i < s.Length && s[i] != '{' && s[i] != ' ')
            i++;
        string name = s[nameStart..i].Trim().Trim('"', '\'');

        // Find opening brace
        int bracePos = s.IndexOf('{', i);
        if (bracePos < 0) { i = s.Length; return; }

        int closePos = FindMatchingBrace(s, bracePos);
        if (closePos < 0) { i = s.Length; return; }

        string innerCss = s[(bracePos + 1)..closePos];

        var keyframes = new ParsedKeyframes { Name = name };
        ParseKeyframeStops(innerCss, keyframes);

        if (keyframes.Frames.Count > 0)
            result.Keyframes[name] = keyframes;

        i = closePos + 1;
    }

    private static void ParseKeyframeStops(string css, ParsedKeyframes keyframes)
    {
        int i = 0;
        int len = css.Length;

        while (i < len)
        {
            SkipWhitespace(css, ref i);
            if (i >= len) break;

            int bracePos = IndexOfUnquoted(css, '{', i);
            if (bracePos < 0) break;

            string stopText = css[i..bracePos].Trim();
            int closePos = FindMatchingBrace(css, bracePos);
            if (closePos < 0) break;

            string blockText = css[(bracePos + 1)..closePos];
            var declarations = ParseDeclarationBlock(blockText);

            // Parse stop percentages (handles "0%", "100%", "from", "to", comma-separated)
            foreach (var stop in stopText.Split(',', StringSplitOptions.TrimEntries))
            {
                float percent = stop.ToLowerInvariant() switch
                {
                    "from" => 0f,
                    "to" => 1f,
                    _ when stop.EndsWith('%') &&
                        float.TryParse(stop[..^1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var p) => p / 100f,
                    _ => -1
                };

                if (percent >= 0)
                {
                    var frame = new ParsedKeyframe { Percent = percent };
                    frame.Declarations.AddRange(declarations);
                    keyframes.Frames.Add(frame);
                }
            }

            i = closePos + 1;
        }
    }

    private static void SkipAtRuleBody(string s, ref int i)
    {
        i++; // skip '@'
        while (i < s.Length)
        {
            if (s[i] == ';') { i++; return; }
            if (s[i] == '{')
            {
                int close = FindMatchingBrace(s, i);
                i = close >= 0 ? close + 1 : s.Length;
                return;
            }
            i++;
        }
    }

    private static List<string> SplitOnSemicolon(string block)
    {
        var parts = new List<string>();
        int start = 0;
        bool inSingle = false, inDouble = false;
        int parenDepth = 0;

        for (int i = 0; i < block.Length; i++)
        {
            char c = block[i];

            if (inSingle) { if (c == '\'' && (i == 0 || block[i - 1] != '\\')) inSingle = false; continue; }
            if (inDouble) { if (c == '"' && (i == 0 || block[i - 1] != '\\')) inDouble = false; continue; }

            switch (c)
            {
                case '\'': inSingle = true; break;
                case '"': inDouble = true; break;
                case '(': parenDepth++; break;
                case ')': if (parenDepth > 0) parenDepth--; break;
                case ';' when parenDepth == 0:
                    parts.Add(block[start..i]);
                    start = i + 1;
                    break;
            }
        }

        if (start < block.Length)
            parts.Add(block[start..]);

        return parts;
    }

    private static List<string> SplitOnComma(string selector)
    {
        var parts = new List<string>();
        int start = 0;
        int parenDepth = 0;

        for (int i = 0; i < selector.Length; i++)
        {
            char c = selector[i];
            if (c == '(') parenDepth++;
            else if (c == ')') { if (parenDepth > 0) parenDepth--; }
            else if (c == ',' && parenDepth == 0)
            {
                parts.Add(selector[start..i]);
                start = i + 1;
            }
        }

        parts.Add(selector[start..]);
        return parts;
    }

    private static int IndexOfPropertyColon(string decl)
    {
        bool inSingle = false, inDouble = false;

        for (int i = 0; i < decl.Length; i++)
        {
            char c = decl[i];

            if (inSingle) { if (c == '\'' && (i == 0 || decl[i - 1] != '\\')) inSingle = false; continue; }
            if (inDouble) { if (c == '"' && (i == 0 || decl[i - 1] != '\\')) inDouble = false; continue; }

            if (c == '\'') inSingle = true;
            else if (c == '"') inDouble = true;
            else if (c == ':') return i;
        }

        return -1;
    }
}
