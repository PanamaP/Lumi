namespace Lumi.Core.Components;

/// <summary>
/// A tooltip that appears on hover near a target element.
/// </summary>
public class LumiTooltip
{
    private readonly BoxElement _container;
    private readonly TextElement _textElement;
    private string _text = "";

    public Element Root => _container;

    public string Text
    {
        get => _text;
        set { _text = value; _textElement.Text = value; }
    }

    public LumiTooltip()
    {
        _container = new BoxElement("div");
        ComponentStyles.ApplyTooltip(_container);

        _textElement = new TextElement();
        _textElement.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; font-size: 12px";
        _container.AddChild(_textElement);
    }

    /// <summary>
    /// Attaches a tooltip to a target element. The tooltip appears on mouse enter
    /// and hides on mouse leave.
    /// </summary>
    public static LumiTooltip Attach(Element target, string text)
    {
        var tooltip = new LumiTooltip { Text = text };
        ComponentStyles.SetVisible(tooltip._container, false);
        target.AddChild(tooltip._container);

        target.On("mouseenter", (_, _) =>
        {
            ComponentStyles.SetVisible(tooltip._container, true);
            target.MarkDirty();
        });

        target.On("mouseleave", (_, _) =>
        {
            ComponentStyles.SetVisible(tooltip._container, false);
            target.MarkDirty();
        });

        return tooltip;
    }
}
