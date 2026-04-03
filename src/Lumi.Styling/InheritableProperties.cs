using Lumi.Core;

namespace Lumi.Styling;

/// <summary>
/// Tracks which CSS properties inherit from parent to child and applies inheritance.
/// </summary>
public static class InheritableProperties
{
    private static readonly HashSet<string> _inheritable =
    [
        "color",
        "font-family",
        "font-size",
        "font-weight",
        "font-style",
        "line-height",
        "text-align",
        "letter-spacing",
        "cursor",
        "visibility"
    ];

    public static bool IsInheritable(string property) => _inheritable.Contains(property);

    /// <summary>
    /// Copies inheritable property values from parent to child for any property
    /// that hasn't been explicitly set on the child (i.e., still at default values).
    /// </summary>
    public static void InheritFrom(ComputedStyle child, ComputedStyle parent, HashSet<string>? explicitlySet = null)
    {
        // Only inherit if the property was NOT explicitly set on the child
        if (explicitlySet == null || !explicitlySet.Contains("color"))
            child.Color = parent.Color;

        if (explicitlySet == null || !explicitlySet.Contains("font-family"))
            child.FontFamily = parent.FontFamily;

        if (explicitlySet == null || !explicitlySet.Contains("font-size"))
            child.FontSize = parent.FontSize;

        if (explicitlySet == null || !explicitlySet.Contains("font-weight"))
            child.FontWeight = parent.FontWeight;

        if (explicitlySet == null || !explicitlySet.Contains("font-style"))
            child.FontStyle = parent.FontStyle;

        if (explicitlySet == null || !explicitlySet.Contains("line-height"))
            child.LineHeight = parent.LineHeight;

        if (explicitlySet == null || !explicitlySet.Contains("text-align"))
            child.TextAlign = parent.TextAlign;

        if (explicitlySet == null || !explicitlySet.Contains("letter-spacing"))
            child.LetterSpacing = parent.LetterSpacing;

        if (explicitlySet == null || !explicitlySet.Contains("cursor"))
            child.Cursor = parent.Cursor;

        if (explicitlySet == null || !explicitlySet.Contains("visibility"))
            child.Visibility = parent.Visibility;

        // CSS custom properties always inherit (per spec)
        foreach (var kvp in parent.CustomProperties)
        {
            if (!child.CustomProperties.ContainsKey(kvp.Key))
                child.CustomProperties[kvp.Key] = kvp.Value;
        }
    }
}
