namespace Lumi.Core.Components;

/// <summary>
/// A range slider component with track, fill, and thumb.
/// </summary>
public class LumiSlider
{
    private readonly BoxElement _container;
    private readonly BoxElement _track;
    private readonly BoxElement _fill;
    private readonly BoxElement _thumb;
    private float _value;
    private float _min;
    private float _max = 1f;
    private bool _isDragging;
    private const float TrackWidth = 200f;
    private const float ThumbSize = 20f;

    public Element Root => _container;

    public Action<float>? OnValueChanged { get; set; }

    public float Value
    {
        get => _value;
        set
        {
            _value = ClampValue(value);
            UpdateVisual();
        }
    }

    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            _value = ClampValue(_value);
            UpdateVisual();
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            _value = ClampValue(_value);
            UpdateVisual();
        }
    }

    public LumiSlider()
    {
        // Container holds track + thumb. Position: relative so thumb can be absolute.
        _container = new BoxElement("div");
        _container.InlineStyle = $"display: flex; flex-direction: column; padding: 8px 0px; width: {TrackWidth:F0}px; position: relative";

        // Track background (full width)
        _track = new BoxElement("div");
        _track.InlineStyle = $"height: 8px; width: {TrackWidth:F0}px; " +
                             $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Border)}; border-radius: 4px; " +
                             $"overflow: hidden";
        _container.AddChild(_track);

        // Fill (proportional width inside track)
        _fill = new BoxElement("div");
        _fill.InlineStyle = $"height: 8px; background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; width: 100px";
        _track.AddChild(_fill);

        // Thumb (absolutely positioned relative to container)
        _thumb = new BoxElement("div");
        _thumb.InlineStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"width: {ThumbSize:F0}px; height: {ThumbSize:F0}px; " +
            $"background-color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; " +
            $"border-radius: {ThumbSize / 2:F0}px; position: absolute; " +
            $"top: 2px; left: {TrackWidth / 2 - ThumbSize / 2:F1}px");
        _container.AddChild(_thumb);

        // Interaction handlers
        _container.On("mousedown", OnMouseDown);
        _container.On("mousemove", OnMouseMove);
        _container.On("mouseup", OnMouseUp);

        UpdateVisual();
    }

    private void OnMouseDown(Element sender, RoutedEvent e)
    {
        _isDragging = true;
        if (e is RoutedMouseEvent me)
            UpdateValueFromPosition(me.X);
    }

    private void OnMouseMove(Element sender, RoutedEvent e)
    {
        if (!_isDragging) return;
        if (e is RoutedMouseEvent me)
            UpdateValueFromPosition(me.X);
    }

    private void OnMouseUp(Element sender, RoutedEvent e)
    {
        _isDragging = false;
    }

    private void UpdateValueFromPosition(float mouseX)
    {
        float trackLeft = _track.LayoutBox.X;
        float trackWidth = _track.LayoutBox.Width;
        if (trackWidth <= 0) trackWidth = TrackWidth;

        float ratio = Math.Clamp((mouseX - trackLeft) / trackWidth, 0f, 1f);
        float newValue = _min + ratio * (_max - _min);
        _value = ClampValue(newValue);
        UpdateVisual();
        OnValueChanged?.Invoke(_value);
    }

    private float ClampValue(float v) => Math.Clamp(v, _min, _max);

    private float NormalizedValue => (_max > _min) ? (_value - _min) / (_max - _min) : 0f;

    private void UpdateVisual()
    {
        float pct = NormalizedValue;
        float fillPx = pct * TrackWidth;
        float thumbLeft = pct * (TrackWidth - ThumbSize);

        _fill.InlineStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"height: 8px; background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; " +
            $"width: {fillPx:F1}px");

        _thumb.InlineStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"width: {ThumbSize:F0}px; height: {ThumbSize:F0}px; " +
            $"background-color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; " +
            $"border-radius: {ThumbSize / 2:F0}px; position: absolute; " +
            $"top: 2px; left: {thumbLeft:F1}px");

        _container.MarkDirty();
    }
}
