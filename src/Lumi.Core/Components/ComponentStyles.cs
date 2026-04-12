namespace Lumi.Core.Components;

/// <summary>
/// Default styles applied consistently across all Lumi components.
/// Uses CSS custom properties (theme variables) where possible so components
/// automatically adapt to light/dark theme changes.
/// </summary>
public static class ComponentStyles
{
    // Fallback palette (dark theme) — used when theme variables are not resolved
    public static readonly Color Background = Color.FromHex("1E293B");
    public static readonly Color Accent = Color.FromHex("38BDF8");
    public static readonly Color TextColor = Color.FromHex("F8FAFC");
    public static readonly Color Subtle = Color.FromHex("94A3B8");
    public static readonly Color Danger = Color.FromHex("EF4444");
    public static readonly Color Surface = Color.FromHex("334155");
    public static readonly Color Overlay = new(0, 0, 0, 160);
    public static readonly Color Border = Color.FromHex("475569");
    public static readonly Color Disabled = Color.FromHex("64748B");

    public static string ToRgba(Color c) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"rgba({c.R},{c.G},{c.B},{c.A / 255.0:F2})");

    /// <summary>
    /// Appends additional CSS declarations to an element's existing inline style.
    /// </summary>
    public static void AppendStyle(Element el, string additionalCss)
    {
        if (string.IsNullOrEmpty(el.InlineStyle))
            el.InlineStyle = additionalCss;
        else
            el.InlineStyle = el.InlineStyle.TrimEnd(';', ' ') + "; " + additionalCss;
    }

    public static void ApplyButton(Element el, ButtonVariant variant)
    {
        var (bg, fg, border) = variant switch
        {
            ButtonVariant.Primary => ($"var(--accent, {ToRgba(Accent)})",
                                      $"var(--bg-primary, {ToRgba(new Color(15, 23, 42, 255))})",
                                      $"var(--accent, {ToRgba(Accent)})"),
            ButtonVariant.Danger  => ($"var(--error, {ToRgba(Danger)})",
                                      $"var(--text-primary, {ToRgba(TextColor)})",
                                      $"var(--error, {ToRgba(Danger)})"),
            _                     => ($"var(--bg-tertiary, {ToRgba(Surface)})",
                                      $"var(--text-primary, {ToRgba(TextColor)})",
                                      $"var(--border-color, {ToRgba(Border)})")
        };

        el.InlineStyle = $"display: flex; flex-direction: row; align-items: center; justify-content: center; " +
                         $"padding: 8px 16px; border-radius: 6px; border-width: 1px; cursor: pointer; " +
                         $"background-color: {bg}; color: {fg}; border-color: {border}";
    }

    public static void ApplyDisabledButton(Element el)
    {
        el.InlineStyle = $"background-color: var(--text-muted, {ToRgba(Disabled)}); " +
                         $"color: var(--text-secondary, {ToRgba(Subtle)}); " +
                         $"border-color: var(--text-muted, {ToRgba(Disabled)}); cursor: default; opacity: 0.6";
    }

    public static void ApplyLabel(Element el)
    {
        el.InlineStyle = $"color: var(--text-secondary, {ToRgba(Subtle)}); font-size: 13px";
    }

    public static void ApplyTextInput(Element el)
    {
        el.InlineStyle = $"display: block; padding: 8px 12px; border-radius: 4px; border-width: 1px; " +
                         $"border-color: var(--border-color, {ToRgba(Border)}); " +
                         $"background-color: var(--bg-secondary, {ToRgba(Background)}); " +
                         $"color: var(--text-primary, {ToRgba(TextColor)}); font-size: 14px; min-height: 36px";
    }

    public static void ApplyContainer(Element el, FlexDirection direction = FlexDirection.Column)
    {
        var dir = direction == FlexDirection.Row ? "row" : "column";
        el.InlineStyle = $"display: flex; flex-direction: {dir}";
    }

    public static void ApplyOverlay(Element el)
    {
        el.InlineStyle = $"position: fixed; top: 0px; left: 0px; right: 0px; bottom: 0px; " +
                         $"background-color: {ToRgba(Overlay)}; display: flex; " +
                         $"justify-content: center; align-items: center; z-index: 1000";
    }

    public static void ApplyDialogPanel(Element el)
    {
        el.InlineStyle = $"background-color: var(--bg-tertiary, {ToRgba(Surface)}); border-radius: 8px; padding: 0px; " +
                         $"min-width: 300px; min-height: 150px; display: flex; flex-direction: column";
    }

    public static void ApplyListContainer(Element el)
    {
        el.InlineStyle = $"display: flex; flex-direction: column; " +
                         $"background-color: var(--bg-secondary, {ToRgba(Background)}); border-width: 1px; " +
                         $"border-color: var(--border-color, {ToRgba(Border)}); border-radius: 4px";
    }

    public static void ApplyListRow(Element el)
    {
        el.InlineStyle = $"display: flex; flex-direction: row; align-items: center; " +
                         $"padding: 8px 12px; border-width: 0px 0px 1px 0px; " +
                         $"border-color: var(--border-color, {ToRgba(Border)}); cursor: pointer; " +
                         $"color: var(--text-primary, {ToRgba(TextColor)})";
    }

    public static void ApplyRadioGroup(Element el)
    {
        el.InlineStyle = "display: flex; flex-direction: column; gap: 4px";
    }

    public static void ApplyToggleTrack(Element el, bool isOn)
    {
        var bg = isOn
            ? $"var(--accent, {ToRgba(Accent)})"
            : $"var(--border-color, {ToRgba(Border)})";
        el.InlineStyle = $"width: 44px; height: 24px; border-radius: 12px; position: relative; " +
                         $"background-color: {bg}";
    }

    public static void ApplyProgressTrack(Element el)
    {
        el.InlineStyle = $"width: 100%; height: 8px; border-radius: 4px; overflow: hidden; " +
                         $"background-color: var(--border-color, {ToRgba(Border)})";
    }

    public static void ApplyTabHeader(Element el)
    {
        el.InlineStyle = $"display: flex; flex-direction: row; " +
                         $"border-width: 0px 0px 1px 0px; border-color: var(--border-color, {ToRgba(Border)}); " +
                         $"background-color: var(--bg-tertiary, {ToRgba(Surface)})";
    }

    public static void ApplyTooltip(Element el)
    {
        el.InlineStyle = $"padding: 4px 8px; border-radius: 4px; " +
                         $"background-color: rgba(0,0,0,0.85); z-index: 10000; pointer-events: none";
    }

    // Tracks elements that have been hidden via SetVisible(false).
    // Uses ConditionalWeakTable so the entries are automatically cleaned up when elements are GC'd.
    // The stored value is unused — the table is used only as a weak-keyed set.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Element, string> s_hiddenElements = new();

    // Matches 'display: none' (with optional spacing/semicolons) anywhere in an inline style string.
    private static readonly System.Text.RegularExpressions.Regex s_displayNoneRegex =
        new(@"\s*;?\s*display\s*:\s*none\s*;?", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Sets an element's display to none (hidden) or restores the previous display value.
    /// Only the display declaration is added or removed; other inline style changes are preserved.
    /// </summary>
    public static void SetVisible(Element el, bool visible)
    {
        if (visible)
        {
            // Strip display:none from the current inline style, preserving any other changes
            if (s_hiddenElements.TryGetValue(el, out _))
            {
                el.InlineStyle = s_displayNoneRegex.Replace(el.InlineStyle ?? "", "").Trim().TrimEnd(';');
                s_hiddenElements.Remove(el);
            }
        }
        else
        {
            var existing = el.InlineStyle ?? "";
            if (!s_hiddenElements.TryGetValue(el, out _))
            {
                // Mark as hidden and append display:none to the current style
                s_hiddenElements.Add(el, "");
                el.InlineStyle = string.IsNullOrEmpty(existing) ? "display: none" : existing.TrimEnd(';') + "; display: none";
            }
        }
        el.MarkDirty();
    }

    /// <summary>
    /// Returns the visual screen-space bounds of an element. LayoutBox already stores
    /// absolute coordinates; this method adjusts for ancestor scroll offsets so the
    /// result matches the element's on-screen position.
    /// </summary>
    internal static LayoutBox GetAbsoluteBounds(Element element)
    {
        float x = element.LayoutBox.X;
        float y = element.LayoutBox.Y;
        var current = element.Parent;
        while (current != null)
        {
            x -= current.ScrollLeft;
            y -= current.ScrollTop;
            current = current.Parent;
        }
        return new LayoutBox(x, y, element.LayoutBox.Width, element.LayoutBox.Height);
    }

    /// <summary>
    /// Walks up the element tree to find the root element.
    /// </summary>
    internal static Element FindRoot(Element element)
    {
        var current = element;
        while (current.Parent != null)
            current = current.Parent;
        return current;
    }
}
