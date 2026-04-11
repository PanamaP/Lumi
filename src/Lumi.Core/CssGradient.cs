namespace Lumi.Core;

/// <summary>
/// Represents a CSS gradient (linear or radial) with color stops.
/// </summary>
public class CssGradient
{
    public GradientType Type { get; set; }

    /// <summary>
    /// Angle in degrees for linear gradients (0 = to top, 90 = to right, 180 = to bottom).
    /// </summary>
    public float Angle { get; set; }

    public List<GradientStop> Stops { get; set; } = [];
}

public struct GradientStop
{
    public Color Color { get; set; }

    /// <summary>Position along the gradient line, 0.0 to 1.0.</summary>
    public float Position { get; set; }

    public GradientStop(Color color, float position)
    {
        Color = color;
        Position = position;
    }
}

public enum GradientType
{
    Linear,
    Radial
}
