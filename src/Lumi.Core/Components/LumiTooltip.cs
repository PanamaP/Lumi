namespace Lumi.Core.Components;

/// <summary>
/// A tooltip that appears on hover near a target element.
/// </summary>
public class LumiTooltip : IDisposable
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

        target.On("mouseleave", (_, e) =>
        {
            // When the mouse moves to the tooltip (a child of the target),
            // the event system fires mouseleave on the target. Don't hide
            // if the mouse is still within the tooltip or target bounds.
            if (e is RoutedMouseEvent me &&
                (tooltip._container.LayoutBox.Contains(me.X, me.Y) ||
                 target.LayoutBox.Contains(me.X, me.Y)))
            {
                return;
            }

            ComponentStyles.SetVisible(tooltip._container, false);
            target.MarkDirty();
        });

        // Hide when the mouse leaves the tooltip itself,
        // unless it moved back to the target.
        tooltip._container.On("mouseleave", (_, e) =>
        {
            if (e is RoutedMouseEvent me &&
                (target.LayoutBox.Contains(me.X, me.Y) ||
                 tooltip._container.LayoutBox.Contains(me.X, me.Y)))
            {
                return;
            }

            ComponentStyles.SetVisible(tooltip._container, false);
            target.MarkDirty();
        });

        return tooltip;
    }

    public void Dispose()
    {
        Root.RemoveAllEventHandlers();
    }
}
