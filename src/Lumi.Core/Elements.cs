namespace Lumi.Core;

/// <summary>
/// A generic container element (div, section, nav, etc.).
/// </summary>
public class BoxElement : Element
{
    private readonly string _tagName;
    public override string TagName => _tagName;

    public BoxElement(string tagName = "div")
    {
        _tagName = tagName;
        if (tagName is "button" or "a")
            IsFocusable = true;
    }

    protected override Element CreateCloneInstance() => new BoxElement(_tagName);
}

/// <summary>
/// An element that displays text content.
/// </summary>
public class TextElement : Element
{
    public override string TagName => "span";
    public string Text { get; set; } = "";

    public TextElement() { }
    public TextElement(string text) => Text = text;

    protected override Element CreateCloneInstance() => new TextElement();

    public override Element DeepClone()
    {
        var clone = (TextElement)base.DeepClone();
        clone.Text = Text;
        return clone;
    }
}

/// <summary>
/// An element that displays an image.
/// </summary>
public class ImageElement : Element
{
    public override string TagName => "img";
    public string? Source { get; set; }
    public float NaturalWidth { get; set; }
    public float NaturalHeight { get; set; }

    protected override Element CreateCloneInstance() => new ImageElement();

    public override Element DeepClone()
    {
        var clone = (ImageElement)base.DeepClone();
        clone.Source = Source;
        clone.NaturalWidth = NaturalWidth;
        clone.NaturalHeight = NaturalHeight;
        return clone;
    }
}

/// <summary>
/// An interactive input element.
/// </summary>
public class InputElement : Element
{
    public override string TagName => "input";
    public string InputType { get; set; } = "text";

    private string _value = "";
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            ValueChanged?.Invoke(value);
        }
    }

    public string Placeholder { get; set; } = "";
    public bool IsDisabled { get; set; }
    public bool IsChecked { get; set; }

    /// <summary>
    /// Raised when the Value property changes, enabling two-way data binding.
    /// </summary>
    public event Action<string>? ValueChanged;

    public InputElement()
    {
        IsFocusable = true;
    }

    protected override Element CreateCloneInstance() => new InputElement();

    public override Element DeepClone()
    {
        var clone = (InputElement)base.DeepClone();
        clone.InputType = InputType;
        clone._value = _value;
        clone.Placeholder = Placeholder;
        clone.IsDisabled = IsDisabled;
        clone.IsChecked = IsChecked;
        return clone;
    }
}
