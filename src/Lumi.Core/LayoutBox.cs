namespace Lumi.Core;

/// <summary>
/// Computed layout position and size for an element, populated by the layout engine.
/// </summary>
public record struct LayoutBox(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Contains(float px, float py) =>
        px >= X && px <= Right && py >= Y && py <= Bottom;

    public static readonly LayoutBox Empty = new(0, 0, 0, 0);
}
