namespace Lumi.Core.Accessibility;

/// <summary>
/// Parses ARIA attributes and infers semantic roles from element tag names.
/// </summary>
public static class AriaParser
{
    private static readonly Dictionary<string, string> TagToRole = new(StringComparer.OrdinalIgnoreCase)
    {
        ["button"] = "button",
        ["input"] = "textbox",
        ["nav"] = "navigation",
        ["main"] = "main",
        ["header"] = "banner",
        ["footer"] = "contentinfo",
        ["h1"] = "heading",
        ["h2"] = "heading",
        ["h3"] = "heading",
        ["h4"] = "heading",
        ["h5"] = "heading",
        ["h6"] = "heading",
        ["ul"] = "list",
        ["ol"] = "list",
        ["li"] = "listitem",
        ["a"] = "link",
        ["img"] = "img",
        ["aside"] = "complementary",
        ["article"] = "article",
        ["section"] = "region",
        ["dialog"] = "dialog",
    };

    /// <summary>
    /// Reads ARIA attributes from element.Attributes and sets AccessibilityInfo.
    /// Also infers roles from tag names when no explicit role is set.
    /// </summary>
    public static void ApplyAriaAttributes(Element element)
    {
        var info = element.Accessibility;
        var attrs = element.Attributes;

        // Explicit role attribute overrides tag inference
        if (attrs.TryGetValue("role", out var role) && !string.IsNullOrEmpty(role))
            info.Role = role;
        else if (TagToRole.TryGetValue(element.TagName, out var inferredRole))
            info.Role = inferredRole;

        // aria-label
        if (attrs.TryGetValue("aria-label", out var label) && !string.IsNullOrEmpty(label))
            info.Label = label;

        // aria-describedby (stores the text directly for now)
        if (attrs.TryGetValue("aria-describedby", out var describedby) && !string.IsNullOrEmpty(describedby))
            info.Description = describedby;

        // aria-hidden
        if (attrs.TryGetValue("aria-hidden", out var hidden))
            info.IsHidden = string.Equals(hidden, "true", StringComparison.OrdinalIgnoreCase);

        // aria-live
        if (attrs.TryGetValue("aria-live", out var live) && !string.IsNullOrEmpty(live))
        {
            info.IsLive = !string.Equals(live, "off", StringComparison.OrdinalIgnoreCase);
            info.LiveMode = live;
        }

        // aria-valuenow/min/max
        if (attrs.TryGetValue("aria-valuenow", out var valueNow) && float.TryParse(valueNow, out var vn))
            info.ValueNow = vn;

        if (attrs.TryGetValue("aria-valuemin", out var valueMin) && float.TryParse(valueMin, out var vmin))
            info.ValueMin = vmin;

        if (attrs.TryGetValue("aria-valuemax", out var valueMax) && float.TryParse(valueMax, out var vmax))
            info.ValueMax = vmax;
    }
}
