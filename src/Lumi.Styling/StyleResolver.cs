using Lumi.Core;
using Element = Lumi.Core.Element;

namespace Lumi.Styling;

/// <summary>
/// Resolves computed styles for an element tree by applying CSS rules in cascade order,
/// inline styles, and property inheritance.
/// </summary>
public class StyleResolver
{
    private readonly List<ParsedStyleSheet> _styleSheets = [];

    // Pooled/reused objects to avoid per-frame allocations
    private readonly List<(ParsedStyleRule Rule, int SheetIndex, int RuleIndex)> _matchBuffer = new(32);
    private readonly HashSet<string> _explicitBuffer = new(32);
    private readonly ComputedStyle _tempStyle = new();

    // Selector match cache: key = element identity + class hash, value = matched rules
    private readonly Dictionary<long, List<(ParsedStyleRule Rule, int SheetIndex, int RuleIndex)>> _selectorCache = new();
    private int _stylesheetVersion;
    private int _lastResolvedVersion;

    public void AddStyleSheet(ParsedStyleSheet sheet)
    {
        _styleSheets.Add(sheet);
        _stylesheetVersion++;
    }

    public void RemoveStyleSheet(ParsedStyleSheet sheet)
    {
        _styleSheets.Remove(sheet);
        _stylesheetVersion++;
    }

    public void ClearStyleSheets()
    {
        _styleSheets.Clear();
        _stylesheetVersion++;
    }

    /// <summary>
    /// Resolve computed styles for the entire element tree.
    /// </summary>
    public void ResolveStyles(Element root, PseudoClassState? pseudoState = null)
    {
        // Invalidate selector cache when stylesheets change
        if (_stylesheetVersion != _lastResolvedVersion)
        {
            _selectorCache.Clear();
            _lastResolvedVersion = _stylesheetVersion;
        }

        ResolveElement(root, null, pseudoState);
    }

    private void ResolveElement(Element element, ComputedStyle? parentStyle, PseudoClassState? pseudoState)
    {
        // 1. Reset temp style to defaults
        _tempStyle.Reset();

        // 2. Get matching rules (cached or computed)
        var matchingRules = GetMatchingRules(element, pseudoState);

        // Track which properties were explicitly set (for inheritance)
        _explicitBuffer.Clear();

        // Set font-size context for em/rem unit resolution (parent's font-size)
        PropertyApplier.SetFontSizeContext(parentStyle?.FontSize ?? 16f);

        // 3. Apply declarations in cascade order (lower specificity first → higher overrides)
        foreach (var (rule, _, _) in matchingRules)
        {
            foreach (var decl in rule.Declarations)
            {
                PropertyApplier.Apply(_tempStyle, decl.Property, decl.Value);
                _explicitBuffer.Add(decl.Property);
            }
        }

        // 4. Apply inline style (highest priority, trumps all stylesheet rules)
        if (!string.IsNullOrWhiteSpace(element.InlineStyle))
        {
            var inlineDeclarations = CssParser.ParseInlineStyle(element.InlineStyle);
            foreach (var decl in inlineDeclarations)
            {
                PropertyApplier.Apply(_tempStyle, decl.Property, decl.Value);
                _explicitBuffer.Add(decl.Property);
            }
        }

        // 5. Inherit inheritable properties from parent
        if (parentStyle != null)
        {
            InheritableProperties.InheritFrom(_tempStyle, parentStyle, _explicitBuffer);
        }

        // 6. Apply resolved values onto the element's existing ComputedStyle
        ApplyToComputedStyle(element.ComputedStyle, _tempStyle);

        // 7. Recurse into children
        foreach (var child in element.Children)
        {
            ResolveElement(child, element.ComputedStyle, pseudoState);
        }
    }

    /// <summary>
    /// Get matching rules for an element, using cache when possible.
    /// Cache key is based on element's class list hash + tag name.
    /// </summary>
    private List<(ParsedStyleRule Rule, int SheetIndex, int RuleIndex)> GetMatchingRules(
        Element element, PseudoClassState? pseudoState)
    {
        // Build a cache key from tag + classes (not identity — elements with same classes share cache)
        long key = ComputeCacheKey(element);

        if (_selectorCache.TryGetValue(key, out var cached))
            return cached;

        // Compute matching rules
        _matchBuffer.Clear();
        for (int s = 0; s < _styleSheets.Count; s++)
        {
            var sheet = _styleSheets[s];
            for (int r = 0; r < sheet.Rules.Count; r++)
            {
                var rule = sheet.Rules[r];
                if (SelectorMatcher.Matches(element, rule.SelectorText, pseudoState))
                {
                    _matchBuffer.Add((rule, s, r));
                }
            }
        }

        // Sort by specificity (stable sort preserves source order for equal specificity)
        _matchBuffer.Sort((a, b) =>
        {
            int cmp = a.Rule.Specificity.CompareTo(b.Rule.Specificity);
            if (cmp != 0) return cmp;
            cmp = a.SheetIndex.CompareTo(b.SheetIndex);
            if (cmp != 0) return cmp;
            return a.RuleIndex.CompareTo(b.RuleIndex);
        });

        // Store a copy in cache
        var result = new List<(ParsedStyleRule Rule, int SheetIndex, int RuleIndex)>(_matchBuffer);
        _selectorCache[key] = result;
        return result;
    }

    /// <summary>
    /// Compute a cache key from element tag name + class list.
    /// Elements with the same tag and classes will share matched rules.
    /// </summary>
    private static long ComputeCacheKey(Element element)
    {
        long hash = 17;
        hash = hash * 31 + (element.TagName?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0);

        if (element.Id != null)
            hash = hash * 31 + element.Id.GetHashCode(StringComparison.Ordinal);

        foreach (var cls in element.Classes)
            hash = hash * 31 + cls.GetHashCode(StringComparison.Ordinal);

        return hash;
    }

    /// <summary>
    /// Copies all resolved property values onto the element's ComputedStyle
    /// (which has an internal setter, so we mutate the existing instance).
    /// </summary>
    private static void ApplyToComputedStyle(ComputedStyle target, ComputedStyle source)
    {
        // Box model
        target.Width = source.Width;
        target.Height = source.Height;
        target.MinWidth = source.MinWidth;
        target.MaxWidth = source.MaxWidth;
        target.MinHeight = source.MinHeight;
        target.MaxHeight = source.MaxHeight;
        target.Margin = source.Margin;
        target.Padding = source.Padding;
        target.BorderWidth = source.BorderWidth;
        target.BoxSizing = source.BoxSizing;

        // Layout
        target.Display = source.Display;
        target.Position = source.Position;
        target.FlexDirection = source.FlexDirection;
        target.FlexWrap = source.FlexWrap;
        target.JustifyContent = source.JustifyContent;
        target.AlignItems = source.AlignItems;
        target.AlignSelf = source.AlignSelf;
        target.FlexGrow = source.FlexGrow;
        target.FlexShrink = source.FlexShrink;
        target.FlexBasis = source.FlexBasis;
        target.Top = source.Top;
        target.Right = source.Right;
        target.Bottom = source.Bottom;
        target.Left = source.Left;
        target.ZIndex = source.ZIndex;
        target.Overflow = source.Overflow;
        target.Gap = source.Gap;
        target.RowGap = source.RowGap;
        target.ColumnGap = source.ColumnGap;

        // Visual
        target.BackgroundColor = source.BackgroundColor;
        target.BorderColor = source.BorderColor;
        target.BorderRadius = source.BorderRadius;
        target.BorderCornerRadius = source.BorderCornerRadius;
        target.BorderStyle = source.BorderStyle;
        target.Opacity = source.Opacity;
        target.Visibility = source.Visibility;
        target.Cursor = source.Cursor;
        target.BoxShadow = source.BoxShadow;
        target.BackgroundImage = source.BackgroundImage;

        // CSS Custom Properties (only copy if source has any, to avoid lazy-init on target)
        if (source.HasCustomProperties)
        {
            foreach (var kvp in source.CustomProperties)
                target.CustomProperties[kvp.Key] = kvp.Value;
        }

        // Text
        target.Color = source.Color;
        target.FontFamily = source.FontFamily;
        target.FontSize = source.FontSize;
        target.FontWeight = source.FontWeight;
        target.FontStyle = source.FontStyle;
        target.LineHeight = source.LineHeight;
        target.TextAlign = source.TextAlign;
        target.LetterSpacing = source.LetterSpacing;
        target.TextDecoration = source.TextDecoration;
        target.TextTransform = source.TextTransform;
        target.WhiteSpace = source.WhiteSpace;
        target.TextOverflow = source.TextOverflow;
        target.WordBreak = source.WordBreak;

        // Transitions
        target.TransitionProperty = source.TransitionProperty;
        target.TransitionDuration = source.TransitionDuration;

        // Pointer events
        target.PointerEvents = source.PointerEvents;
    }
}
