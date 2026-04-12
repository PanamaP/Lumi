namespace Lumi.Core.Animation;

/// <summary>
/// Watches for style property changes on elements and creates tweens for smooth CSS transitions.
/// </summary>
public sealed class TransitionManager
{
    private readonly TweenEngine _tweenEngine = new();
    private readonly Dictionary<Element, Dictionary<string, float>> _previousValues = [];
    private readonly Dictionary<Element, Dictionary<string, Color>> _previousColors = [];
    private readonly HashSet<(Element, string)> _activeTransitions = [];

    /// <summary>
    /// Tick the transition manager — call once per frame.
    /// </summary>
    public void Update(double deltaTime)
    {
        _tweenEngine.Update((float)deltaTime);
    }

    /// <summary>
    /// Clears all tracked state and running transitions.
    /// Used during window navigation to release old element references.
    /// </summary>
    public void Clear()
    {
        _tweenEngine.Clear();
        _previousValues.Clear();
        _previousColors.Clear();
        _activeTransitions.Clear();
    }

    /// <summary>
    /// Snapshot current style values for an element so we can detect changes next frame.
    /// Call after styles have been resolved.
    /// </summary>
    public void CaptureState(Element element)
    {
        var style = element.ComputedStyle;
        var values = GetOrCreateValues(element);
        var colors = GetOrCreateColors(element);

        values["opacity"] = style.Opacity;
        values["width"] = style.Width;
        values["height"] = style.Height;
        values["margin-top"] = style.Margin.Top;
        values["margin-right"] = style.Margin.Right;
        values["margin-bottom"] = style.Margin.Bottom;
        values["margin-left"] = style.Margin.Left;
        values["padding-top"] = style.Padding.Top;
        values["padding-right"] = style.Padding.Right;
        values["padding-bottom"] = style.Padding.Bottom;
        values["padding-left"] = style.Padding.Left;
        values["border-radius"] = style.BorderRadius;
        values["font-size"] = style.FontSize;

        colors["background-color"] = style.BackgroundColor;
    }

    /// <summary>
    /// Check for property changes and create tweens for any that have transition settings.
    /// Call after styles are resolved but before painting.
    /// </summary>
    public void DetectChanges(Element element)
    {
        var style = element.ComputedStyle;
        if (style.TransitionDuration <= 0 || string.IsNullOrEmpty(style.TransitionProperty))
            return;

        var transitionProps = ParseTransitionProperties(style.TransitionProperty);
        var easingFunc = Easing.FromName(style.TransitionTimingFunction ?? "ease");
        float duration = style.TransitionDuration;

        if (!_previousValues.TryGetValue(element, out var prevValues))
            return; // No previous state captured yet

        _previousColors.TryGetValue(element, out var prevColors);

        foreach (var prop in transitionProps)
        {
            if (prop is "all" or "background-color" && prevColors != null)
            {
                TryTransitionColor(element, "background-color",
                    prevColors.GetValueOrDefault("background-color", Color.Transparent),
                    style.BackgroundColor, duration, easingFunc);
            }

            if (prop == "all" || IsNumericProperty(prop))
            {
                var propsToCheck = prop == "all" ? NumericPropertyNames : [prop];
                foreach (var p in propsToCheck)
                {
                    float oldVal = prevValues.GetValueOrDefault(p, float.NaN);
                    float newVal = GetNumericPropertyValue(style, p);

                    if (!float.IsNaN(oldVal) && !float.IsNaN(newVal) && MathF.Abs(oldVal - newVal) > 0.001f)
                    {
                        if (_activeTransitions.Contains((element, p)))
                            continue;

                        _activeTransitions.Add((element, p));
                        var tween = new Tween(oldVal, newVal, duration, easingFunc)
                        {
                            OnUpdate = v => SetNumericPropertyValue(element.ComputedStyle, p, v),
                            OnComplete = () =>
                            {
                                _activeTransitions.Remove((element, p));
                                element.MarkDirty();
                            }
                        };
                        _tweenEngine.Add(tween);
                        SetNumericPropertyValue(style, p, oldVal);
                        element.MarkDirty();
                    }
                }
            }
        }

        // Update captured state for next frame
        CaptureState(element);
    }

    private void TryTransitionColor(Element element, string prop, Color oldColor, Color newColor,
        float duration, Func<float, float> easingFunc)
    {
        if (oldColor == newColor) return;
        if (_activeTransitions.Contains((element, prop))) return;

        _activeTransitions.Add((element, prop));
        var tween = new Tween(0f, 1f, duration, easingFunc)
        {
            OnUpdate = t =>
            {
                byte r = (byte)(oldColor.R + (newColor.R - oldColor.R) * t);
                byte g = (byte)(oldColor.G + (newColor.G - oldColor.G) * t);
                byte b = (byte)(oldColor.B + (newColor.B - oldColor.B) * t);
                byte a = (byte)(oldColor.A + (newColor.A - oldColor.A) * t);
                element.ComputedStyle.BackgroundColor = new Color(r, g, b, a);
                element.MarkDirty();
            },
            OnComplete = () => _activeTransitions.Remove((element, prop))
        };
        _tweenEngine.Add(tween);
        element.ComputedStyle.BackgroundColor = oldColor;
    }

    private static readonly string[] NumericPropertyNames =
    [
        "opacity", "width", "height",
        "margin-top", "margin-right", "margin-bottom", "margin-left",
        "padding-top", "padding-right", "padding-bottom", "padding-left",
        "border-radius", "font-size"
    ];

    private static bool IsNumericProperty(string prop) =>
        Array.IndexOf(NumericPropertyNames, prop) >= 0;

    private static float GetNumericPropertyValue(ComputedStyle style, string prop) => prop switch
    {
        "opacity" => style.Opacity,
        "width" => style.Width,
        "height" => style.Height,
        "margin-top" => style.Margin.Top,
        "margin-right" => style.Margin.Right,
        "margin-bottom" => style.Margin.Bottom,
        "margin-left" => style.Margin.Left,
        "padding-top" => style.Padding.Top,
        "padding-right" => style.Padding.Right,
        "padding-bottom" => style.Padding.Bottom,
        "padding-left" => style.Padding.Left,
        "border-radius" => style.BorderRadius,
        "font-size" => style.FontSize,
        _ => float.NaN
    };

    private static void SetNumericPropertyValue(ComputedStyle style, string prop, float value)
    {
        switch (prop)
        {
            case "opacity": style.Opacity = value; break;
            case "width": style.Width = value; break;
            case "height": style.Height = value; break;
            case "margin-top": style.Margin = style.Margin with { Top = value }; break;
            case "margin-right": style.Margin = style.Margin with { Right = value }; break;
            case "margin-bottom": style.Margin = style.Margin with { Bottom = value }; break;
            case "margin-left": style.Margin = style.Margin with { Left = value }; break;
            case "padding-top": style.Padding = style.Padding with { Top = value }; break;
            case "padding-right": style.Padding = style.Padding with { Right = value }; break;
            case "padding-bottom": style.Padding = style.Padding with { Bottom = value }; break;
            case "padding-left": style.Padding = style.Padding with { Left = value }; break;
            case "border-radius": style.BorderRadius = value; break;
            case "font-size": style.FontSize = value; break;
        }
    }

    private static string[] ParseTransitionProperties(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private Dictionary<string, float> GetOrCreateValues(Element element)
    {
        if (!_previousValues.TryGetValue(element, out var values))
        {
            values = new Dictionary<string, float>();
            _previousValues[element] = values;
        }
        return values;
    }

    private Dictionary<string, Color> GetOrCreateColors(Element element)
    {
        if (!_previousColors.TryGetValue(element, out var colors))
        {
            colors = new Dictionary<string, Color>();
            _previousColors[element] = colors;
        }
        return colors;
    }
}
