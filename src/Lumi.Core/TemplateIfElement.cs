using System.ComponentModel;

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
    /// Parsed and added as children when the condition is true.
    /// </summary>
    public string TemplateHtml { get; set; } = "";

    private PropertyChangedEventHandler? _propertyHandler;
    private INotifyPropertyChanged? _notifySource;
    private Func<Element>? _createContent;
    private bool _isRendered;

    /// <summary>
    /// Disconnect from property change notifications.
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
                    var newValue = Binding.BindingEngine.ResolvePath(source, ConditionPath);
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
}
