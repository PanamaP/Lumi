using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace Lumi.Core.Binding;

/// <summary>
/// Represents an active binding between a source property and a target element property.
/// </summary>
internal class ActiveBinding
{
    public Element Target { get; init; } = null!;
    public string TargetProperty { get; init; } = "";
    public object Source { get; init; } = null!;
    public BindingExpression Expression { get; init; } = null!;
    public bool IsActive { get; set; } = true;
    public PropertyChangedEventHandler? Handler { get; set; }
    public Action<string>? ReverseHandler { get; set; }
}

/// <summary>
/// The binding engine manages data bindings between source objects and UI elements.
/// It watches INotifyPropertyChanged sources and updates target element properties.
/// </summary>
public class BindingEngine
{
    private readonly List<ActiveBinding> _bindings = [];

    /// <summary>
    /// Create a binding between a source object's property and a target element's property.
    /// </summary>
    public void Bind(Element target, string targetProperty, object source, BindingExpression expr)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(expr);

        var binding = new ActiveBinding
        {
            Target = target,
            TargetProperty = targetProperty,
            Source = source,
            Expression = expr
        };

        // Initial push: source → target
        var value = ResolvePath(source, expr.Path);
        SetTargetValue(target, targetProperty, value, expr.FallbackValue);

        // For OneTime, we're done — no event subscription
        if (expr.Mode == BindingMode.OneTime)
        {
            _bindings.Add(binding);
            return;
        }

        // Watch source for OneWay / TwoWay
        if (source is INotifyPropertyChanged notifier)
        {
            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (!binding.IsActive) return;

                // React if the changed property is the root of our path
                var rootProp = GetRootProperty(expr.Path);
                if (e.PropertyName == rootProp || string.IsNullOrEmpty(e.PropertyName))
                {
                    var newValue = ResolvePath(source, expr.Path);
                    SetTargetValue(target, targetProperty, newValue, expr.FallbackValue);
                }
            };

            notifier.PropertyChanged += handler;
            binding.Handler = handler;
        }

        // TwoWay: push input changes back to source
        if (expr.Mode == BindingMode.TwoWay && target is InputElement input)
        {
            Action<string> reverseHandler = newValue =>
            {
                if (!binding.IsActive) return;
                SetSourceValue(source, expr.Path, newValue);
            };

            input.ValueChanged += reverseHandler;
            binding.ReverseHandler = reverseHandler;
        }

        _bindings.Add(binding);
    }

    /// <summary>
    /// Force-update all active bindings from source to target.
    /// </summary>
    public void UpdateAll()
    {
        foreach (var b in _bindings)
        {
            if (!b.IsActive) continue;
            var value = ResolvePath(b.Source, b.Expression.Path);
            SetTargetValue(b.Target, b.TargetProperty, value, b.Expression.FallbackValue);
        }
    }

    /// <summary>
    /// Remove and unhook all bindings.
    /// </summary>
    public void ClearAll()
    {
        foreach (var b in _bindings)
        {
            b.IsActive = false;

            if (b.Handler != null && b.Source is INotifyPropertyChanged notifier)
                notifier.PropertyChanged -= b.Handler;

            if (b.ReverseHandler != null && b.Target is InputElement input)
                input.ValueChanged -= b.ReverseHandler;
        }

        _bindings.Clear();
    }

    /// <summary>
    /// Resolve a dot-separated property path on an object via reflection.
    /// e.g. "User.Name" on a ViewModel navigates ViewModel.User.Name.
    /// </summary>
    public static object? ResolvePath(object? source, string path)
    {
        if (source == null || string.IsNullOrEmpty(path))
            return source;

        var current = source;
        var segments = path.Split('.');

        foreach (var segment in segments)
        {
            if (current == null) return null;

            var prop = current.GetType().GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance);

            if (prop == null) return null;
            current = prop.GetValue(current);
        }

        return current;
    }

    /// <summary>
    /// Set a value on a source object at the given dot-path.
    /// </summary>
    internal static void SetSourceValue(object source, string path, object? value)
    {
        var segments = path.Split('.');
        var target = source;

        // Navigate to the parent of the final property
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (target == null) return;
            var prop = target.GetType().GetProperty(segments[i],
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return;
            target = prop.GetValue(target);
        }

        if (target == null) return;

        var finalProp = target.GetType().GetProperty(segments[^1],
            BindingFlags.Public | BindingFlags.Instance);

        if (finalProp?.CanWrite == true)
        {
            var converted = ConvertValue(value, finalProp.PropertyType);
            finalProp.SetValue(target, converted);
        }
    }

    private static string GetRootProperty(string path)
    {
        var dotIndex = path.IndexOf('.');
        return dotIndex < 0 ? path : path[..dotIndex];
    }

    private static void SetTargetValue(Element target, string targetProperty, object? value, string? fallback)
    {
        var displayValue = value?.ToString() ?? fallback ?? "";

        switch (targetProperty)
        {
            case "Text" when target is TextElement textEl:
                textEl.Text = displayValue;
                target.MarkDirty();
                break;
            case "Value" when target is InputElement inputEl:
                inputEl.Value = displayValue;
                target.MarkDirty();
                break;
            case "InlineStyle":
                target.InlineStyle = displayValue;
                target.MarkDirty();
                break;
            case "Source" when target is ImageElement imgEl:
                imgEl.Source = displayValue;
                target.MarkDirty();
                break;
            default:
                // Try generic reflection-based set
                var prop = target.GetType().GetProperty(targetProperty,
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop?.CanWrite == true)
                {
                    prop.SetValue(target, ConvertValue(value, prop.PropertyType));
                    target.MarkDirty();
                }
                break;
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        if (targetType.IsInstanceOfType(value)) return value;
        if (targetType == typeof(string)) return value.ToString();

        try { return Convert.ChangeType(value, targetType); }
        catch { return targetType.IsValueType ? Activator.CreateInstance(targetType) : null; }
    }
}
