namespace Lumi.Core.Accessibility;

/// <summary>
/// Holds ARIA-like accessibility metadata for an element.
/// </summary>
public class AccessibilityInfo
{
    public string? Role { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public bool IsHidden { get; set; }
    public bool IsLive { get; set; }
    public string LiveMode { get; set; } = "off";
    public float? ValueNow { get; set; }
    public float? ValueMin { get; set; }
    public float? ValueMax { get; set; }
}
