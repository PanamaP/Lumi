namespace Lumi.Core.Components;

/// <summary>
/// A toggle checkbox component with label.
/// </summary>
public class LumiCheckbox
{
    private readonly BoxElement _container;
    private readonly BoxElement _checkBox;
    private readonly BoxElement _checkIndicator;
    private readonly TextElement _labelElement;
    private bool _isChecked;
    private string _label = "";

    public Element Root => _container;

    public Action<bool>? OnChanged { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;
            UpdateCheckVisual();
        }
    }

    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            _labelElement.Text = value;
        }
    }

    public LumiCheckbox()
    {
        _container = new BoxElement("div");
        _container.InlineStyle = "display: flex; flex-direction: row; align-items: center; cursor: pointer; padding: 4px 0px";

        // Check indicator box (outer border)
        _checkBox = new BoxElement("div");
        _checkBox.InlineStyle = $"width: 22px; height: 22px; border-width: 2px; " +
                                $"border-color: {ComponentStyles.ToRgba(ComponentStyles.Border)}; border-radius: 4px; " +
                                $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Background)}; " +
                                $"display: flex; justify-content: center; align-items: center";
        _container.AddChild(_checkBox);

        // Inner filled square (visible when checked)
        _checkIndicator = new BoxElement("div");
        _checkIndicator.InlineStyle = $"width: 12px; height: 12px; border-radius: 2px; " +
                                      $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; display: none";
        _checkBox.AddChild(_checkIndicator);

        // Label
        _labelElement = new TextElement();
        _labelElement.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; padding: 0px 0px 0px 8px";
        _container.AddChild(_labelElement);

        // Click handler on container
        _container.On("click", OnClickHandler);
    }

    private void OnClickHandler(Element sender, RoutedEvent e)
    {
        _isChecked = !_isChecked;
        UpdateCheckVisual();
        OnChanged?.Invoke(_isChecked);
    }

    private void UpdateCheckVisual()
    {
        var display = _isChecked ? "block" : "none";
        var borderColor = _isChecked ? ComponentStyles.Accent : ComponentStyles.Border;

        _checkIndicator.InlineStyle = $"width: 12px; height: 12px; border-radius: 2px; " +
                                      $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; display: {display}";
        _checkBox.InlineStyle = $"width: 22px; height: 22px; border-width: 2px; " +
                                $"border-color: {ComponentStyles.ToRgba(borderColor)}; border-radius: 4px; " +
                                $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Background)}; " +
                                $"display: flex; justify-content: center; align-items: center";
        _container.MarkDirty();
    }
}
