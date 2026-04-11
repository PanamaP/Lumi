namespace Lumi.Core.Components;

/// <summary>
/// A progress bar indicator with determinate and indeterminate modes.
/// </summary>
public class LumiProgressBar
{
    private readonly BoxElement _container;
    private readonly BoxElement _fill;
    private float _value;
    private bool _isIndeterminate;

    public Element Root => _container;

    public float Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0f, 1f); UpdateVisual(); }
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set { _isIndeterminate = value; UpdateVisual(); }
    }

    public LumiProgressBar()
    {
        _container = new BoxElement("div");
        ComponentStyles.ApplyProgressTrack(_container);

        _fill = new BoxElement("div");
        _fill.InlineStyle = $"height: 8px; background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; " +
                            $"border-radius: 4px; width: 0px";
        _container.AddChild(_fill);
    }

    private void UpdateVisual()
    {
        if (_isIndeterminate)
        {
            _fill.InlineStyle = $"height: 8px; background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; " +
                                $"border-radius: 4px; width: 100%; opacity: 0.7";
        }
        else
        {
            var widthPercent = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"{_value * 100f:F1}%");
            _fill.InlineStyle = $"height: 8px; background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; " +
                                $"border-radius: 4px; width: {widthPercent}";
        }
        _container.MarkDirty();
    }
}
