namespace Lumi.Core.Components;

/// <summary>
/// A tooltip that appears on hover near a target element.
/// </summary>
public class LumiTooltip : IDisposable
{
    private readonly BoxElement _container;
    private readonly TextElement _textElement;
    private Element? _target;
    private RoutedEventHandler? _mouseEnterHandler;
    private RoutedEventHandler? _mouseLeaveHandler;
    private RoutedEventHandler? _containerLeaveHandler;
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
        tooltip._target = target;
        ComponentStyles.SetVisible(tooltip._container, false);
        target.AddChild(tooltip._container);

        tooltip._mouseEnterHandler = (_, _) =>
        {
            ComponentStyles.SetVisible(tooltip._container, true);
            target.MarkDirty();
        };
        target.On("mouseenter", tooltip._mouseEnterHandler);

        tooltip._mouseLeaveHandler = (_, e) =>
        {
            if (e is RoutedMouseEvent me &&
                (tooltip._container.LayoutBox.Contains(me.X, me.Y) ||
                 target.LayoutBox.Contains(me.X, me.Y)))
            {
                return;
            }

            ComponentStyles.SetVisible(tooltip._container, false);
            target.MarkDirty();
        };
        target.On("mouseleave", tooltip._mouseLeaveHandler);

        tooltip._containerLeaveHandler = (_, e) =>
        {
            if (e is RoutedMouseEvent me &&
                (target.LayoutBox.Contains(me.X, me.Y) ||
                 tooltip._container.LayoutBox.Contains(me.X, me.Y)))
            {
                return;
            }

            ComponentStyles.SetVisible(tooltip._container, false);
            target.MarkDirty();
        };
        tooltip._container.On("mouseleave", tooltip._containerLeaveHandler);

        return tooltip;
    }

    public void Dispose()
    {
        if (_target != null)
        {
            if (_mouseEnterHandler != null)
                _target.Off("mouseenter", _mouseEnterHandler);
            if (_mouseLeaveHandler != null)
                _target.Off("mouseleave", _mouseLeaveHandler);
        }
        if (_containerLeaveHandler != null)
            Root.Off("mouseleave", _containerLeaveHandler);
        _mouseEnterHandler = null;
        _mouseLeaveHandler = null;
        _containerLeaveHandler = null;
        _target = null;
    }
}
