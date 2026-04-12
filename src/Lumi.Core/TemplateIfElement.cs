using System.ComponentModel;
using Lumi.Core.Binding;

namespace Lumi.Core;

/// <summary>
/// A template element that conditionally renders its content based on a boolean binding.
/// Usage in HTML: &lt;template if="{IsVisible}"&gt;...&lt;/template&gt;
/// </summary>
public class TemplateIfElement : Element
{
    public override string TagName => "template";

    /// <summary>
    /// Property path to a boolean property on the DataContext (e.g., "IsVisible").
    /// </summary>
    public string ConditionPath { get; set; } = "";

    /// <summary>
    /// The inner HTML of the template, stored as a raw string.
    /// Parsed once into a prototype element tree on first activation.
    /// </summary>
    public string TemplateHtml { get; set; } = "";

    /// <summary>
    /// Cached prototype element tree, lazily parsed from TemplateHtml.
    /// </summary>
    internal Element? _prototype;

    /// <summary>
    /// Active template bindings for rendered content. Disposed when condition becomes false.
    /// </summary>
    internal List<TemplateBinding> _bindings = [];

    private PropertyChangedEventHandler? _propertyHandler;
    private INotifyPropertyChanged? _notifySource;
    private Func<Element>? _createContent;
    private bool _isRendered;

    /// <summary>
    /// Disconnect from property change notifications and dispose all bindings.
    /// </summary>
    public void Unbind()
    {
        if (_notifySource != null && _propertyHandler != null)
        {
            _notifySource.PropertyChanged -= _propertyHandler;
            _notifySource = null;
            _propertyHandler = null;
        }
        _createContent = null;

        foreach (var binding in _bindings)
            binding.Dispose();
        _bindings.Clear();
    }

    /// <summary>
    /// Subscribe to property changes for live condition toggling.
    /// Called by TemplateEngine during activation.
    /// </summary>
    internal void BindCondition(object source, Func<Element> createContent)
    {
        Unbind();
        _createContent = createContent;

        if (source is INotifyPropertyChanged notifier)
        {
            _notifySource = notifier;
            var rootProp = GetRootProperty(ConditionPath);
            _propertyHandler = (_, e) =>
            {
                if (e.PropertyName == rootProp || string.IsNullOrEmpty(e.PropertyName))
                {
                    var newValue = BindingEngine.ResolvePath(source, ConditionPath);
                    SetRendered(IsTruthy(newValue));
                }
            };
            notifier.PropertyChanged += _propertyHandler;
        }
    }

    internal void SetRendered(bool shouldRender)
    {
        if (shouldRender == _isRendered) return;

        _isRendered = shouldRender;

        if (shouldRender && _createContent != null)
        {
            var content = _createContent();
            foreach (var child in content.Children.ToList())
            {
                child.Parent = null;
                AddChild(child);
            }
        }
        else if (!shouldRender)
        {
            foreach (var binding in _bindings)
                binding.Dispose();
            _bindings.Clear();
            ClearChildren();
        }
    }

    internal static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            _ => true
        };
    }

    private static string GetRootProperty(string path)
    {
        var dotIndex = path.IndexOf('.');
        return dotIndex < 0 ? path : path[..dotIndex];
    }

    protected override Element CreateCloneInstance() => new TemplateIfElement();

    public override Element DeepClone()
    {
        var clone = (TemplateIfElement)base.DeepClone();
        clone.ConditionPath = ConditionPath;
        clone.TemplateHtml = TemplateHtml;
        return clone;
    }
}
