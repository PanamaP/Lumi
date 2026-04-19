namespace Lumi.Core.Components;

/// <summary>
/// A tooltip that appears on hover near a target element.
/// Rendered at the root level to avoid ancestor clipping, with auto-positioning
/// to stay fully visible within the viewport.
/// </summary>
public class LumiTooltip : IDisposable
{
    private readonly BoxElement _container;
    private readonly TextElement _textElement;
    private Element? _target;
    private RoutedEventHandler? _mouseEnterHandler;
    private RoutedEventHandler? _mouseLeaveHandler;
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
        _textElement.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; font-size: 12px; pointer-events: none";
        _container.AddChild(_textElement);
    }

    /// <summary>
    /// Attaches a tooltip to a target element. The tooltip appears on mouse enter
    /// and hides on mouse leave. It is rendered at the root level with auto-positioning.
    /// </summary>
    public static LumiTooltip Attach(Element target, string text)
    {
        var tooltip = new LumiTooltip { Text = text };
        tooltip._target = target;

        tooltip._mouseEnterHandler = (_, _) =>
        {
            tooltip.Show();
        };
        target.On("mouseenter", tooltip._mouseEnterHandler);

        tooltip._mouseLeaveHandler = (_, _) =>
        {
            tooltip.Hide();
        };
        target.On("mouseleave", tooltip._mouseLeaveHandler);

        return tooltip;
    }

    private void Show()
    {
        if (_target == null) return;
        var root = ComponentStyles.FindRoot(_target);
        var targetBounds = ComponentStyles.GetAbsoluteBounds(_target);
        float viewW = root.LayoutBox.Width;
        float viewH = root.LayoutBox.Height;

        // Estimate tooltip size (use actual layout if available, else estimate from text)
        float tooltipW = _container.LayoutBox.Width > 0 ? _container.LayoutBox.Width : _text.Length * 7f + 16f;
        float tooltipH = _container.LayoutBox.Height > 0 ? _container.LayoutBox.Height : 24f;

        float x, y;

        // Try right of target
        if (targetBounds.Right + 4 + tooltipW <= viewW)
        {
            x = targetBounds.Right + 4;
            y = targetBounds.Y;
        }
        // Try left of target
        else if (targetBounds.X - 4 - tooltipW >= 0)
        {
            x = targetBounds.X - 4 - tooltipW;
            y = targetBounds.Y;
        }
        // Try below target
        else if (targetBounds.Bottom + 4 + tooltipH <= viewH)
        {
            x = targetBounds.X;
            y = targetBounds.Bottom + 4;
        }
        // Fall back to above target
        else
        {
            x = targetBounds.X;
            y = Math.Max(0, targetBounds.Y - 4 - tooltipH);
        }

        _container.InlineStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"position: absolute; left: {x:F0}px; top: {y:F0}px; padding: 4px 8px; border-radius: 4px; background-color: rgba(0,0,0,0.85); z-index: 10000; pointer-events: none");

        if (_container.Parent != root)
        {
            _container.Parent?.RemoveChild(_container);
            root.AddChild(_container);
        }
        root.MarkDirty();
    }

    private void Hide()
    {
        _container.Parent?.RemoveChild(_container);
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
        _container.Parent?.RemoveChild(_container);
        _mouseEnterHandler = null;
        _mouseLeaveHandler = null;
        _target = null;
    }
}
