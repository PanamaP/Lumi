using System.Globalization;
using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Styling;

/// <summary>
/// Maps CSS property name/value strings onto a <see cref="ComputedStyle"/>.
/// </summary>
public static class PropertyApplier
{
    public static void Apply(ComputedStyle style, string property, string value)
    {
        value = value.Trim();

        // Resolve var() references using the style's own custom properties
        value = ResolveVariables(value, style);

        // Store CSS custom properties (--name: value)
        if (property.StartsWith("--"))
        {
            style.CustomProperties[property] = value;
            return;
        }

        switch (property)
        {
            // --- Box model: sizing ---
            case "width":
                style.Width = ParseLength(value);
                break;
            case "height":
                style.Height = ParseLength(value);
                break;
            case "min-width":
                style.MinWidth = ParseLengthOrZero(value);
                break;
            case "max-width":
                style.MaxWidth = ParseLengthOrInfinity(value);
                break;
            case "min-height":
                style.MinHeight = ParseLengthOrZero(value);
                break;
            case "max-height":
                style.MaxHeight = ParseLengthOrInfinity(value);
                break;

            // --- Margin ---
            case "margin":
                style.Margin = ParseEdgeValues(value);
                break;
            case "margin-top":
                style.Margin = style.Margin with { Top = ParseLengthOrZero(value) };
                break;
            case "margin-right":
                style.Margin = style.Margin with { Right = ParseLengthOrZero(value) };
                break;
            case "margin-bottom":
                style.Margin = style.Margin with { Bottom = ParseLengthOrZero(value) };
                break;
            case "margin-left":
                style.Margin = style.Margin with { Left = ParseLengthOrZero(value) };
                break;

            // --- Padding ---
            case "padding":
                style.Padding = ParseEdgeValues(value);
                break;
            case "padding-top":
                style.Padding = style.Padding with { Top = ParseLengthOrZero(value) };
                break;
            case "padding-right":
                style.Padding = style.Padding with { Right = ParseLengthOrZero(value) };
                break;
            case "padding-bottom":
                style.Padding = style.Padding with { Bottom = ParseLengthOrZero(value) };
                break;
            case "padding-left":
                style.Padding = style.Padding with { Left = ParseLengthOrZero(value) };
                break;

            // --- Border ---
            case "border-width":
                style.BorderWidth = ParseEdgeValues(value);
                break;
            case "border-top-width":
                style.BorderWidth = style.BorderWidth with { Top = ParseLengthOrZero(value) };
                break;
            case "border-right-width":
                style.BorderWidth = style.BorderWidth with { Right = ParseLengthOrZero(value) };
                break;
            case "border-bottom-width":
                style.BorderWidth = style.BorderWidth with { Bottom = ParseLengthOrZero(value) };
                break;
            case "border-left-width":
                style.BorderWidth = style.BorderWidth with { Left = ParseLengthOrZero(value) };
                break;
            case "border-color":
            case "border-top-color":
            case "border-right-color":
            case "border-bottom-color":
            case "border-left-color":
                style.BorderColor = ParseColor(value);
                break;
            case "border-radius":
                var radii = ParseCornerRadius(value);
                style.BorderRadius = radii.TopLeft; // backward compat for uniform
                style.BorderCornerRadius = radii;
                break;
            case "border-top-left-radius":
                style.BorderCornerRadius = style.BorderCornerRadius with { TopLeft = ParseLengthOrZero(value.Split(' ')[0]) };
                style.BorderRadius = style.BorderCornerRadius.TopLeft;
                break;
            case "border-top-right-radius":
                style.BorderCornerRadius = style.BorderCornerRadius with { TopRight = ParseLengthOrZero(value.Split(' ')[0]) };
                break;
            case "border-bottom-right-radius":
                style.BorderCornerRadius = style.BorderCornerRadius with { BottomRight = ParseLengthOrZero(value.Split(' ')[0]) };
                break;
            case "border-bottom-left-radius":
                style.BorderCornerRadius = style.BorderCornerRadius with { BottomLeft = ParseLengthOrZero(value.Split(' ')[0]) };
                break;
            case "border-style":
            case "border-top-style":
            case "border-right-style":
            case "border-bottom-style":
            case "border-left-style":
                style.BorderStyle = value switch
                {
                    "none" => BorderStyle.None,
                    "solid" => BorderStyle.Solid,
                    "dashed" => BorderStyle.Dashed,
                    "dotted" => BorderStyle.Dotted,
                    "double" => BorderStyle.Double,
                    _ => style.BorderStyle
                };
                break;

            // --- Box sizing ---
            case "box-sizing":
                style.BoxSizing = value switch
                {
                    "content-box" => BoxSizing.ContentBox,
                    "border-box" => BoxSizing.BorderBox,
                    _ => style.BoxSizing
                };
                break;

            // --- Display ---
            case "display":
                style.Display = value switch
                {
                    "block" => DisplayMode.Block,
                    "flex" => DisplayMode.Flex,
                    "grid" => DisplayMode.Grid,
                    "none" => DisplayMode.None,
                    _ => style.Display
                };
                break;

            // --- Position ---
            case "position":
                style.Position = value switch
                {
                    "relative" => Position.Relative,
                    "absolute" => Position.Absolute,
                    "fixed" => Position.Fixed,
                    _ => style.Position
                };
                break;

            // --- Flex layout ---
            case "flex-direction":
                style.FlexDirection = value switch
                {
                    "row" => FlexDirection.Row,
                    "row-reverse" => FlexDirection.RowReverse,
                    "column" => FlexDirection.Column,
                    "column-reverse" => FlexDirection.ColumnReverse,
                    _ => style.FlexDirection
                };
                break;
            case "flex-wrap":
                style.FlexWrap = value switch
                {
                    "nowrap" => FlexWrap.NoWrap,
                    "wrap" => FlexWrap.Wrap,
                    "wrap-reverse" => FlexWrap.WrapReverse,
                    _ => style.FlexWrap
                };
                break;
            case "justify-content":
                style.JustifyContent = value switch
                {
                    "flex-start" => JustifyContent.FlexStart,
                    "flex-end" => JustifyContent.FlexEnd,
                    "center" => JustifyContent.Center,
                    "space-between" => JustifyContent.SpaceBetween,
                    "space-around" => JustifyContent.SpaceAround,
                    "space-evenly" => JustifyContent.SpaceEvenly,
                    _ => style.JustifyContent
                };
                break;
            case "align-items":
                style.AlignItems = value switch
                {
                    "flex-start" => AlignItems.FlexStart,
                    "flex-end" => AlignItems.FlexEnd,
                    "center" => AlignItems.Center,
                    "stretch" => AlignItems.Stretch,
                    "baseline" => AlignItems.Baseline,
                    _ => style.AlignItems
                };
                break;
            case "align-self":
                style.AlignSelf = value switch
                {
                    "flex-start" => AlignItems.FlexStart,
                    "flex-end" => AlignItems.FlexEnd,
                    "center" => AlignItems.Center,
                    "stretch" => AlignItems.Stretch,
                    "baseline" => AlignItems.Baseline,
                    _ => style.AlignSelf
                };
                break;
            case "flex-grow":
                style.FlexGrow = ParseFloat(value, 0);
                break;
            case "flex-shrink":
                style.FlexShrink = ParseFloat(value, 1);
                break;
            case "flex-basis":
                style.FlexBasis = ParseLength(value);
                break;

            // --- Gap ---
            case "gap":
                style.Gap = ParseLengthOrZero(value);
                break;
            case "row-gap":
                style.RowGap = ParseLengthOrZero(value);
                break;
            case "column-gap":
                style.ColumnGap = ParseLengthOrZero(value);
                break;

            // --- Grid layout ---
            case "grid-template-columns":
                style.GridTemplateColumns = value;
                break;
            case "grid-template-rows":
                style.GridTemplateRows = value;
                break;
            case "grid-gap":
                style.GridGap = ParseLengthOrZero(value);
                break;

            // --- Offsets ---
            case "top":
                style.Top = ParseLength(value);
                break;
            case "right":
                style.Right = ParseLength(value);
                break;
            case "bottom":
                style.Bottom = ParseLength(value);
                break;
            case "left":
                style.Left = ParseLength(value);
                break;

            // --- Z-index ---
            case "z-index":
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var z))
                    style.ZIndex = z;
                break;

            // --- Overflow ---
            case "overflow":
            case "overflow-x":
            case "overflow-y":
                style.Overflow = value switch
                {
                    "visible" => Overflow.Visible,
                    "hidden" => Overflow.Hidden,
                    "scroll" => Overflow.Scroll,
                    _ => style.Overflow
                };
                break;

            // --- Visual ---
            case "background-color":
                style.BackgroundColor = ParseColor(value);
                break;
            case "opacity":
                style.Opacity = ParseFloat(value, 1);
                break;
            case "visibility":
                style.Visibility = value switch
                {
                    "visible" => Visibility.Visible,
                    "hidden" => Visibility.Hidden,
                    _ => style.Visibility
                };
                break;

            // --- Text / Color ---
            case "color":
                style.Color = ParseColor(value);
                break;
            case "font-family":
                style.FontFamily = value.Trim('"', '\'', ' ');
                break;
            case "font-size":
                style.FontSize = ParseLengthOrZero(value);
                break;
            case "font-weight":
                style.FontWeight = ParseFontWeight(value);
                break;
            case "font-style":
                style.FontStyle = value switch
                {
                    "italic" => FontStyle.Italic,
                    "normal" => FontStyle.Normal,
                    _ => style.FontStyle
                };
                break;
            case "line-height":
                style.LineHeight = ParseFloat(value, 1.2f);
                break;
            case "text-align":
                style.TextAlign = value switch
                {
                    "left" => TextAlign.Left,
                    "right" => TextAlign.Right,
                    "center" => TextAlign.Center,
                    _ => style.TextAlign
                };
                break;
            case "letter-spacing":
                style.LetterSpacing = ParseLengthOrZero(value);
                break;
            case "white-space":
                style.WhiteSpace = value switch
                {
                    "normal" => WhiteSpace.Normal,
                    "nowrap" => WhiteSpace.NoWrap,
                    "pre" => WhiteSpace.Pre,
                    _ => style.WhiteSpace
                };
                break;
            case "text-overflow":
                style.TextOverflow = value switch
                {
                    "clip" => TextOverflow.Clip,
                    "ellipsis" => TextOverflow.Ellipsis,
                    _ => style.TextOverflow
                };
                break;
            case "word-break":
                style.WordBreak = value switch
                {
                    "normal" => WordBreak.Normal,
                    "break-all" => WordBreak.BreakAll,
                    _ => style.WordBreak
                };
                break;
            case "text-decoration":
            case "text-decoration-line":
                style.TextDecoration = value switch
                {
                    "none" => TextDecoration.None,
                    "underline" => TextDecoration.Underline,
                    "line-through" => TextDecoration.LineThrough,
                    _ => style.TextDecoration
                };
                break;
            case "text-transform":
                style.TextTransform = value switch
                {
                    "none" => TextTransform.None,
                    "uppercase" => TextTransform.Uppercase,
                    "lowercase" => TextTransform.Lowercase,
                    "capitalize" => TextTransform.Capitalize,
                    _ => style.TextTransform
                };
                break;

            // --- Cursor ---
            case "cursor":
                style.Cursor = value;
                break;

            // --- Pointer events ---
            case "pointer-events":
                style.PointerEvents = value != "none";
                break;

            // --- Transitions ---
            case "transition-property":
                style.TransitionProperty = value;
                break;
            case "transition-duration":
                style.TransitionDuration = ParseDuration(value);
                break;
            case "transition-timing-function":
                style.TransitionTimingFunction = value;
                break;

            // --- Animation ---
            case "animation-name":
                style.AnimationName = value;
                break;
            case "animation-duration":
                style.AnimationDuration = ParseDuration(value);
                break;
            case "animation-delay":
                style.AnimationDelay = ParseDuration(value);
                break;
            case "animation-iteration-count":
                style.AnimationIterationCount = value == "infinite" ? -1 : (int)ParseFloat(value, 1);
                break;
            case "animation-direction":
                style.AnimationDirection = ParseAnimationDirection(value);
                break;
            case "animation-fill-mode":
                style.AnimationFillMode = ParseAnimationFillMode(value);
                break;
            case "animation-timing-function":
                style.AnimationTimingFunction = value;
                break;
            case "animation":
                ParseAnimationShorthand(style, value);
                break;

            // --- Box shadow ---
            case "box-shadow":
                style.BoxShadow = ParseBoxShadow(value);
                break;

            // --- Background image ---
            case "background-image":
                if (IsGradient(value))
                {
                    style.BackgroundGradient = ParseGradient(value);
                    style.BackgroundImage = null;
                }
                else
                {
                    style.BackgroundImage = ParseUrl(value);
                    style.BackgroundGradient = null;
                }
                break;

            // --- Shorthands ---
            case "border":
                ParseBorderShorthand(style, value);
                break;
            case "flex":
                ParseFlexShorthand(style, value);
                break;
            case "background":
                ParseBackgroundShorthand(style, value);
                break;
            case "font":
                ParseFontShorthand(style, value);
                break;

            // --- Transform ---
            case "transform":
                style.Transform = ParseTransform(value);
                break;
            case "transform-origin":
                ParseTransformOrigin(style, value);
                break;
        }
    }

    // ── Value parsers ───────────────────────────────────────────────────

    /// <summary>
    /// Parses a CSS length value. Returns NaN for "auto".
    /// </summary>
    /// <summary>
    /// Resolves a CSS length value to pixels, handling unit conversion.
    /// Percentages are encoded as negative values for Yoga's percent APIs.
    /// </summary>
    private static float ResolveLength(string value, float fontSize, float fallback)
    {
        // Handle calc() expressions
        if (value.StartsWith("calc(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var expr = value[5..^1].Trim();
            return CalcExpression.Evaluate(expr, fontSize, _viewportWidth, _viewportHeight, fallback);
        }

        if (value.EndsWith('%'))
            return -ParseFloat(value[..^1], fallback);

        if (value.EndsWith("vmin", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^4], fallback) * Math.Min(_viewportWidth, _viewportHeight) / 100f;

        if (value.EndsWith("vmax", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^4], fallback) * Math.Max(_viewportWidth, _viewportHeight) / 100f;

        if (value.EndsWith("vh", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^2], fallback) * _viewportHeight / 100f;

        if (value.EndsWith("vw", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^2], fallback) * _viewportWidth / 100f;

        if (value.EndsWith("rem", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^3], fallback) * RootFontSize;

        if (value.EndsWith("em", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^2], fallback) * fontSize;

        if (value.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^2], fallback) * PtToPx;

        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^2], fallback);

        return ParseFloat(value, fallback);
    }

    // 1pt = 96/72 px (CSS spec: 96 DPI screen, 72 points per inch)
    private const float PtToPx = 96f / 72f;

    // Default root font size (browser default)
    private const float RootFontSize = 16f;

    private static float ParseLength(string value)
    {
        if (value is "auto" or "initial")
            return float.NaN;

        return ResolveLength(value, _currentFontSize, float.NaN);
    }

    private static float ParseLengthOrZero(string value)
    {
        if (value is "auto" or "initial" or "0")
            return 0;

        return ResolveLength(value, _currentFontSize, 0);
    }

    private static float ParseLengthOrInfinity(string value)
    {
        if (value is "none" or "auto" or "initial")
            return float.PositiveInfinity;

        return ResolveLength(value, _currentFontSize, float.PositiveInfinity);
    }

    // Font size context for resolving em units — set before applying properties
    [ThreadStatic] private static float _currentFontSize;

    // Viewport dimensions for vh/vw units
    [ThreadStatic] private static float _viewportWidth;
    [ThreadStatic] private static float _viewportHeight;

    /// <summary>
    /// Sets the font-size context used to resolve em units.
    /// Call this before Apply() with the element's inherited/computed font size.
    /// </summary>
    public static void SetFontSizeContext(float fontSize)
    {
        _currentFontSize = fontSize > 0 ? fontSize : RootFontSize;
    }

    /// <summary>
    /// Sets the viewport dimensions used to resolve vh/vw/vmin/vmax units.
    /// Call once per frame before style resolution.
    /// </summary>
    public static void SetViewportContext(float width, float height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
    }

    private static string StripUnit(string value)
    {
        foreach (var unit in new[] { "px", "em", "rem", "pt", "%" })
        {
            if (value.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                return value[..^unit.Length];
        }
        return value;
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
    }

    private static EdgeValues ParseEdgeValues(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            1 => new EdgeValues(ParseLengthOrZero(parts[0])),
            2 => new EdgeValues(ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1])),
            3 => new EdgeValues(ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1]),
                                ParseLengthOrZero(parts[2]), ParseLengthOrZero(parts[1])),
            4 => new EdgeValues(ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1]),
                                ParseLengthOrZero(parts[2]), ParseLengthOrZero(parts[3])),
            _ => EdgeValues.Zero
        };
    }

    /// <summary>
    /// Parse border-radius shorthand: 1-4 values for TL TR BR BL corners.
    /// CSS order: top-left, top-right, bottom-right, bottom-left.
    /// </summary>
    private static CornerRadius ParseCornerRadius(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            1 => new CornerRadius(ParseLengthOrZero(parts[0])),
            2 => new CornerRadius(
                ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1]),
                ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1])),
            3 => new CornerRadius(
                ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1]),
                ParseLengthOrZero(parts[2]), ParseLengthOrZero(parts[1])),
            4 => new CornerRadius(
                ParseLengthOrZero(parts[0]), ParseLengthOrZero(parts[1]),
                ParseLengthOrZero(parts[2]), ParseLengthOrZero(parts[3])),
            _ => new CornerRadius(0)
        };
    }

    private static int ParseFontWeight(string value)
    {
        return value switch
        {
            "normal" => 400,
            "bold" => 700,
            "lighter" => 300,
            "bolder" => 700,
            _ => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) ? w : 400
        };
    }

    private static float ParseDuration(string value)
    {
        if (value.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^2], 0) / 1000f;

        if (value.EndsWith('s'))
            return ParseFloat(value[..^1], 0);

        return ParseFloat(value, 0);
    }

    internal static Color ParseColor(string value)
    {
        if (value.StartsWith('#'))
            return Color.FromHex(value);

        // Handle rgb(r, g, b) and rgba(r, g, b, a)
        if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            var inner = value;
            var start = inner.IndexOf('(');
            var end = inner.LastIndexOf(')');
            if (start >= 0 && end > start)
            {
                var parts = inner[(start + 1)..end]
                    .Split(',', StringSplitOptions.TrimEntries);

                if (parts.Length >= 3
                    && byte.TryParse(parts[0], out var r)
                    && byte.TryParse(parts[1], out var g)
                    && byte.TryParse(parts[2], out var b))
                {
                    byte a = 255;
                    if (parts.Length >= 4 && float.TryParse(parts[3], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var af))
                    {
                        a = (byte)(af <= 1f ? af * 255 : af);
                    }
                    return new Color(r, g, b, a);
                }
            }
        }

        // Handle hsl(h, s%, l%) and hsla(h, s%, l%, a)
        if (value.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            var start = value.IndexOf('(');
            var end = value.LastIndexOf(')');
            if (start >= 0 && end > start)
            {
                var parts = value[(start + 1)..end]
                    .Split(',', StringSplitOptions.TrimEntries);

                if (parts.Length >= 3
                    && float.TryParse(parts[0].TrimEnd('°'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var h)
                    && float.TryParse(parts[1].TrimEnd('%'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var s)
                    && float.TryParse(parts[2].TrimEnd('%'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var l))
                {
                    byte a = 255;
                    if (parts.Length >= 4 && float.TryParse(parts[3].TrimEnd('%'), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var af))
                    {
                        a = (byte)(af <= 1f ? af * 255 : af);
                    }
                    return CssColors.FromHsl(h, s / 100f, l / 100f, a);
                }
            }
        }

        // Try all 147 CSS named colors
        if (CssColors.TryGet(value, out var namedColor))
            return namedColor;

        return Color.Black;
    }

    private static BoxShadow ParseBoxShadow(string value)
    {
        if (value is "none" or "initial")
            return BoxShadow.None;

        // Format: offsetX offsetY [blur [spread]] color [inset]
        // Color can be a named color, hex, or rgba(...) which contains spaces
        bool inset = false;
        if (value.EndsWith("inset", StringComparison.OrdinalIgnoreCase))
        {
            inset = true;
            value = value[..^5].TrimEnd();
        }
        else if (value.StartsWith("inset", StringComparison.OrdinalIgnoreCase))
        {
            inset = true;
            value = value[5..].TrimStart();
        }

        // Extract color portion: look for rgb/rgba(...) or the last token
        string colorStr;
        string lengthPart;

        int rgbIndex = value.IndexOf("rgb", StringComparison.OrdinalIgnoreCase);
        if (rgbIndex >= 0)
        {
            int parenClose = value.IndexOf(')', rgbIndex);
            if (parenClose >= 0)
            {
                colorStr = value[rgbIndex..(parenClose + 1)];
                lengthPart = value[..rgbIndex].Trim();
            }
            else
            {
                return BoxShadow.None;
            }
        }
        else
        {
            // Color is the last token (hex or named)
            var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3)
                return BoxShadow.None;
            colorStr = tokens[^1];
            lengthPart = string.Join(' ', tokens[..^1]);
        }

        var lengths = lengthPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lengths.Length < 2)
            return BoxShadow.None;

        float offsetX = ParseLengthOrZero(lengths[0]);
        float offsetY = ParseLengthOrZero(lengths[1]);
        float blur = lengths.Length > 2 ? ParseLengthOrZero(lengths[2]) : 0;
        float spread = lengths.Length > 3 ? ParseLengthOrZero(lengths[3]) : 0;

        return new BoxShadow(offsetX, offsetY, blur, spread, ParseColor(colorStr), inset);
    }

    private static string? ParseUrl(string value)
    {
        if (value is "none" or "initial")
            return null;

        // url('path') or url("path") or url(path)
        if (value.StartsWith("url(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var inner = value[4..^1].Trim().Trim('"', '\'');
            return string.IsNullOrWhiteSpace(inner) ? null : inner;
        }

        return null;
    }

    private static string ResolveVariables(string value, ComputedStyle style, HashSet<string>? resolving = null)
    {
        // Fast path: no var() references
        if (!value.Contains("var(", StringComparison.OrdinalIgnoreCase))
            return value;

        int safety = 10; // prevent infinite loops
        while (value.Contains("var(", StringComparison.OrdinalIgnoreCase) && safety-- > 0)
        {
            int start = value.IndexOf("var(", StringComparison.OrdinalIgnoreCase);
            if (start < 0) break;

            // Find matching closing paren (handle nested parens)
            int depth = 0;
            int end = -1;
            for (int i = start + 4; i < value.Length; i++)
            {
                if (value[i] == '(') depth++;
                else if (value[i] == ')')
                {
                    if (depth == 0) { end = i; break; }
                    depth--;
                }
            }

            if (end < 0) break;

            string varExpr = value[(start + 4)..end];

            // Split on first comma for fallback: var(--name, fallback)
            string varName;
            string? fallback = null;
            int commaIdx = varExpr.IndexOf(',');
            if (commaIdx >= 0)
            {
                varName = varExpr[..commaIdx].Trim();
                fallback = varExpr[(commaIdx + 1)..].Trim();
            }
            else
            {
                varName = varExpr.Trim();
            }

            string resolved;
            resolving ??= new HashSet<string>(StringComparer.Ordinal);

            if (resolving.Contains(varName))
            {
                // Circular reference detected — use fallback or empty string
                resolved = fallback ?? "";
            }
            else if (style.CustomProperties.TryGetValue(varName, out var propValue))
            {
                resolving.Add(varName);
                resolved = ResolveVariables(propValue, style, resolving);
                resolving.Remove(varName);
            }
            else
            {
                resolved = fallback ?? "";
            }

            value = string.Concat(value.AsSpan(0, start), resolved, value.AsSpan(end + 1));
        }

        return value;
    }

    // ── Shorthand parsers ────────────────────────────────────────────────

    /// <summary>
    /// Parse "border: [width] [style] [color]" shorthand.
    /// Any of the three components may be omitted in any order.
    /// </summary>
    private static void ParseBorderShorthand(ComputedStyle style, string value)
    {
        if (value is "none" or "initial")
        {
            style.BorderWidth = EdgeValues.Zero;
            style.BorderStyle = BorderStyle.None;
            return;
        }

        // Split tokens, but keep rgb()/rgba() together
        var tokens = SplitCssTokens(value);
        float? width = null;
        BorderStyle? parsedStyle = null;
        string? colorToken = null;

        foreach (var token in tokens)
        {
            // Check for border-style keywords first
            var bs = token switch
            {
                "solid" => (BorderStyle?)BorderStyle.Solid,
                "dashed" => (BorderStyle?)BorderStyle.Dashed,
                "dotted" => (BorderStyle?)BorderStyle.Dotted,
                "double" => (BorderStyle?)BorderStyle.Double,
                "none" => (BorderStyle?)BorderStyle.None,
                _ => null
            };

            if (bs.HasValue)
            {
                parsedStyle = bs.Value;
                continue;
            }

            // Try as a length (width)
            if (width == null)
            {
                var w = ParseLengthOrZero(token);
                if (w > 0 || token == "0" || token == "0px")
                {
                    width = w;
                    continue;
                }
            }

            // Otherwise treat as color
            colorToken ??= token;
        }

        if (width.HasValue)
            style.BorderWidth = new EdgeValues(width.Value);
        if (parsedStyle.HasValue)
            style.BorderStyle = parsedStyle.Value;
        if (colorToken != null)
            style.BorderColor = ParseColor(colorToken);
    }

    /// <summary>
    /// Parse "flex: [grow] [shrink] [basis]" shorthand.
    /// Supports: flex: none | auto | initial | &lt;number&gt; | &lt;number&gt; &lt;number&gt; | &lt;number&gt; &lt;number&gt; &lt;basis&gt;
    /// </summary>
    private static void ParseFlexShorthand(ComputedStyle style, string value)
    {
        switch (value)
        {
            case "none":
                style.FlexGrow = 0;
                style.FlexShrink = 0;
                style.FlexBasis = float.NaN; // auto
                return;
            case "auto":
                style.FlexGrow = 1;
                style.FlexShrink = 1;
                style.FlexBasis = float.NaN; // auto
                return;
            case "initial":
                style.FlexGrow = 0;
                style.FlexShrink = 1;
                style.FlexBasis = float.NaN; // auto
                return;
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            // Single number: flex: <grow> → grow=N, shrink=1, basis=0
            style.FlexGrow = ParseFloat(parts[0], 0);
            style.FlexShrink = 1;
            style.FlexBasis = 0;
        }
        else if (parts.Length == 2)
        {
            style.FlexGrow = ParseFloat(parts[0], 0);
            // Second value could be shrink (number) or basis (has unit)
            if (HasUnit(parts[1]))
            {
                style.FlexShrink = 1;
                style.FlexBasis = ParseLength(parts[1]);
            }
            else
            {
                style.FlexShrink = ParseFloat(parts[1], 1);
                style.FlexBasis = 0;
            }
        }
        else if (parts.Length >= 3)
        {
            style.FlexGrow = ParseFloat(parts[0], 0);
            style.FlexShrink = ParseFloat(parts[1], 1);
            style.FlexBasis = ParseLength(parts[2]);
        }
    }

    /// <summary>
    /// Parse "background: [color] [url] [gradient] ..." shorthand.
    /// </summary>
    private static void ParseBackgroundShorthand(ComputedStyle style, string value)
    {
        if (value is "none" or "initial")
        {
            style.BackgroundColor = Color.Transparent;
            style.BackgroundImage = null;
            style.BackgroundGradient = null;
            return;
        }

        if (IsGradient(value))
        {
            style.BackgroundGradient = ParseGradient(value);
            style.BackgroundImage = null;
            return;
        }

        // Extract url() if present
        int urlStart = value.IndexOf("url(", StringComparison.OrdinalIgnoreCase);
        if (urlStart >= 0)
        {
            int urlEnd = value.IndexOf(')', urlStart);
            if (urlEnd >= 0)
            {
                style.BackgroundImage = ParseUrl(value[urlStart..(urlEnd + 1)]);
                style.BackgroundGradient = null;
                string rest = (value[..urlStart] + value[(urlEnd + 1)..]).Trim();
                if (rest.Length > 0)
                    style.BackgroundColor = ParseColor(rest);
                return;
            }
        }

        // No url() — treat entire value as color
        style.BackgroundColor = ParseColor(value);
        style.BackgroundGradient = null;
        style.BackgroundImage = null;
    }

    /// <summary>
    /// Parse "font: [style] [weight] size[/line-height] family" shorthand.
    /// Size and family are required; style and weight are optional.
    /// </summary>
    private static void ParseFontShorthand(ComputedStyle style, string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        int i = 0;

        // Optional font-style
        if (i < parts.Length && parts[i] is "italic" or "oblique")
        {
            style.FontStyle = FontStyle.Italic;
            i++;
        }
        else if (i < parts.Length && parts[i] == "normal")
        {
            // "normal" could be style or weight — skip and treat as default
            i++;
        }

        // Optional font-weight
        if (i < parts.Length && IsFontWeight(parts[i]))
        {
            style.FontWeight = ParseFontWeight(parts[i]);
            i++;
        }

        // Required: font-size (possibly with /line-height)
        if (i < parts.Length)
        {
            string sizePart = parts[i];
            int slashIdx = sizePart.IndexOf('/');
            if (slashIdx >= 0)
            {
                style.FontSize = ParseLengthOrZero(sizePart[..slashIdx]);
                style.LineHeight = ParseFloat(sizePart[(slashIdx + 1)..], 1.2f);
            }
            else
            {
                style.FontSize = ParseLengthOrZero(sizePart);
            }
            i++;
        }

        // Remaining parts: font-family (may contain spaces for multi-word names)
        if (i < parts.Length)
        {
            style.FontFamily = string.Join(' ', parts[i..]).Trim('"', '\'', ' ');
        }
    }

    private static bool IsFontWeight(string value) =>
        value is "bold" or "bolder" or "lighter" or "normal"
        || (int.TryParse(value, out var w) && w >= 100 && w <= 900);

    private static bool HasUnit(string value) =>
        value.EndsWith("px", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith("em", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith("rem", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith("pt", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith('%') ||
        value is "auto";

    /// <summary>
    /// Split CSS value tokens while keeping rgb()/rgba()/hsl()/hsla() together.
    /// </summary>
    private static List<string> SplitCssTokens(string value)
    {
        var tokens = new List<string>();
        int start = 0;
        int depth = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ' ' && depth == 0)
            {
                if (i > start)
                    tokens.Add(value[start..i]);
                start = i + 1;
            }
        }

        if (start < value.Length)
            tokens.Add(value[start..]);

        return tokens;
    }

    // ── Transform parsers ────────────────────────────────────────────────

    /// <summary>
    /// Parse CSS transform: "translate(10px, 20px) scale(1.5) rotate(45deg) skew(10deg, 5deg)"
    /// </summary>
    internal static CssTransform ParseTransform(string value)
    {
        if (value is "none" or "initial")
            return CssTransform.Identity;

        float tx = 0, ty = 0, sx = 1, sy = 1, rot = 0, skx = 0, sky = 0;

        int i = 0;
        while (i < value.Length)
        {
            // Skip whitespace
            while (i < value.Length && value[i] == ' ') i++;
            if (i >= value.Length) break;

            // Find function name
            int nameStart = i;
            while (i < value.Length && value[i] != '(') i++;
            if (i >= value.Length) break;
            string funcName = value[nameStart..i].Trim().ToLowerInvariant();
            i++; // skip '('

            // Find matching closing paren (handles nested parens in calc(), etc.)
            int parenEnd = FindMatchingParen(value, i);
            if (parenEnd < 0) break;
            string args = value[i..parenEnd].Trim();
            i = parenEnd + 1;

            var parts = args.Split(',', StringSplitOptions.TrimEntries);

            switch (funcName)
            {
                case "translate":
                    tx += ParseLengthOrZero(parts[0]);
                    if (parts.Length > 1) ty += ParseLengthOrZero(parts[1]);
                    break;
                case "translatex":
                    tx += ParseLengthOrZero(parts[0]);
                    break;
                case "translatey":
                    ty += ParseLengthOrZero(parts[0]);
                    break;
                case "scale":
                    float s1 = ParseFloat(parts[0], 1);
                    float s2 = parts.Length > 1 ? ParseFloat(parts[1], s1) : s1;
                    sx *= s1;
                    sy *= s2;
                    break;
                case "scalex":
                    sx *= ParseFloat(parts[0], 1);
                    break;
                case "scaley":
                    sy *= ParseFloat(parts[0], 1);
                    break;
                case "rotate":
                    rot += ParseAngle(parts[0]);
                    break;
                case "skew":
                    skx += ParseAngle(parts[0]);
                    if (parts.Length > 1) sky += ParseAngle(parts[1]);
                    break;
                case "skewx":
                    skx += ParseAngle(parts[0]);
                    break;
                case "skewy":
                    sky += ParseAngle(parts[0]);
                    break;
            }
        }

        return new CssTransform(tx, ty, sx, sy, rot, skx, sky);
    }

    private static float ParseAngle(string value)
    {
        value = value.Trim();
        if (value.EndsWith("deg", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^3], 0);
        if (value.EndsWith("rad", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^3], 0) * (180f / MathF.PI);
        if (value.EndsWith("turn", StringComparison.OrdinalIgnoreCase))
            return ParseFloat(value[..^4], 0) * 360f;
        return ParseFloat(value, 0); // assume degrees
    }

    private static void ParseTransformOrigin(ComputedStyle style, string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1)
            style.TransformOriginX = ParseOriginComponent(parts[0]);
        if (parts.Length >= 2)
            style.TransformOriginY = ParseOriginComponent(parts[1]);
    }

    private static float ParseOriginComponent(string value) => value.ToLowerInvariant() switch
    {
        "left" or "top" => 0,
        "center" => 50,
        "right" or "bottom" => 100,
        _ when value.EndsWith('%') => ParseFloat(value[..^1], 50),
        _ => ParseLengthOrZero(value) // absolute px value
    };

    private static AnimationDirection ParseAnimationDirection(string value) => value.Trim().ToLowerInvariant() switch
    {
        "reverse" => AnimationDirection.Reverse,
        "alternate" => AnimationDirection.Alternate,
        "alternate-reverse" => AnimationDirection.AlternateReverse,
        _ => AnimationDirection.Normal
    };

    private static AnimationFillMode ParseAnimationFillMode(string value) => value.Trim().ToLowerInvariant() switch
    {
        "forwards" => AnimationFillMode.Forwards,
        "backwards" => AnimationFillMode.Backwards,
        "both" => AnimationFillMode.Both,
        _ => AnimationFillMode.None
    };

    /// <summary>
    /// Parse `animation: name duration timing-function delay iteration-count direction fill-mode`
    /// e.g. `animation: spin 1s ease-in-out infinite` or `animation: fadeIn 0.3s`
    /// </summary>
    internal static void ParseAnimationShorthand(ComputedStyle style, string value)
    {
        if (value is "none" or "initial" or "inherit")
        {
            style.AnimationName = null;
            return;
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool durationSet = false;

        foreach (var part in parts)
        {
            var p = part.Trim();

            // Duration or delay (first time value = duration, second = delay)
            if (p.EndsWith('s') && float.TryParse(p.EndsWith("ms", StringComparison.Ordinal) ? p[..^2] : p[..^1],
                NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                if (!durationSet)
                {
                    style.AnimationDuration = ParseDuration(p);
                    durationSet = true;
                }
                else
                {
                    style.AnimationDelay = ParseDuration(p);
                }
                continue;
            }

            // Iteration count
            if (p == "infinite") { style.AnimationIterationCount = -1; continue; }
            if (int.TryParse(p, out int ic) && ic >= 0) { style.AnimationIterationCount = ic; continue; }

            // Direction
            if (p is "normal" or "reverse" or "alternate" or "alternate-reverse")
            { style.AnimationDirection = ParseAnimationDirection(p); continue; }

            // Fill mode
            if (p is "forwards" or "backwards" or "both")
            { style.AnimationFillMode = ParseAnimationFillMode(p); continue; }

            // Timing function
            if (p is "ease" or "ease-in" or "ease-out" or "ease-in-out" or "linear" or "step-start" or "step-end")
            { style.AnimationTimingFunction = p; continue; }

            // Anything else is the animation name
            style.AnimationName = p;
        }
    }

    // ── Gradient parsers ─────────────────────────────────────────────────

    private static bool IsGradient(string value) =>
        value.StartsWith("linear-gradient(", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("radial-gradient(", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses a CSS gradient function string into a CssGradient object.
    /// </summary>
    /// <returns>The parsed gradient, or <c>null</c> if the input is not a valid gradient function.</returns>
    internal static CssGradient? ParseGradient(string value)
    {
        if (value.StartsWith("linear-gradient(", StringComparison.OrdinalIgnoreCase))
            return ParseLinearGradient(value);
        if (value.StartsWith("radial-gradient(", StringComparison.OrdinalIgnoreCase))
            return ParseRadialGradient(value);
        return null;
    }

    private static CssGradient ParseLinearGradient(string value)
    {
        var inner = ExtractFunctionArgs(value, "linear-gradient");
        var args = SplitGradientArgs(inner);

        float angle = 180; // default: to bottom
        int stopStart = 0;

        if (args.Count > 0)
        {
            var first = args[0].Trim();
            if (TryParseGradientDirection(first, out float parsedAngle))
            {
                angle = parsedAngle;
                stopStart = 1;
            }
        }

        var stops = ParseGradientStops(args, stopStart);
        stops.Sort((a, b) => a.Position.CompareTo(b.Position));

        return new CssGradient
        {
            Type = GradientType.Linear,
            Angle = angle,
            Stops = stops
        };
    }

    private static CssGradient ParseRadialGradient(string value)
    {
        var inner = ExtractFunctionArgs(value, "radial-gradient");
        var args = SplitGradientArgs(inner);

        int stopStart = 0;

        if (args.Count > 0)
        {
            var first = args[0].Trim().ToLowerInvariant();
            if (first is "circle" or "ellipse")
                stopStart = 1;
        }

        var stops = ParseGradientStops(args, stopStart);
        stops.Sort((a, b) => a.Position.CompareTo(b.Position));

        return new CssGradient
        {
            Type = GradientType.Radial,
            Angle = 0,
            Stops = stops
        };
    }

    private static string ExtractFunctionArgs(string value, string funcName)
    {
        int start = funcName.Length + 1;
        int end = value.LastIndexOf(')');
        if (end <= start) return "";
        return value[start..end];
    }

    private static List<string> SplitGradientArgs(string inner)
    {
        var args = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];
            if (c == '(') depth++;
            else if (c == ')' && depth > 0) depth--;
            else if (c == ',' && depth == 0)
            {
                args.Add(inner[start..i].Trim());
                start = i + 1;
            }
        }

        if (start < inner.Length)
            args.Add(inner[start..].Trim());

        return args;
    }

    private static bool TryParseGradientDirection(string arg, out float angle)
    {
        angle = 180;
        var lower = arg.ToLowerInvariant().Trim();

        if (lower.EndsWith("deg") || lower.EndsWith("rad") || lower.EndsWith("turn"))
        {
            angle = ParseAngle(lower);
            return true;
        }

        if (lower.StartsWith("to "))
        {
            var dir = lower[3..].Trim();
            angle = dir switch
            {
                "top" => 0,
                "right" => 90,
                "bottom" => 180,
                "left" => 270,
                "top right" or "right top" => 45,
                "top left" or "left top" => 315,
                "bottom right" or "right bottom" => 135,
                "bottom left" or "left bottom" => 225,
                _ => 180
            };
            return true;
        }

        return false;
    }

    private static List<GradientStop> ParseGradientStops(List<string> args, int startIndex)
    {
        var stops = new List<GradientStop>();
        int count = args.Count - startIndex;
        if (count <= 0) return stops;

        for (int i = startIndex; i < args.Count; i++)
        {
            var arg = args[i].Trim();
            if (arg.Length == 0) continue;

            SplitColorAndPosition(arg, out var colorStr, out var posStr);

            var color = ParseColor(colorStr);
            float position;

            if (posStr != null)
            {
                position = ParseStopPosition(posStr);
            }
            else
            {
                int idx = i - startIndex;
                position = count <= 1 ? 0 : (float)idx / (count - 1);
            }

            stops.Add(new GradientStop(color, position));
        }

        return stops;
    }

    private static void SplitColorAndPosition(string arg, out string colorStr, out string? posStr)
    {
        posStr = null;

        int depth = 0;
        int lastSpace = -1;

        for (int i = 0; i < arg.Length; i++)
        {
            char c = arg[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ' ' && depth == 0) lastSpace = i;
        }

        if (lastSpace > 0)
        {
            var tail = arg[(lastSpace + 1)..].Trim();
            if (tail.EndsWith('%') || float.TryParse(tail, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out _))
            {
                colorStr = arg[..lastSpace].Trim();
                posStr = tail;
                return;
            }
        }

        colorStr = arg;
    }

    private static float ParseStopPosition(string pos)
    {
        if (pos.EndsWith('%'))
        {
            if (float.TryParse(pos[..^1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var pct))
                return pct / 100f;
        }
        else if (float.TryParse(pos, NumberStyles.Float,
                     CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }

        return 0;
    }

    /// <summary>
    /// Finds the matching closing parenthesis starting from position <paramref name="start"/>,
    /// accounting for nested parentheses.
    /// </summary>
    private static int FindMatchingParen(string s, int start)
    {
        int depth = 1;
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')' && --depth == 0) return i;
        }
        return -1;
    }
}
