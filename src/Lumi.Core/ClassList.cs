using System.Collections;

namespace Lumi.Core;

/// <summary>
/// An observable, deduplicated list of CSS class names.
/// Backed by both a List (ordered iteration) and HashSet (O(1) Contains).
/// Notifies the owning element's index when classes are added or removed.
/// </summary>
public sealed class ClassList : IList<string>, IReadOnlyList<string>
{
    private readonly List<string> _list = [];
    private readonly HashSet<string> _set = new(StringComparer.Ordinal);

    /// <summary>
    /// The element that owns this class list. Set internally by Element.
    /// </summary>
    internal Element? Owner { get; set; }

    public ClassList() { }

    public ClassList(IEnumerable<string> classes)
    {
        foreach (var cls in classes)
        {
            if (_set.Add(cls))
                _list.Add(cls);
        }
    }

    public int Count => _list.Count;
    public bool IsReadOnly => false;

    public string this[int index]
    {
        get => _list[index];
        set
        {
            var old = _list[index];
            if (old == value) return;

            _set.Remove(old);
            if (!_set.Add(value))
            {
                // New value already exists elsewhere — just remove the old entry
                _list.RemoveAt(index);
                Owner?._index?.OnClassRemoved(Owner, old);
                return;
            }

            _list[index] = value;
            Owner?._index?.OnClassRemoved(Owner, old);
            Owner?._index?.OnClassAdded(Owner, value);
        }
    }

    public bool Contains(string item) => _set.Contains(item);

    public void Add(string item)
    {
        if (!_set.Add(item)) return;
        _list.Add(item);
        Owner?._index?.OnClassAdded(Owner, item);
    }

    public bool Remove(string item)
    {
        if (!_set.Remove(item)) return false;
        _list.Remove(item);
        Owner?._index?.OnClassRemoved(Owner, item);
        return true;
    }

    public void Clear()
    {
        if (_list.Count == 0) return;
        var old = _list.ToArray();
        _list.Clear();
        _set.Clear();
        foreach (var cls in old)
            Owner?._index?.OnClassRemoved(Owner, cls);
    }

    /// <summary>
    /// Replace all classes with the given collection.
    /// </summary>
    public void SetFrom(IEnumerable<string> classes)
    {
        var oldClasses = _list.ToArray();
        _list.Clear();
        _set.Clear();

        foreach (var cls in classes)
        {
            if (_set.Add(cls))
                _list.Add(cls);
        }

        // Update index: remove old, add new
        if (Owner?._index is { } index)
        {
            foreach (var cls in oldClasses)
                index.OnClassRemoved(Owner, cls);
            foreach (var cls in _list)
                index.OnClassAdded(Owner, cls);
        }
    }

    public int IndexOf(string item) => _list.IndexOf(item);

    public void Insert(int index, string item)
    {
        if (!_set.Add(item)) return;
        _list.Insert(index, item);
        Owner?._index?.OnClassAdded(Owner, item);
    }

    public void RemoveAt(int index)
    {
        var item = _list[index];
        _list.RemoveAt(index);
        _set.Remove(item);
        Owner?._index?.OnClassRemoved(Owner, item);
    }

    public void CopyTo(string[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public IEnumerator<string> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
