using System.Collections;
using System.Collections.Specialized;

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
    /// Re-parsed for each collection item during activation.
    /// </summary>
    public string TemplateHtml { get; set; } = "";

    private NotifyCollectionChangedEventHandler? _collectionHandler;
    private INotifyCollectionChanged? _observableSource;

    /// <summary>
    /// Disconnect from any collection change notifications.
    /// </summary>
    public void Unbind()
    {
        if (_observableSource != null && _collectionHandler != null)
        {
            _observableSource.CollectionChanged -= _collectionHandler;
            _observableSource = null;
            _collectionHandler = null;
        }
    }

    /// <summary>
    /// Subscribe to collection changes for live updates.
    /// Called by TemplateEngine during activation.
    /// </summary>
    internal void BindCollection(IEnumerable source, Func<object, Element> createInstance)
    {
        Unbind();

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
                            RemoveChild(Children[idx]);
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
                            Children[idx].DataContext = e.NewItems[i];
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ClearChildren();
                if (_observableSource is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        var element = createInstance(item);
                        AddChild(element);
                    }
                }
                break;
        }
    }
}
