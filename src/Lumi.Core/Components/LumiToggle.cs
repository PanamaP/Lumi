namespace Lumi.Core.Components;

/// <summary>
/// A toggle/switch control with an optional label.
/// </summary>
public class LumiToggle
{
    private readonly BoxElement _container;
    private readonly BoxElement _track;
    private readonly BoxElement _thumb;
    private readonly TextElement _labelElement;
    private bool _isOn;
    private string? _label;

    private const float TrackWidth = 44f;
    private const float TrackHeight = 24f;
    private const float ThumbSize = 20f;
    private const float ThumbMargin = 2f;

    public Element Root => _container;
    public Action<bool>? OnToggle { get; set; }

    public bool IsOn
    {
        get => _isOn;
        set { _isOn = value; UpdateVisual(); }
    }

    public string? Label
    {
        get => _label;
        set
        {
            _label = value;
            _labelElement.Text = value ?? "";
            var display = string.IsNullOrEmpty(value) ? "none" : "block";
            _labelElement.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; " +
                                        $"padding: 0px 0px 0px 8px; font-size: 14px; display: {display}";
            _container.MarkDirty();
        }
    }

    public LumiToggle()
    {
        _container = new BoxElement("div");
        _container.InlineStyle = "display: flex; flex-direction: row; align-items: center; cursor: pointer";

        _track = new BoxElement("div");
        ComponentStyles.ApplyToggleTrack(_track, false);
        _container.AddChild(_track);

        _thumb = new BoxElement("div");
        UpdateThumbStyle();
        _track.AddChild(_thumb);

        _labelElement = new TextElement();
        _labelElement.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; " +
                                    $"padding: 0px 0px 0px 8px; font-size: 14px; display: none";
        _container.AddChild(_labelElement);

        _container.On("click", OnClickHandler);
    }

    private void OnClickHandler(Element sender, RoutedEvent e)
    {
        _isOn = !_isOn;
        UpdateVisual();
        OnToggle?.Invoke(_isOn);
    }

    private void UpdateVisual()
    {
        ComponentStyles.ApplyToggleTrack(_track, _isOn);
        UpdateThumbStyle();
        _container.MarkDirty();
    }

    private void UpdateThumbStyle()
    {
        float left = _isOn ? TrackWidth - ThumbSize - ThumbMargin : ThumbMargin;
        _thumb.InlineStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"width: {ThumbSize:F0}px; height: {ThumbSize:F0}px; border-radius: {ThumbSize / 2:F0}px; " +
            $"background-color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; " +
            $"position: absolute; top: {ThumbMargin:F0}px; left: {left:F0}px");
    }
}
