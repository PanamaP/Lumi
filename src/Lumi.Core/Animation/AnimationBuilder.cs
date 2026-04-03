namespace Lumi.Core.Animation;

/// <summary>
/// Fluent API for building tween-based animations on elements.
/// Usage: element.Animate().Property("opacity", 0, 1).Duration(0.3f).Easing(Easing.EaseOut).Start()
/// </summary>
public sealed class AnimationBuilder
{
    private readonly Element _element;
    private readonly TweenEngine _engine;
    private readonly List<(string Property, float From, float To)> _properties = [];
    private float _duration = 0.3f;
    private Func<float, float> _easing = Animation.Easing.Linear;
    private Action? _onComplete;
    private float _delay;

    internal AnimationBuilder(Element element, TweenEngine engine)
    {
        _element = element;
        _engine = engine;
    }

    /// <summary>
    /// Add a property to animate from one value to another.
    /// </summary>
    public AnimationBuilder Property(string name, float from, float to)
    {
        _properties.Add((name, from, to));
        return this;
    }

    /// <summary>
    /// Set the animation duration in seconds.
    /// </summary>
    public AnimationBuilder Duration(float seconds)
    {
        _duration = seconds;
        return this;
    }

    /// <summary>
    /// Set the easing function.
    /// </summary>
    public AnimationBuilder Easing(Func<float, float> easing)
    {
        _easing = easing;
        return this;
    }

    /// <summary>
    /// Set a delay before the animation starts (in seconds).
    /// </summary>
    public AnimationBuilder Delay(float seconds)
    {
        _delay = seconds;
        return this;
    }

    /// <summary>
    /// Set a callback to invoke when all properties finish animating.
    /// </summary>
    public AnimationBuilder OnComplete(Action callback)
    {
        _onComplete = callback;
        return this;
    }

    /// <summary>
    /// Build and start the animation.
    /// </summary>
    public AnimationBuilder Start()
    {
        int remaining = _properties.Count;

        foreach (var (prop, from, to) in _properties)
        {
            var tween = new Tween(from, to, _duration, _easing)
            {
                // Apply delay by starting with negative elapsed time
                Elapsed = -_delay,
                OnUpdate = value =>
                {
                    SetProperty(_element, prop, value);
                    _element.MarkDirty();
                },
                OnComplete = () =>
                {
                    remaining--;
                    if (remaining <= 0)
                        _onComplete?.Invoke();
                }
            };
            _engine.Add(tween);
        }

        return this;
    }

    private static void SetProperty(Element element, string prop, float value)
    {
        var style = element.ComputedStyle;
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
}

/// <summary>
/// Extension methods for the fluent animation API.
/// </summary>
public static class AnimationExtensions
{
    /// <summary>
    /// Global tween engine shared by fluent API animations.
    /// Tick this each frame via <see cref="TweenEngine.Update"/>.
    /// </summary>
    public static TweenEngine GlobalTweenEngine { get; } = new();

    /// <summary>
    /// Begin building a fluent animation on this element.
    /// </summary>
    public static AnimationBuilder Animate(this Element element) =>
        new(element, GlobalTweenEngine);

    /// <summary>
    /// Begin building a fluent animation on this element using a specific tween engine.
    /// </summary>
    public static AnimationBuilder Animate(this Element element, TweenEngine engine) =>
        new(element, engine);
}
