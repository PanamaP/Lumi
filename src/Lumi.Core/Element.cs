using Lumi.Core.Accessibility;

namespace Lumi.Core;

/// <summary>
/// Delegate for measuring an element's intrinsic size.
/// Used by the layout engine to query text/image dimensions.
/// </summary>
public delegate (float Width, float Height) ElementMeasureDelegate(Element element, float availableWidth, float availableHeight);

/// <summary>
/// Collects dirty regions from the element tree for partial repaint.
/// </summary>
public sealed class DirtyRegionTracker
{
    private readonly List<LayoutBox> _dirtyRects = [];

    public IReadOnlyList<LayoutBox> DirtyRects => _dirtyRects;
    public bool HasDirtyRegions => _dirtyRects.Count > 0;

    /// <summary>
    /// Record an element's absolute bounding box as dirty.
    /// </summary>
    public void Add(LayoutBox absoluteBox)
    {
        if (absoluteBox.Width <= 0 || absoluteBox.Height <= 0) return;
        _dirtyRects.Add(absoluteBox);
    }

    /// <summary>
    /// Clear all tracked dirty regions after a repaint.
    /// </summary>
    public void Clear() => _dirtyRects.Clear();

    /// <summary>
    /// Returns the fraction of the total surface covered by dirty regions.
    /// Used to decide whether partial vs full repaint is more efficient.
    /// </summary>
    public float CoverageRatio(int surfaceWidth, int surfaceHeight)
    {
        if (surfaceWidth <= 0 || surfaceHeight <= 0 || _dirtyRects.Count == 0)
            return 0f;

        float totalArea = 0;
        foreach (var r in _dirtyRects)
            totalArea += r.Width * r.Height;

        return totalArea / (surfaceWidth * surfaceHeight);
    }
}

/// <summary>
/// Base class for all elements in the Lumi visual tree.
/// </summary>
public abstract class Element
{
    public string? Id { get; set; }
    public List<string> Classes { get; set; } = [];
    public Element? Parent { get; internal set; }

    private readonly List<Element> _children = [];
    public IReadOnlyList<Element> Children => _children;

    public AccessibilityInfo Accessibility { get; } = new();
    public ComputedStyle ComputedStyle { get; internal set; } = new();
    public LayoutBox LayoutBox { get; set; }

    /// <summary>
    /// The element's previous layout box, used for dirty region tracking.
    /// When an element moves or resizes, both old and new positions are dirty.
    /// </summary>
    public LayoutBox PreviousLayoutBox { get; set; }

    public bool IsDirty { get; set; } = true;
    public bool IsLayoutDirty { get; set; } = true;

    // Scroll state
    public float ScrollTop { get; set; } = 0;
    public float ScrollLeft { get; set; } = 0;
    public float ScrollHeight { get; set; } = 0;
    public float ScrollWidth { get; set; } = 0;

    // Focus/keyboard navigation
    public bool IsFocusable { get; set; } = false;
    public int TabIndex { get; set; } = 0;
    public bool IsFocused { get; set; } = false;

    public string? InlineStyle { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = [];
    public bool IsVisible => ComputedStyle.Display != DisplayMode.None;

    /// <summary>
    /// Data context for data binding. When null, the effective context is
    /// inherited from the nearest ancestor with a non-null DataContext.
    /// </summary>
    public object? DataContext { get; set; }

    private readonly Dictionary<string, List<RoutedEventHandler>> _eventHandlers = new(StringComparer.OrdinalIgnoreCase);

    public void On(string eventName, RoutedEventHandler handler)
    {
        if (!_eventHandlers.TryGetValue(eventName, out var list))
        {
            list = [];
            _eventHandlers[eventName] = list;
        }
        list.Add(handler);
    }

    public void Off(string eventName, RoutedEventHandler handler)
    {
        if (_eventHandlers.TryGetValue(eventName, out var list))
            list.Remove(handler);
    }

    internal void RaiseEvent(RoutedEvent e)
    {
        // Standard handlers only fire during Direct and Bubble phases,
        // matching web browser addEventListener default behavior.
        // Tunnel/capture handlers can be added later via OnCapture() if needed.
        if (e.Phase == RoutingPhase.Tunnel) return;

        if (_eventHandlers.TryGetValue(e.Name, out var list))
        {
            foreach (var handler in list)
            {
                handler(this, e);
                if (e.Handled) break;
            }
        }
    }

    public void AddChild(Element child)
    {
        child.Parent = this;
        _children.Add(child);
        MarkDirty();
    }

    public void RemoveChild(Element child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            MarkDirty();
        }
    }

    public void InsertChild(int index, Element child)
    {
        child.Parent = this;
        _children.Insert(index, child);
        MarkDirty();
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
            child.Parent = null;
        _children.Clear();
        MarkDirty();
    }

    public void MarkDirty()
    {
        IsDirty = true;
        IsLayoutDirty = true;
        Parent?.MarkDirty();
    }

    /// <summary>
    /// Scroll to an absolute position, clamped to valid range.
    /// </summary>
    public void ScrollTo(float x, float y)
    {
        float maxScrollLeft = Math.Max(0, ScrollWidth - LayoutBox.Width);
        float maxScrollTop = Math.Max(0, ScrollHeight - LayoutBox.Height);
        ScrollLeft = Math.Clamp(x, 0, maxScrollLeft);
        ScrollTop = Math.Clamp(y, 0, maxScrollTop);
        MarkDirty();
    }

    /// <summary>
    /// Scroll by a relative delta, clamped to valid range.
    /// </summary>
    public void ScrollBy(float dx, float dy)
    {
        ScrollTo(ScrollLeft + dx, ScrollTop + dy);
    }

    /// <summary>
    /// Tag name of this element (e.g. "div", "span", "button").
    /// </summary>
    public abstract string TagName { get; }

    public override string ToString() =>
        $"<{TagName}" +
        (Id != null ? $" id=\"{Id}\"" : "") +
        (Classes.Count > 0 ? $" class=\"{string.Join(' ', Classes)}\"" : "") +
        ">";
}
