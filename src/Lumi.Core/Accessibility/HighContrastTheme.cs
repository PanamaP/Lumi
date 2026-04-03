namespace Lumi.Core.Accessibility;

/// <summary>
/// Provides high-contrast color overrides for accessibility.
/// </summary>
public static class HighContrastTheme
{
    public static readonly Color Background = new(0, 0, 0, 255);
    public static readonly Color Foreground = new(255, 255, 255, 255);
    public static readonly Color Hyperlink = new(255, 255, 0, 255);
    public static readonly Color Disabled = new(128, 128, 128, 255);
    public static readonly Color ButtonFace = new(0, 0, 0, 255);
    public static readonly Color ButtonText = new(255, 255, 255, 255);
    public static readonly Color ButtonBorder = new(255, 255, 255, 255);

    /// <summary>
    /// Returns a dictionary of CSS-like property overrides for high contrast mode.
    /// </summary>
    public static Dictionary<string, string> GetHighContrastStyle()
    {
        return new Dictionary<string, string>
        {
            ["background-color"] = FormatColor(Background),
            ["color"] = FormatColor(Foreground),
            ["border-color"] = FormatColor(ButtonBorder),
        };
    }

    /// <summary>
    /// Walks the element tree and overrides visual styles for high contrast.
    /// </summary>
    public static void Apply(Element root)
    {
        ApplyToElement(root);
        foreach (var child in root.Children)
            Apply(child);
    }

    private static void ApplyToElement(Element element)
    {
        var style = element.ComputedStyle;
        var role = element.Accessibility.Role;

        style.BackgroundColor = Background;
        style.Color = Foreground;
        style.BorderColor = ButtonBorder;

        // Links/hyperlinks get distinct color
        if (role is "link" || element.TagName == "a")
        {
            style.Color = Hyperlink;
        }

        // Disabled inputs
        if (element is InputElement { IsDisabled: true })
        {
            style.Color = Disabled;
        }

        // Buttons
        if (role is "button" || element.TagName == "button")
        {
            style.BackgroundColor = ButtonFace;
            style.Color = ButtonText;
            style.BorderColor = ButtonBorder;
        }
    }

    private static string FormatColor(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
