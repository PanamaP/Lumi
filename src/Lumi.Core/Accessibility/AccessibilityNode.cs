namespace Lumi.Core.Accessibility;

/// <summary>
/// A node in the flattened accessibility tree.
/// </summary>
public class AccessibilityNode
{
    public string Role { get; set; } = "none";
    public string Name { get; set; } = "";
    public Element Element { get; set; } = null!;
    public List<AccessibilityNode> Children { get; set; } = [];
}
