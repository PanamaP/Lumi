using System.Collections;
using System.Collections.Specialized;

namespace Lumi.Core.Binding;

/// <summary>
/// Renders a collection of items into child elements using a template factory.
/// Supports INotifyCollectionChanged for automatic add/remove of children.
/// </summary>
public class ItemsRenderer
{
    private Element? _container;
    private Func<Element>? _templateFactory;
    private NotifyCollectionChangedEventHandler? _collectionHandler;
    private INotifyCollectionChanged? _observableCollection;

    /// <summary>
    /// Bind an items source to a container element. Each item becomes a DataContext
    /// on a cloned template element added as a child.
    /// </summary>
    public void BindItemsSource(Element container, IEnumerable source, Func<Element> templateFactory)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(templateFactory);

        // Clean up any previous binding
        Unbind();

        _container = container;
        _templateFactory = templateFactory;

        // Initial population
        container.ClearChildren();
        foreach (var item in source)
        {
            var element = templateFactory();
            element.DataContext = item;
            container.AddChild(element);
        }

        // Watch for collection changes
        if (source is INotifyCollectionChanged observable)
        {
            _observableCollection = observable;
            _collectionHandler = OnCollectionChanged;
            observable.CollectionChanged += _collectionHandler;
        }
    }

    /// <summary>
    /// Disconnect from the collection's change notifications.
    /// </summary>
    public void Unbind()
    {
        if (_observableCollection != null && _collectionHandler != null)
        {
            _observableCollection.CollectionChanged -= _collectionHandler;
            _observableCollection = null;
            _collectionHandler = null;
        }

        _container = null;
        _templateFactory = null;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_container == null || _templateFactory == null) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    int insertIndex = e.NewStartingIndex >= 0
                        ? e.NewStartingIndex
                        : _container.Children.Count;

                    foreach (var item in e.NewItems)
                    {
                        var element = _templateFactory();
                        element.DataContext = item;
                        _container.InsertChild(insertIndex++, element);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null && e.OldStartingIndex >= 0)
                {
                    for (int i = e.OldItems.Count - 1; i >= 0; i--)
                    {
                        var idx = e.OldStartingIndex + i;
                        if (idx < _container.Children.Count)
                            _container.RemoveChild(_container.Children[idx]);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                _container.ClearChildren();
                if (sender is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        var element = _templateFactory();
                        element.DataContext = item;
                        _container.AddChild(element);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems != null && e.NewStartingIndex >= 0)
                {
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        var idx = e.NewStartingIndex + i;
                        if (idx < _container.Children.Count)
                            _container.Children[idx].DataContext = e.NewItems[i];
                    }
                }
                break;
        }
    }
}
