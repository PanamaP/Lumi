namespace Lumi.Core.Components;

/// <summary>
/// Tab navigation control with content panels.
/// </summary>
public class LumiTabControl
{
    private readonly BoxElement _container;
    private readonly BoxElement _headerRow;
    private readonly BoxElement _contentArea;
    private readonly List<(BoxElement header, TextElement text, Element content)> _tabs = [];
    private int _selectedIndex = -1;

    public Element Root => _container;
    public Action<int>? OnTabChanged { get; set; }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= _tabs.Count) return;
            _selectedIndex = value;
            UpdateVisuals();
        }
    }

    public LumiTabControl()
    {
        _container = new BoxElement("div");
        _container.InlineStyle = "display: flex; flex-direction: column";

        _headerRow = new BoxElement("div");
        ComponentStyles.ApplyTabHeader(_headerRow);
        _container.AddChild(_headerRow);

        _contentArea = new BoxElement("div");
        _contentArea.InlineStyle = $"display: flex; flex-direction: column; padding: 8px; " +
                                   $"background-color: {ComponentStyles.ToRgba(ComponentStyles.Background)}";
        _container.AddChild(_contentArea);
    }

    public void AddTab(string title, Element content)
    {
        var idx = _tabs.Count;

        var header = new BoxElement("div");
        header.InlineStyle = $"padding: 8px 16px; cursor: pointer; " +
                             $"border-width: 0px 0px 2px 0px; border-color: transparent; " +
                             $"color: {ComponentStyles.ToRgba(ComponentStyles.Subtle)}";
        _headerRow.AddChild(header);

        var text = new TextElement(title);
        text.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.Subtle)}; font-size: 14px";
        header.AddChild(text);

        content.InlineStyle = content.InlineStyle != null
            ? content.InlineStyle + "; display: none"
            : "display: none";
        ComponentStyles.SetVisible(content, false);
        _contentArea.AddChild(content);

        _tabs.Add((header, text, content));

        header.On("click", (_, _) =>
        {
            _selectedIndex = idx;
            UpdateVisuals();
            OnTabChanged?.Invoke(idx);
        });

        if (_tabs.Count == 1)
        {
            _selectedIndex = 0;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            var (header, text, content) = _tabs[i];
            bool active = i == _selectedIndex;

            var borderColor = active
                ? ComponentStyles.ToRgba(ComponentStyles.Accent)
                : "transparent";
            var textColor = active ? ComponentStyles.TextColor : ComponentStyles.Subtle;

            header.InlineStyle = $"padding: 8px 16px; cursor: pointer; " +
                                 $"border-width: 0px 0px 2px 0px; border-color: {borderColor}";
            text.InlineStyle = $"color: {ComponentStyles.ToRgba(textColor)}; font-size: 14px";

            ComponentStyles.SetVisible(content, active);
        }
        _container.MarkDirty();
    }
}
