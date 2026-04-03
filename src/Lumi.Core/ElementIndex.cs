namespace Lumi.Core;

/// <summary>
/// Maintains dictionary-based indexes over an element tree for O(1) lookups
/// by ID and CSS class name. Automatically stays in sync with tree mutations
/// and element property changes via internal hooks in Element.
/// </summary>
public sealed class ElementIndex
{
    private readonly Dictionary<string, Element> _idMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<Element>> _classMap = new(StringComparer.Ordinal);

    /// <summary>
    /// The root element this index is attached to, or null if detached.
    /// </summary>
    public Element? Root { get; private set; }

    /// <summary>
    /// Attach this index to an element tree. Recursively registers
    /// every element in the subtree for O(1) lookups.
    /// </summary>
    public void AttachTo(Element root)
    {
        Detach();
        Root = root;
        RegisterSubtree(root);
    }

    /// <summary>
    /// Detach from the current tree, clearing all index data.
    /// </summary>
    public void Detach()
    {
        if (Root != null)
        {
            UnregisterSubtree(Root);
            Root = null;
        }
        _idMap.Clear();
        _classMap.Clear();
    }

    /// <summary>
    /// Find an element by its ID. Returns null if not found.
    /// O(1) dictionary lookup.
    /// </summary>
    public Element? FindById(string id)
    {
        return _idMap.TryGetValue(id, out var element) ? element : null;
    }

    /// <summary>
    /// Find all elements with the given CSS class.
    /// O(1) dictionary lookup + O(m) to copy results.
    /// </summary>
    public List<Element> FindByClass(string className)
    {
        if (_classMap.TryGetValue(className, out var set))
            return [.. set];
        return [];
    }

    /// <summary>
    /// Register an element and its entire subtree with this index.
    /// Called when elements are added to an indexed tree.
    /// </summary>
    internal void Register(Element element)
    {
        RegisterSubtree(element);
    }

    /// <summary>
    /// Unregister an element and its entire subtree from this index.
    /// Called when elements are removed from an indexed tree.
    /// </summary>
    internal void Unregister(Element element)
    {
        UnregisterSubtree(element);
    }

    /// <summary>
    /// Called when an element's Id property changes.
    /// </summary>
    internal void OnIdChanged(Element element, string? oldId, string? newId)
    {
        if (oldId != null && _idMap.TryGetValue(oldId, out var existing) && existing == element)
            _idMap.Remove(oldId);

        if (newId != null)
            _idMap[newId] = element;
    }

    /// <summary>
    /// Called when a class is added to an element's ClassList.
    /// </summary>
    internal void OnClassAdded(Element element, string className)
    {
        if (!_classMap.TryGetValue(className, out var set))
        {
            set = [];
            _classMap[className] = set;
        }
        set.Add(element);
    }

    /// <summary>
    /// Called when a class is removed from an element's ClassList.
    /// </summary>
    internal void OnClassRemoved(Element element, string className)
    {
        if (_classMap.TryGetValue(className, out var set))
        {
            set.Remove(element);
            if (set.Count == 0)
                _classMap.Remove(className);
        }
    }

    private void RegisterSubtree(Element element)
    {
        element._index = this;
        RegisterSingle(element);

        foreach (var child in element.Children)
            RegisterSubtree(child);
    }

    private void UnregisterSubtree(Element element)
    {
        element._index = null;
        UnregisterSingle(element);

        foreach (var child in element.Children)
            UnregisterSubtree(child);
    }

    private void RegisterSingle(Element element)
    {
        if (element.Id != null)
            _idMap[element.Id] = element;

        foreach (var cls in element.Classes)
        {
            if (!_classMap.TryGetValue(cls, out var set))
            {
                set = [];
                _classMap[cls] = set;
            }
            set.Add(element);
        }
    }

    private void UnregisterSingle(Element element)
    {
        if (element.Id != null && _idMap.TryGetValue(element.Id, out var existing) && existing == element)
            _idMap.Remove(element.Id);

        foreach (var cls in element.Classes)
        {
            if (_classMap.TryGetValue(cls, out var set))
            {
                set.Remove(element);
                if (set.Count == 0)
                    _classMap.Remove(cls);
            }
        }
    }
}
