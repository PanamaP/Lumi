namespace Lumi.Core.Animation;

/// <summary>
/// Standard easing functions that map a linear progress t ∈ [0,1] to an eased value.
/// </summary>
public static class Easing
{
    public static float Linear(float t) => t;

    public static float EaseInCubic(float t) => t * t * t;

    public static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    public static float EaseInOutCubic(float t) =>
        t < 0.5f
            ? 4f * t * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;

    public static float EaseInQuad(float t) => t * t;

    public static float EaseOutQuad(float t) => t * (2f - t);

    /// <summary>
    /// Resolve a CSS timing-function name to a delegate.
    /// </summary>
    public static Func<float, float> FromName(string name) => name.ToLowerInvariant() switch
    {
        "linear" => Linear,
        "ease" => EaseInOutCubic,
        "ease-in" => EaseInCubic,
        "ease-out" => EaseOutCubic,
        "ease-in-out" => EaseInOutCubic,
        _ => EaseInOutCubic
    };
}
