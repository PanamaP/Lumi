namespace Lumi.Core.Components;

/// <summary>
/// A dropdown select component with a toggleable item list.
/// </summary>
public class LumiDropdown
{
    private readonly BoxElement _container;
    private readonly BoxElement _button;
    private readonly TextElement _buttonText;
    private readonly BoxElement _listContainer;
    private List<string> _items = [];
    private int _selectedIndex = -1;
    private bool _isOpen;

    public Element Root => _container;

    public Action<int>? OnSelectionChanged { get; set; }

    public List<string> Items
    {
        get => _items;
        set
        {
            _items = value ?? [];
            RebuildList();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            UpdateButtonText();
        }
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            _isOpen = value;
            if (_isOpen)
            {
                var root = ComponentStyles.FindRoot(_container);
                var buttonBounds = ComponentStyles.GetAbsoluteBounds(_button);
                _listContainer.InlineStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"display: flex; flex-direction: column; position: absolute; top: {buttonBounds.Bottom:F0}px; left: {buttonBounds.X:F0}px; width: {buttonBounds.Width:F0}px; background-color: {ComponentStyles.ToRgba(ComponentStyles.Surface)}; border-width: 1px; border-color: {ComponentStyles.ToRgba(ComponentStyles.Border)}; border-radius: 4px; z-index: 10000; overflow: scroll; max-height: 200px");
                if (_listContainer.Parent != root)
                {
                    _listContainer.Parent?.RemoveChild(_listContainer);
                    root.AddChild(_listContainer);
                }
            }
            else
            {
                _listContainer.Parent?.RemoveChild(_listContainer);
            }
            _container.MarkDirty();
        }
    }

    public LumiDropdown()
    {
        _container = new BoxElement("div");
        _container.InlineStyle = "display: flex; flex-direction: column; position: relative";

        // Trigger button
        _button = new BoxElement("button");
        ComponentStyles.ApplyButton(_button, ButtonVariant.Secondary);
        ComponentStyles.AppendStyle(_button, "justify-content: space-between");
        _container.AddChild(_button);

        _buttonText = new TextElement("Select...");
        _buttonText.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; font-size: 14px";
        _button.AddChild(_buttonText);

        var arrow = new TextElement("▼");
        arrow.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.Subtle)}; font-size: 10px; padding: 0px 0px 0px 8px";
        _button.AddChild(arrow);

        // Dropdown list (added to root element when opened, not to container)
        _listContainer = new BoxElement("div");

        _button.On("click", OnButtonClick);
    }

    private void OnButtonClick(Element sender, RoutedEvent e)
    {
        IsOpen = !_isOpen;
    }

    private void UpdateButtonText()
    {
        _buttonText.Text = (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            ? _items[_selectedIndex]
            : "Select...";
        _buttonText.MarkDirty();
    }

    private void RebuildList()
    {
        _listContainer.ClearChildren();

        for (int i = 0; i < _items.Count; i++)
        {
            var idx = i;
            var row = new BoxElement("div");
            ComponentStyles.ApplyListRow(row);
            var bgColor = (i == _selectedIndex) ? ComponentStyles.Accent : ComponentStyles.Surface;
            ComponentStyles.AppendStyle(row, $"background-color: {ComponentStyles.ToRgba(bgColor)}");

            var text = new TextElement(_items[i]);
            var textColor = (i == _selectedIndex) ? new Color(15, 23, 42, 255) : ComponentStyles.TextColor;
            text.InlineStyle = $"color: {ComponentStyles.ToRgba(textColor)}; font-size: 14px";
            row.AddChild(text);

            row.On("click", (_, _) =>
            {
                _selectedIndex = idx;
                UpdateButtonText();
                IsOpen = false;
                RebuildList();
                OnSelectionChanged?.Invoke(idx);
            });

            _listContainer.AddChild(row);
        }

        _container.MarkDirty();
    }
}
