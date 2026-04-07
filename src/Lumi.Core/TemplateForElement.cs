using System.Collections;
using System.Collections.Specialized;
using Lumi.Core.Binding;

namespace Lumi.Core;

/// <summary>
/// A template element that repeats its content for each item in a bound collection.
/// Usage in HTML: &lt;template for="{Items}" as="item"&gt;...&lt;/template&gt;
/// </summary>
public class TemplateForElement : Element
{
    public override string TagName => "template";

    /// <summary>
    /// Property path to the collection on the DataContext (e.g., "Items").
    /// </summary>
    public string CollectionPath { get; set; } = "";

    /// <summary>
    /// Alias for the loop variable (e.g., "item"). Used for text interpolation
    /// like {item.Name} inside the template body.
    /// </summary>
    public string ItemAlias { get; set; } = "item";

    /// <summary>
    /// The inner HTML of the template, stored as a raw string.
    /// Parsed once into a prototype element tree on first activation.
    /// </summary>
    public string TemplateHtml { get; set; } = "";

    /// <summary>
    /// Cached prototype element tree, lazily parsed from TemplateHtml.
    /// Cloned for each collection item instead of re-parsing HTML.
    /// </summary>
    internal Element? _prototype;

    /// <summary>
    /// Active template bindings for all rendered items. Disposed on Unbind/re-render.
    /// </summary>
    internal List<TemplateBinding> _bindings = [];

    private NotifyCollectionChangedEventHandler? _collectionHandler;
    private INotifyCollectionChanged? _observableSource;

    /// <summary>
    /// Disconnect from any collection change notifications and dispose all bindings.
    /// </summary>
    public void Unbind()
    {
        if (_observableSource != null && _collectionHandler != null)
        {
            _observableSource.CollectionChanged -= _collectionHandler;
            _observableSource = null;
            _collectionHandler = null;
        }

        foreach (var binding in _bindings)
            binding.Dispose();
        _bindings.Clear();
    }

    /// <summary>
    /// Subscribe to collection changes for live updates.
    /// Called by TemplateEngine during activation.
    /// </summary>
    internal void BindCollection(IEnumerable source, Func<object, Element> createInstance)
    {
        // Keep existing bindings — only re-subscribe to collection changes
        if (_observableSource != null && _collectionHandler != null)
        {
            _observableSource.CollectionChanged -= _collectionHandler;
        }

        if (source is INotifyCollectionChanged observable)
        {
            _observableSource = observable;
            _collectionHandler = (sender, e) => OnCollectionChanged(e, createInstance);
            observable.CollectionChanged += _collectionHandler;
        }
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e, Func<object, Element> createInstance)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    int insertIndex = e.NewStartingIndex >= 0
                        ? e.NewStartingIndex
                        : Children.Count;

                    foreach (var item in e.NewItems)
                    {
                        var element = createInstance(item);
                        InsertChild(insertIndex++, element);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null && e.OldStartingIndex >= 0)
                {
                    for (int i = e.OldItems.Count - 1; i >= 0; i--)
                    {
                        var idx = e.OldStartingIndex + i;
                        if (idx < Children.Count)
                        {
                            DisposeBindingsForSubtree(Children[idx]);
                            RemoveChild(Children[idx]);
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems != null && e.NewStartingIndex >= 0)
                {
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        var idx = e.NewStartingIndex + i;
                        if (idx < Children.Count)
                        {
                            // Dispose old bindings and re-create
                            DisposeBindingsForSubtree(Children[idx]);
                            RemoveChild(Children[idx]);
                            var element = createInstance(e.NewItems[i]!);
                            InsertChild(idx, element);
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                // Save source before Unbind() nulls it
                var currentSource = _observableSource;
                Unbind();
                ClearChildren();
                if (currentSource is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        var element = createInstance(item);
                        AddChild(element);
                    }
                }
                // Re-subscribe so future mutations are still tracked
                if (currentSource is IEnumerable enumerableSource)
                    BindCollection(enumerableSource, createInstance);
                break;
        }
    }

    /// <summary>
    /// Remove and dispose any TemplateBindings targeting elements in the given subtree.
    /// </summary>
    private void DisposeBindingsForSubtree(Element root)
    {
        var toRemove = new List<TemplateBinding>();
        foreach (var binding in _bindings)
        {
            if (IsDescendantOf(binding.Target, root))
                toRemove.Add(binding);
        }
        foreach (var binding in toRemove)
        {
            binding.Dispose();
            _bindings.Remove(binding);
        }
    }

    private static bool IsDescendantOf(Element? element, Element ancestor)
    {
        var current = element;
        while (current != null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }

    protected override Element CreateCloneInstance() => new TemplateForElement();

    public override Element DeepClone()
    {
        var clone = (TemplateForElement)base.DeepClone();
        clone.CollectionPath = CollectionPath;
        clone.ItemAlias = ItemAlias;
        clone.TemplateHtml = TemplateHtml;
        return clone;
    }
}
