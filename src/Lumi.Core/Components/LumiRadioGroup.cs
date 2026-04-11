namespace Lumi.Core.Components;

/// <summary>
/// A radio button group where only one option can be selected at a time.
/// </summary>
public class LumiRadioGroup : IDisposable
{
    private readonly BoxElement _container;
    private readonly List<BoxElement> _indicators = [];
    private readonly List<string> _options;
    private int _selectedIndex;

    public Element Root => _container;
    public IReadOnlyList<string> Options => _options;
    public Action<int>? OnSelectionChanged { get; set; }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= _options.Count) return;
            _selectedIndex = value;
            UpdateVisuals();
        }
    }

    public LumiRadioGroup(List<string> options)
    {
        _options = new List<string>(options ?? []);
        _container = new BoxElement("div");
        ComponentStyles.ApplyRadioGroup(_container);

        for (int i = 0; i < _options.Count; i++)
        {
            var idx = i;
            var row = new BoxElement("div");
            row.InlineStyle = "display: flex; flex-direction: row; align-items: center; cursor: pointer; padding: 4px 0px";

            var circle = new BoxElement("div");
            circle.InlineStyle = $"width: 16px; height: 16px; border-width: 2px; border-radius: 8px; " +
                                 $"border-color: {ComponentStyles.ToRgba(ComponentStyles.Border)}; " +
                                 $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Background)}; " +
                                 $"display: flex; justify-content: center; align-items: center";
            row.AddChild(circle);

            var indicator = new BoxElement("div");
            indicator.InlineStyle = $"width: 8px; height: 8px; border-radius: 4px; " +
                                    $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; display: none";
            circle.AddChild(indicator);
            _indicators.Add(indicator);

            var label = new TextElement(_options[i]);
            label.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; padding: 0px 0px 0px 8px; font-size: 14px";
            row.AddChild(label);

            row.On("click", (_, _) =>
            {
                _selectedIndex = idx;
                UpdateVisuals();
                OnSelectionChanged?.Invoke(idx);
            });

            _container.AddChild(row);
        }

        if (_options.Count > 0) UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < _indicators.Count; i++)
        {
            var display = i == _selectedIndex ? "block" : "none";
            _indicators[i].InlineStyle = $"width: 8px; height: 8px; border-radius: 4px; " +
                                         $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Accent)}; display: {display}";
            var circle = (BoxElement)_indicators[i].Parent!;
            var borderColor = i == _selectedIndex ? ComponentStyles.Accent : ComponentStyles.Border;
            circle.InlineStyle = $"width: 16px; height: 16px; border-width: 2px; border-radius: 8px; " +
                                 $"border-color: {ComponentStyles.ToRgba(borderColor)}; " +
                                 $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Background)}; " +
                                 $"display: flex; justify-content: center; align-items: center";
        }
        _container.MarkDirty();
    }

    public void Dispose()
    {
        Root.RemoveAllEventHandlers();
    }
}
