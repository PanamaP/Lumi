using Lumi.Core.Time;

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
            CursorPosition = Math.Clamp(CursorPosition, 0, _value.Length);
            SelectionStart = Math.Clamp(SelectionStart, 0, _value.Length);
            SelectionEnd = Math.Clamp(SelectionEnd, 0, _value.Length);
            ValueChanged?.Invoke(value);
        }
    }

    public string Placeholder { get; set; } = "";
    public bool IsDisabled { get; set; }
    public bool IsChecked { get; set; }

    /// <summary>
    /// 0-based cursor index within <see cref="Value"/>.
    /// </summary>
    public int CursorPosition { get; set; }

    /// <summary>
    /// Start of the text selection range.
    /// </summary>
    public int SelectionStart { get; set; }

    /// <summary>
    /// End of the text selection range.
    /// </summary>
    public int SelectionEnd { get; set; }

    /// <summary>
    /// True when a non-empty selection exists.
    /// </summary>
    public bool HasSelection => SelectionStart != SelectionEnd;

    /// <summary>
    /// Tick count (ms) of the last edit or cursor movement, used for caret blink.
    /// </summary>
    public long LastEditTick { get; set; }

    /// <summary>
    /// Raised when the Value property changes, enabling two-way data binding.
    /// </summary>
    public event Action<string>? ValueChanged;

    public InputElement()
    {
        IsFocusable = true;
    }

    /// <summary>
    /// Delete the currently selected text and place the cursor at the selection start.
    /// </summary>
    public void DeleteSelection()
    {
        if (!HasSelection) return;
        int lo = Math.Min(SelectionStart, SelectionEnd);
        int hi = Math.Max(SelectionStart, SelectionEnd);
        Value = Value[..lo] + Value[hi..];
        CursorPosition = lo;
        ClearSelection();
    }

    /// <summary>
    /// Collapse the selection so that Start == End == <see cref="CursorPosition"/>.
    /// </summary>
    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }

    /// <summary>
    /// Reset the caret blink timer (should be called on every keystroke / cursor move).
    /// </summary>
    public void ResetBlink()
    {
        LastEditTick = TimeSource.Default.TickCount64;
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
        clone.CursorPosition = CursorPosition;
        clone.SelectionStart = SelectionStart;
        clone.SelectionEnd = SelectionEnd;
        clone.LastEditTick = LastEditTick;
        return clone;
    }
}
