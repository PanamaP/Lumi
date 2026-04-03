using Lumi.Core;

namespace Lumi.Input;

/// <summary>
/// Tracks element interaction states (hover, active, focus) for pseudo-class resolution.
/// </summary>
public class InteractionState
{
    public Element? HoveredElement { get; private set; }
    public Element? ActiveElement { get; private set; }
    public Element? FocusedElement { get; private set; }

    private readonly HashSet<Element> _hoveredPath = [];
    private readonly HashSet<Element> _focusedPath = [];

    public bool IsHovered(Element element) => _hoveredPath.Contains(element);
    public bool IsActive(Element element) => ActiveElement == element;
    public bool IsFocused(Element element) => _focusedPath.Contains(element);

    public void SetHovered(Element? element)
    {
        if (HoveredElement == element) return;
        HoveredElement = element;
        _hoveredPath.Clear();
        var current = element;
        while (current != null)
        {
            _hoveredPath.Add(current);
            current = current.Parent;
        }
    }

    public void SetActive(Element? element) => ActiveElement = element;

    public void SetFocused(Element? element)
    {
        if (FocusedElement == element) return;
        FocusedElement = element;
        _focusedPath.Clear();
        var current = element;
        while (current != null)
        {
            _focusedPath.Add(current);
            current = current.Parent;
        }
    }
}
