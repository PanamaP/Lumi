namespace Lumi.Core.Components;

/// <summary>
/// A scrollable list component that displays a column of styled rows.
/// </summary>
public class LumiList
{
    private readonly BoxElement _container;
    private List<string> _items = [];

    public Element Root => _container;

    public Action<int>? OnItemClick { get; set; }

    public List<string> Items
    {
        get => _items;
        set
        {
            _items = value ?? [];
            RebuildItems();
        }
    }

    public LumiList()
    {
        _container = new BoxElement("div");
        ComponentStyles.ApplyListContainer(_container);
    }

    private void RebuildItems()
    {
        _container.ClearChildren();

        for (int i = 0; i < _items.Count; i++)
        {
            var idx = i;
            var row = new BoxElement("div");
            ComponentStyles.ApplyListRow(row);

            var text = new TextElement(_items[i]);
            text.InlineStyle = $"color: {ComponentStyles.ToRgba(ComponentStyles.TextColor)}; font-size: 14px";
            row.AddChild(text);

            row.On("click", (_, _) => OnItemClick?.Invoke(idx));

            _container.AddChild(row);
        }
    }
}
