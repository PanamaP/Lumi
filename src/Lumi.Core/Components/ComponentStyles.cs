namespace Lumi.Core.Components;

/// <summary>
/// Default dark-theme styles applied consistently across all Lumi components.
/// Uses InlineStyle strings so styles survive the StyleResolver cascade.
/// </summary>
public static class ComponentStyles
{
    // Dark theme palette
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
            ButtonVariant.Primary => (Accent, new Color(15, 23, 42, 255), Accent),
            ButtonVariant.Danger => (Danger, TextColor, Danger),
            _ => (Surface, TextColor, Border)
        };

        el.InlineStyle = $"display: flex; flex-direction: row; align-items: center; justify-content: center; " +
                         $"padding: 8px 16px; border-radius: 6px; border-width: 1px; cursor: pointer; " +
                         $"background-color: {ToRgba(bg)}; color: {ToRgba(fg)}; border-color: {ToRgba(border)}";
    }

    public static void ApplyDisabledButton(Element el)
    {
        el.InlineStyle = $"background-color: {ToRgba(Disabled)}; color: {ToRgba(Subtle)}; " +
                         $"border-color: {ToRgba(Disabled)}; cursor: default; opacity: 0.6";
    }

    public static void ApplyLabel(Element el)
    {
        el.InlineStyle = $"color: {ToRgba(Subtle)}; font-size: 13px";
    }

    public static void ApplyTextInput(Element el)
    {
        el.InlineStyle = $"display: block; padding: 8px 12px; border-radius: 4px; border-width: 1px; " +
                         $"border-color: {ToRgba(Border)}; background-color: {ToRgba(Background)}; " +
                         $"color: {ToRgba(TextColor)}; font-size: 14px";
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
        el.InlineStyle = $"background-color: {ToRgba(Surface)}; border-radius: 8px; padding: 0px; " +
                         $"min-width: 300px; min-height: 150px; display: flex; flex-direction: column";
    }

    public static void ApplyListContainer(Element el)
    {
        el.InlineStyle = $"display: flex; flex-direction: column; " +
                         $"background-color: {ToRgba(Background)}; border-width: 1px; " +
                         $"border-color: {ToRgba(Border)}; border-radius: 4px";
    }

    public static void ApplyListRow(Element el)
    {
        el.InlineStyle = $"display: flex; flex-direction: row; align-items: center; " +
                         $"padding: 8px 12px; border-width: 0px 0px 1px 0px; " +
                         $"border-color: {ToRgba(Border)}; cursor: pointer; " +
                         $"color: {ToRgba(TextColor)}";
    }
}
