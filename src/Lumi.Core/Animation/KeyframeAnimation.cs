namespace Lumi.Core.Animation;

/// <summary>
/// A single keyframe at a given percentage through an animation.
/// </summary>
public sealed record Keyframe(float Percent, Dictionary<string, string> Properties);

/// <summary>
/// A named @keyframes animation definition.
/// </summary>
public sealed class KeyframeAnimation
{
    public string Name { get; set; } = "";
    public float Duration { get; set; }
    public int IterationCount { get; set; } = 1;
    public AnimationDirection Direction { get; set; } = AnimationDirection.Normal;
    public AnimationFillMode FillMode { get; set; } = AnimationFillMode.None;
    public List<Keyframe> Keyframes { get; set; } = [];
}

public enum AnimationDirection { Normal, Reverse, Alternate, AlternateReverse }
public enum AnimationFillMode { None, Forwards, Backwards, Both }

/// <summary>
/// Plays keyframe animations on elements by interpolating properties between keyframes.
/// </summary>
public sealed class KeyframePlayer
{
    private readonly TweenEngine _tweenEngine = new();
    private readonly Dictionary<string, KeyframeAnimation> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ActiveAnimation> _active = [];

    /// <summary>
    /// Register a parsed @keyframes definition.
    /// </summary>
    public void Register(KeyframeAnimation animation)
    {
        _registry[animation.Name] = animation;
    }

    /// <summary>
    /// Look up a registered animation by name.
    /// </summary>
    public KeyframeAnimation? Get(string name) =>
        _registry.TryGetValue(name, out var anim) ? anim : null;

    /// <summary>
    /// Start playing a named animation on an element.
    /// </summary>
    public void Play(Element element, string animationName, float duration, int iterationCount = 1,
        AnimationDirection direction = AnimationDirection.Normal)
    {
        if (!_registry.TryGetValue(animationName, out var animation))
            return;

        var active = new ActiveAnimation
        {
            Element = element,
            Animation = animation,
            Duration = duration > 0 ? duration : animation.Duration,
            IterationCount = iterationCount,
            Direction = direction
        };
        _active.Add(active);
    }

    /// <summary>
    /// Tick all active animations.
    /// </summary>
    public void Update(float deltaTime)
    {
        _tweenEngine.Update(deltaTime);

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var anim = _active[i];
            anim.Elapsed += deltaTime;

            float totalDuration = anim.Duration * anim.IterationCount;
            if (anim.IterationCount < 0) // infinite
                totalDuration = float.MaxValue;

            if (anim.Elapsed >= totalDuration && anim.IterationCount > 0)
            {
                ApplyKeyframeAt(anim, 1f);
                _active.RemoveAt(i);
                continue;
            }

            float iterationProgress = anim.Duration > 0 ? (anim.Elapsed % anim.Duration) / anim.Duration : 1f;
            int currentIteration = anim.Duration > 0 ? (int)(anim.Elapsed / anim.Duration) : 0;

            // Handle direction
            bool reverse = anim.Direction switch
            {
                AnimationDirection.Reverse => true,
                AnimationDirection.Alternate => currentIteration % 2 == 1,
                AnimationDirection.AlternateReverse => currentIteration % 2 == 0,
                _ => false
            };

            float t = reverse ? 1f - iterationProgress : iterationProgress;
            ApplyKeyframeAt(anim, t);
        }
    }

    private static void ApplyKeyframeAt(ActiveAnimation anim, float t)
    {
        var keyframes = anim.Animation.Keyframes;
        if (keyframes.Count == 0) return;

        // Find surrounding keyframes
        Keyframe? before = null;
        Keyframe? after = null;

        for (int i = 0; i < keyframes.Count; i++)
        {
            if (keyframes[i].Percent <= t)
                before = keyframes[i];
            if (keyframes[i].Percent >= t && after == null)
                after = keyframes[i];
        }

        before ??= keyframes[0];
        after ??= keyframes[^1];

        float segmentT = 0f;
        if (before != after && MathF.Abs(after.Percent - before.Percent) > 0.0001f)
            segmentT = (t - before.Percent) / (after.Percent - before.Percent);

        // Interpolate all properties that exist in both keyframes
        var allProps = new HashSet<string>(before.Properties.Keys);
        foreach (var key in after.Properties.Keys)
            allProps.Add(key);

        foreach (var prop in allProps)
        {
            before.Properties.TryGetValue(prop, out var fromStr);
            after.Properties.TryGetValue(prop, out var toStr);

            fromStr ??= toStr;
            toStr ??= fromStr;
            if (fromStr == null) continue;

            // Try numeric interpolation
            if (TryParseNumeric(fromStr, out float fromVal) && TryParseNumeric(toStr!, out float toVal))
            {
                float value = fromVal + (toVal - fromVal) * segmentT;
                ApplyNumericProperty(anim.Element.ComputedStyle, prop, value);
            }

            anim.Element.MarkDirty();
        }
    }

    private static void ApplyNumericProperty(ComputedStyle style, string prop, float value)
    {
        switch (prop)
        {
            case "opacity": style.Opacity = value; break;
            case "width": style.Width = value; break;
            case "height": style.Height = value; break;
            case "border-radius": style.BorderRadius = value; break;
            case "font-size": style.FontSize = value; break;
            case "margin-top": style.Margin = style.Margin with { Top = value }; break;
            case "margin-right": style.Margin = style.Margin with { Right = value }; break;
            case "margin-bottom": style.Margin = style.Margin with { Bottom = value }; break;
            case "margin-left": style.Margin = style.Margin with { Left = value }; break;
            case "padding-top": style.Padding = style.Padding with { Top = value }; break;
            case "padding-right": style.Padding = style.Padding with { Right = value }; break;
            case "padding-bottom": style.Padding = style.Padding with { Bottom = value }; break;
            case "padding-left": style.Padding = style.Padding with { Left = value }; break;
        }
    }

    private static bool TryParseNumeric(string value, out float result)
    {
        var stripped = value.TrimEnd('p', 'x', 'e', 'm', '%', 's');
        return float.TryParse(stripped, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    private sealed class ActiveAnimation
    {
        public required Element Element { get; init; }
        public required KeyframeAnimation Animation { get; init; }
        public required float Duration { get; init; }
        public required int IterationCount { get; init; }
        public required AnimationDirection Direction { get; init; }
        public float Elapsed { get; set; }
    }
}
