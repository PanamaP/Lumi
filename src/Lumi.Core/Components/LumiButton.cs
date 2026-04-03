namespace Lumi.Core.Components;

/// <summary>
/// A styled button component with text and variant support.
/// </summary>
public class LumiButton
{
    private readonly BoxElement _root;
    private readonly TextElement _label;
    private string _text = "";
    private bool _isDisabled;
    private ButtonVariant _variant = ButtonVariant.Primary;

    public Element Root => _root;

    public Action? OnClick { get; set; }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            _label.Text = value;
            _root.MarkDirty();
        }
    }

    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            _isDisabled = value;
            ApplyStyles();
            _root.MarkDirty();
        }
    }

    public ButtonVariant Variant
    {
        get => _variant;
        set
        {
            _variant = value;
            ApplyStyles();
            _root.MarkDirty();
        }
    }

    public LumiButton()
    {
        _root = new BoxElement("button");
        _label = new TextElement();
        _root.AddChild(_label);

        _root.On("click", OnClickHandler);

        ApplyStyles();
    }

    private void OnClickHandler(Element sender, RoutedEvent e)
    {
        if (_isDisabled)
        {
            e.Handled = true;
            return;
        }
        OnClick?.Invoke();
    }

    private void ApplyStyles()
    {
        ComponentStyles.ApplyButton(_root, _variant);
        _label.InlineStyle = $"color: {ComponentStyles.ToRgba(_variant == ButtonVariant.Primary ? new Color(15, 23, 42, 255) : ComponentStyles.TextColor)}; font-size: 14px";

        if (_isDisabled)
            ComponentStyles.ApplyDisabledButton(_root);
    }
}
