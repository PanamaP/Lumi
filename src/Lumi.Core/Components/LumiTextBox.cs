namespace Lumi.Core.Components;

/// <summary>
/// A text input component with optional label.
/// </summary>
public class LumiTextBox
{
    private readonly BoxElement _container;
    private readonly TextElement? _labelElement;
    private readonly InputElement _input;
    private string? _label;

    public Element Root => _container;

    public Action<string>? OnValueChanged { get; set; }

    public string? Label
    {
        get => _label;
        set
        {
            _label = value;
            if (_labelElement != null)
                _labelElement.Text = value ?? "";
        }
    }

    public string Value
    {
        get => _input.Value;
        set
        {
            _input.Value = value;
            _input.MarkDirty();
        }
    }

    public string Placeholder
    {
        get => _input.Placeholder;
        set
        {
            _input.Placeholder = value;
            _input.MarkDirty();
        }
    }

    public bool IsReadOnly { get; set; }

    public InputElement InputElement => _input;

    public LumiTextBox()
    {
        _container = new BoxElement("div");
        ComponentStyles.ApplyContainer(_container);
        ComponentStyles.AppendStyle(_container, "padding: 0px 0px 8px 0px");

        // Label
        _labelElement = new TextElement();
        ComponentStyles.ApplyLabel(_labelElement);
        ComponentStyles.AppendStyle(_labelElement, "padding: 0px 0px 4px 0px");
        _container.AddChild(_labelElement);

        // Input
        _input = new InputElement { InputType = "text" };
        ComponentStyles.ApplyTextInput(_input);
        _container.AddChild(_input);

        _input.On("input", OnInputHandler);
    }

    private void OnInputHandler(Element sender, RoutedEvent e)
    {
        if (IsReadOnly) return;
        OnValueChanged?.Invoke(_input.Value);
    }
}
