namespace Lumi.Core.Animation;

/// <summary>
/// Interpolates a float value from <see cref="From"/> to <see cref="To"/> over <see cref="Duration"/> seconds.
/// </summary>
public sealed class Tween
{
    public float From { get; }
    public float To { get; }
    public float Duration { get; }
    public Func<float, float> EasingFunc { get; }
    public Action<float>? OnUpdate { get; set; }
    public Action? OnComplete { get; set; }

    public float Elapsed { get; internal set; }
    public bool IsComplete => Elapsed >= Duration;

    /// <summary>
    /// Current interpolated value.
    /// </summary>
    public float Value
    {
        get
        {
            if (Duration <= 0f) return To;
            float t = Math.Clamp(Elapsed / Duration, 0f, 1f);
            float eased = EasingFunc(t);
            return From + (To - From) * eased;
        }
    }

    public Tween(float from, float to, float duration, Func<float, float>? easing = null)
    {
        From = from;
        To = to;
        Duration = duration;
        EasingFunc = easing ?? Easing.Linear;
    }
}

/// <summary>
/// Manages a set of active tweens, advancing them each frame.
/// </summary>
public sealed class TweenEngine
{
    private readonly List<Tween> _tweens = [];
    private readonly List<Tween> _toAdd = [];

    public int ActiveCount => _tweens.Count;

    public void Add(Tween tween)
    {
        _toAdd.Add(tween);
    }

    public void Update(float deltaTime)
    {
        // Add pending tweens
        if (_toAdd.Count > 0)
        {
            _tweens.AddRange(_toAdd);
            _toAdd.Clear();
        }

        for (int i = _tweens.Count - 1; i >= 0; i--)
        {
            var tween = _tweens[i];
            tween.Elapsed += deltaTime;

            tween.OnUpdate?.Invoke(tween.Value);

            if (tween.IsComplete)
            {
                tween.OnUpdate?.Invoke(tween.To);
                tween.OnComplete?.Invoke();
                _tweens.RemoveAt(i);
            }
        }
    }

    public void Clear()
    {
        _tweens.Clear();
        _toAdd.Clear();
    }
}
