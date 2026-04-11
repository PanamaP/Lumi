using Lumi.Core.Animation;

namespace Lumi.Core;

/// <summary>
/// Holds the fully resolved style values for an element (all values in pixels / concrete values).
/// </summary>
public class ComputedStyle
{
    // Box model
    public float Width { get; set; } = float.NaN;
    public float Height { get; set; } = float.NaN;
    public float MinWidth { get; set; } = 0;
    public float MaxWidth { get; set; } = float.PositiveInfinity;
    public float MinHeight { get; set; } = 0;
    public float MaxHeight { get; set; } = float.PositiveInfinity;

    public EdgeValues Margin { get; set; }
    public EdgeValues Padding { get; set; }
    public EdgeValues BorderWidth { get; set; }
    public BoxSizing BoxSizing { get; set; } = BoxSizing.BorderBox;

    // Layout
    public DisplayMode Display { get; set; } = DisplayMode.Block;
    public Position Position { get; set; } = Position.Relative;
    public FlexDirection FlexDirection { get; set; } = FlexDirection.Column;
    public FlexWrap FlexWrap { get; set; } = FlexWrap.NoWrap;
    public JustifyContent JustifyContent { get; set; } = JustifyContent.FlexStart;
    public AlignItems AlignItems { get; set; } = AlignItems.Stretch;
    public AlignItems AlignSelf { get; set; } = AlignItems.Stretch;
    public float FlexGrow { get; set; } = 0;
    public float FlexShrink { get; set; } = 1;
    public float FlexBasis { get; set; } = float.NaN;
    public float Top { get; set; } = float.NaN;
    public float Right { get; set; } = float.NaN;
    public float Bottom { get; set; } = float.NaN;
    public float Left { get; set; } = float.NaN;
    public int ZIndex { get; set; } = 0;
    public Overflow Overflow { get; set; } = Overflow.Visible;
    public float Gap { get; set; } = 0;
    public float RowGap { get; set; } = float.NaN;
    public float ColumnGap { get; set; } = float.NaN;

    // Visual
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public Color BorderColor { get; set; } = Color.Transparent;
    public float BorderRadius { get; set; } = 0;
    public CornerRadius BorderCornerRadius { get; set; }
    public BorderStyle BorderStyle { get; set; } = BorderStyle.Solid;
    public float Opacity { get; set; } = 1;
    public Visibility Visibility { get; set; } = Visibility.Visible;
    public string Cursor { get; set; } = "default";
    public BoxShadow BoxShadow { get; set; } = BoxShadow.None;
    public string? BackgroundImage { get; set; }
    public CssGradient? BackgroundGradient { get; set; }

    // CSS Custom Properties (lazy-initialized to save ~1KB per element when unused)
    private Dictionary<string, string>? _customProperties;
    public Dictionary<string, string> CustomProperties
    {
        get => _customProperties ??= new();
        set => _customProperties = value;
    }
    public bool HasCustomProperties => _customProperties is { Count: > 0 };

    // Text
    public Color Color { get; set; } = new(0, 0, 0, 255);
    public string FontFamily { get; set; } = "sans-serif";
    public float FontSize { get; set; } = 16;
    public int FontWeight { get; set; } = 400;
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;
    public float LineHeight { get; set; } = 1.2f;
    public TextAlign TextAlign { get; set; } = TextAlign.Left;
    public float LetterSpacing { get; set; } = 0;
    public WhiteSpace WhiteSpace { get; set; } = WhiteSpace.Normal;
    public TextOverflow TextOverflow { get; set; } = TextOverflow.Clip;
    public WordBreak WordBreak { get; set; } = WordBreak.Normal;
    public TextDecoration TextDecoration { get; set; } = TextDecoration.None;
    public TextTransform TextTransform { get; set; } = TextTransform.None;

    // Transitions
    public string? TransitionProperty { get; set; }
    public float TransitionDuration { get; set; } = 0;
    public string? TransitionTimingFunction { get; set; }

    // Animation
    public string? AnimationName { get; set; }
    public float AnimationDuration { get; set; } = 0;
    public float AnimationDelay { get; set; } = 0;
    public int AnimationIterationCount { get; set; } = 1;
    public AnimationDirection AnimationDirection { get; set; } = AnimationDirection.Normal;
    public AnimationFillMode AnimationFillMode { get; set; } = AnimationFillMode.None;
    public string? AnimationTimingFunction { get; set; }

    // Pointer events
    public bool PointerEvents { get; set; } = true;

    // Transform
    public CssTransform Transform { get; set; } = CssTransform.Identity;
    public float TransformOriginX { get; set; } = 50; // percentage
    public float TransformOriginY { get; set; } = 50; // percentage

    /// <summary>
    /// Reset all properties to their default values. Used by pooled style resolution
    /// to avoid allocating new ComputedStyle instances every frame.
    /// </summary>
    public void Reset()
    {
        Width = float.NaN;
        Height = float.NaN;
        MinWidth = 0;
        MaxWidth = float.PositiveInfinity;
        MinHeight = 0;
        MaxHeight = float.PositiveInfinity;
        Margin = default;
        Padding = default;
        BorderWidth = default;
        BoxSizing = BoxSizing.BorderBox;

        Display = DisplayMode.Block;
        Position = Position.Relative;
        FlexDirection = FlexDirection.Column;
        FlexWrap = FlexWrap.NoWrap;
        JustifyContent = JustifyContent.FlexStart;
        AlignItems = AlignItems.Stretch;
        AlignSelf = AlignItems.Stretch;
        FlexGrow = 0;
        FlexShrink = 1;
        FlexBasis = float.NaN;
        Top = float.NaN;
        Right = float.NaN;
        Bottom = float.NaN;
        Left = float.NaN;
        ZIndex = 0;
        Overflow = Overflow.Visible;
        Gap = 0;
        RowGap = float.NaN;
        ColumnGap = float.NaN;

        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
        BorderRadius = 0;
        BorderCornerRadius = default;
        BorderStyle = BorderStyle.Solid;
        Opacity = 1;
        Visibility = Visibility.Visible;
        Cursor = "default";
        BoxShadow = BoxShadow.None;
        BackgroundImage = null;
        BackgroundGradient = null;

        _customProperties?.Clear();

        Color = new Color(0, 0, 0, 255);
        FontFamily = "sans-serif";
        FontSize = 16;
        FontWeight = 400;
        FontStyle = FontStyle.Normal;
        LineHeight = 1.2f;
        TextAlign = TextAlign.Left;
        LetterSpacing = 0;
        WhiteSpace = WhiteSpace.Normal;
        TextOverflow = TextOverflow.Clip;
        WordBreak = WordBreak.Normal;
        TextDecoration = TextDecoration.None;
        TextTransform = TextTransform.None;

        TransitionProperty = null;
        TransitionDuration = 0;
        TransitionTimingFunction = null;

        AnimationName = null;
        AnimationDuration = 0;
        AnimationDelay = 0;
        AnimationIterationCount = 1;
        AnimationDirection = AnimationDirection.Normal;
        AnimationFillMode = AnimationFillMode.None;
        AnimationTimingFunction = null;

        PointerEvents = true;

        Transform = CssTransform.Identity;
        TransformOriginX = 50;
        TransformOriginY = 50;
    }
}

/// <summary>
/// Edge values for margin, padding, and border.
/// </summary>
public record struct EdgeValues(float Top, float Right, float Bottom, float Left)
{
    public static readonly EdgeValues Zero = new(0, 0, 0, 0);

    public EdgeValues(float all) : this(all, all, all, all) { }
    public EdgeValues(float vertical, float horizontal) : this(vertical, horizontal, vertical, horizontal) { }
}

/// <summary>
/// Box shadow value.
/// </summary>
public record struct BoxShadow(float OffsetX, float OffsetY, float BlurRadius, float SpreadRadius, Color Color, bool Inset)
{
    public static readonly BoxShadow None = default;
    public bool IsNone => Color.A == 0 && BlurRadius == 0 && OffsetX == 0 && OffsetY == 0;
}

/// <summary>
/// RGBA color value.
/// </summary>
public record struct Color(byte R, byte G, byte B, byte A)
{
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color Black = new(0, 0, 0, 255);
    public static readonly Color White = new(255, 255, 255, 255);

    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return hex.Length switch
        {
            3 => new Color(
                (byte)(Convert.ToByte(hex[..1], 16) * 17),
                (byte)(Convert.ToByte(hex[1..2], 16) * 17),
                (byte)(Convert.ToByte(hex[2..3], 16) * 17),
                255),
            4 => new Color(
                (byte)(Convert.ToByte(hex[..1], 16) * 17),
                (byte)(Convert.ToByte(hex[1..2], 16) * 17),
                (byte)(Convert.ToByte(hex[2..3], 16) * 17),
                (byte)(Convert.ToByte(hex[3..4], 16) * 17)),
            6 => new Color(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                255),
            8 => new Color(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                Convert.ToByte(hex[6..8], 16)),
            _ => Black
        };
    }
}

/// <summary>
/// Per-corner border radius. When HasPerCorner is true, uses individual values instead of uniform.
/// </summary>
public record struct CornerRadius(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public bool HasPerCorner => TopLeft != TopRight || TopRight != BottomRight || BottomRight != BottomLeft;
    public float Uniform => TopLeft;
    public CornerRadius(float all) : this(all, all, all, all) { }
}

public enum BorderStyle
{
    None,
    Solid,
    Dashed,
    Dotted,
    Double
}

/// <summary>
/// 2D CSS transform: translate, scale, rotate, skew.
/// </summary>
public record struct CssTransform(
    float TranslateX, float TranslateY,
    float ScaleX, float ScaleY,
    float Rotate,
    float SkewX, float SkewY)
{
    public static readonly CssTransform Identity = new(0, 0, 1, 1, 0, 0, 0);
    public bool IsIdentity => TranslateX == 0 && TranslateY == 0 &&
                              ScaleX == 1 && ScaleY == 1 &&
                              Rotate == 0 && SkewX == 0 && SkewY == 0;
}
