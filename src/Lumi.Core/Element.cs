using Lumi.Core.Accessibility;
using Lumi.Core.DragDrop;

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
    private string? _id;
    public string? Id
    {
        get => _id;
        set
        {
            var old = _id;
            _id = value;
            _index?.OnIdChanged(this, old, value);
        }
    }

    private ClassList _classes;
    public ClassList Classes
    {
        get => _classes;
        set
        {
            if (_classes == value) return;
            // Unregister old classes from index
            if (_index != null)
            {
                foreach (var cls in _classes)
                    _index.OnClassRemoved(this, cls);
            }
            _classes.Owner = null;
            _classes = value ?? new ClassList();
            _classes.Owner = this;
            // Register new classes with index
            if (_index != null)
            {
                foreach (var cls in _classes)
                    _index.OnClassAdded(this, cls);
            }
        }
    }

    public Element? Parent { get; internal set; }

    /// <summary>
    /// Internal reference to the element index for O(1) lookups.
    /// Set by ElementIndex when this element is part of an indexed tree.
    /// </summary>
    internal ElementIndex? _index;

    protected Element()
    {
        _classes = new ClassList { Owner = this };
    }

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

    // Drag and drop
    public bool IsDraggable { get; set; } = false;
    public event Action<DragData>? OnDragStart;
    public event Action<DragData>? OnDragOver;
    public event Action<DragData>? OnDragEnter;
    public event Action<DragData>? OnDragLeave;
    public event Action<DragData>? OnDrop;
    public event Action? OnDragEnd;

    public string? InlineStyle { get; set; }

    /// <summary>
    /// Theme CSS custom-property declarations applied at stylesheet level
    /// (lower specificity than inline styles). Set by <c>ThemeManager</c>.
    /// </summary>
    public Dictionary<string, string>? ThemeVariables { get; set; }

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

    /// <summary>
    /// Remove all event handlers from this element.
    /// Call before discarding an element to break closure references and prevent leaks.
    /// </summary>
    public void RemoveAllEventHandlers()
    {
        _eventHandlers.Clear();
        OnDragStart = null;
        OnDragOver = null;
        OnDragEnter = null;
        OnDragLeave = null;
        OnDrop = null;
        OnDragEnd = null;
    }

    internal void RaiseDragStart(DragData data) => OnDragStart?.Invoke(data);
    internal void RaiseDragOver(DragData data) => OnDragOver?.Invoke(data);
    internal void RaiseDragEnter(DragData data) => OnDragEnter?.Invoke(data);
    internal void RaiseDragLeave(DragData data) => OnDragLeave?.Invoke(data);
    internal void RaiseDrop(DragData data) => OnDrop?.Invoke(data);
    internal void RaiseDragEnd() => OnDragEnd?.Invoke();

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
        _index?.Register(child);
        MarkDirty();
    }

    public void RemoveChild(Element child)
    {
        if (_children.Remove(child))
        {
            _index?.Unregister(child);
            child.Parent = null;
            MarkDirty();
        }
    }

    public void InsertChild(int index, Element child)
    {
        child.Parent = this;
        _children.Insert(index, child);
        _index?.Register(child);
        MarkDirty();
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            _index?.Unregister(child);
            child.Parent = null;
        }
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

    /// <summary>
    /// Create a deep clone of this element and its subtree.
    /// Copies visual properties (Id, Classes, InlineStyle, Attributes) and children.
    /// Resets layout state. Does NOT copy Parent, DataContext, or ElementIndex ref.
    /// </summary>
    public virtual Element DeepClone()
    {
        var clone = CreateCloneInstance();
        CopyBasePropertiesTo(clone);
        foreach (var child in _children)
            clone.AddChild(child.DeepClone());
        return clone;
    }

    /// <summary>
    /// Create an empty instance of the same element type.
    /// Override in subclasses that require constructor arguments.
    /// </summary>
    protected abstract Element CreateCloneInstance();

    /// <summary>
    /// Copy base Element properties to a clone.
    /// </summary>
    protected void CopyBasePropertiesTo(Element clone)
    {
        clone._id = _id;
        foreach (var cls in _classes)
            clone._classes.Add(cls);
        clone.InlineStyle = InlineStyle;
        if (ThemeVariables != null)
            clone.ThemeVariables = new Dictionary<string, string>(ThemeVariables);
        foreach (var kvp in Attributes)
            clone.Attributes[kvp.Key] = kvp.Value;
        clone.IsFocusable = IsFocusable;
        clone.TabIndex = TabIndex;
    }

    public override string ToString() =>
        $"<{TagName}" +
        (Id != null ? $" id=\"{Id}\"" : "") +
        (Classes.Count > 0 ? $" class=\"{string.Join(' ', Classes)}\"" : "") +
        ">";
}
