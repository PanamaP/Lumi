namespace Lumi.Core.Components;

/// <summary>
/// A modal dialog component with title bar, close button, content area, and backdrop overlay.
/// </summary>
public class LumiDialog
{
    private readonly BoxElement _overlay;
    private readonly BoxElement _panel;
    private readonly BoxElement _titleBar;
    private readonly TextElement _titleText;
    private readonly BoxElement _closeButton;
    private readonly BoxElement _contentArea;
    private string _title = "";
    private Element? _content;
    private bool _isOpen;

    public Element Root => _overlay;

    public Action? OnClose { get; set; }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            _titleText.Text = value;
            _titleText.MarkDirty();
        }
    }

    public Element? Content
    {
        get => _content;
        set
        {
            if (_content != null)
                _contentArea.RemoveChild(_content);
            _content = value;
            if (_content != null)
                _contentArea.AddChild(_content);
        }
    }

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            _isOpen = value;
            var display = _isOpen ? "flex" : "none";
            // Re-apply overlay style with updated display
            var o = ComponentStyles.Overlay;
            _overlay.InlineStyle = $"position: fixed; top: 0px; left: 0px; right: 0px; bottom: 0px; " +
                                   $"background-color: {ComponentStyles.ToRgba(o)}; display: {display}; " +
                                   $"justify-content: center; align-items: center; z-index: 1000";
            _overlay.MarkDirty();
        }
    }

    public LumiDialog()
    {
        // Overlay / backdrop (starts hidden)
        _overlay = new BoxElement("div");
        var o = ComponentStyles.Overlay;
        _overlay.InlineStyle = $"position: fixed; top: 0px; left: 0px; right: 0px; bottom: 0px; " +
                               $"background-color: {ComponentStyles.ToRgba(o)}; display: none; " +
                               $"justify-content: center; align-items: center; z-index: 1000";

        // Dialog panel
        _panel = new BoxElement("div");
        ComponentStyles.ApplyDialogPanel(_panel);
        _overlay.AddChild(_panel);

        // Title bar
        _titleBar = new BoxElement("div");
        _titleBar.InlineStyle = $"display: flex; flex-direction: row; justify-content: space-between; " +
                                $"align-items: center; padding: 12px 16px; border-width: 0px 0px 1px 0px; " +
                                $"border-color: {ComponentStyles.ToRgba(ComponentStyles.Border)}";
        _panel.AddChild(_titleBar);

        _titleText = new TextElement();
        _titleText.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; font-size: 16px; font-weight: 600";
        _titleBar.AddChild(_titleText);

        // Close button
        _closeButton = new BoxElement("button");
        _closeButton.InlineStyle = "display: flex; justify-content: center; align-items: center; " +
                                   "width: 28px; height: 28px; border-radius: 4px; background-color: transparent; cursor: pointer";
        _titleBar.AddChild(_closeButton);

        var closeX = new TextElement("✕");
        closeX.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.Subtle)}; font-size: 14px";
        _closeButton.AddChild(closeX);

        _closeButton.On("click", OnCloseClick);

        // Content area
        _contentArea = new BoxElement("div");
        _contentArea.InlineStyle = "padding: 16px; flex-grow: 1";
        _panel.AddChild(_contentArea);
    }

    private void OnCloseClick(Element sender, RoutedEvent e)
    {
        IsOpen = false;
        OnClose?.Invoke();
    }
}
