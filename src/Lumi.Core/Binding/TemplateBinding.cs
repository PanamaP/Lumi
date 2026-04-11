using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Lumi.Core.Binding;

/// <summary>
/// A reactive binding that handles {alias.PropertyPath} text interpolation
/// with live updates via INotifyPropertyChanged.
/// 
/// Converts patterns like "Hello {item.Name}, count: {item.Count}" into a format
/// string "Hello {0}, count: {1}" with property paths ["Name", "Count"], then
/// re-evaluates whenever a bound property changes.
/// </summary>
public sealed class TemplateBinding : IDisposable
{
    /// <summary>
    /// The target element containing the bound text or attribute.
    /// </summary>
    public Element Target { get; }

    /// <summary>
    /// The target property name ("Text" for TextElement, or attribute key).
    /// </summary>
    public string TargetProperty { get; }

    /// <summary>
    /// Whether this binding targets an HTML attribute rather than a typed property.
    /// </summary>
    public bool IsAttribute { get; }

    private readonly string _formatTemplate;
    private readonly string[] _propertyPaths;
    private readonly object _source;
    private PropertyChangedEventHandler? _handler;
    private bool _disposed;

    private TemplateBinding(
        Element target,
        string targetProperty,
        bool isAttribute,
        string formatTemplate,
        string[] propertyPaths,
        object source)
    {
        Target = target;
        TargetProperty = targetProperty;
        IsAttribute = isAttribute;
        _formatTemplate = formatTemplate;
        _propertyPaths = propertyPaths;
        _source = source;

        // Initial evaluation
        Evaluate();

        // Subscribe for live updates
        if (source is INotifyPropertyChanged notifier && propertyPaths.Length > 0)
        {
            var rootProps = new HashSet<string>(
                propertyPaths.Select(p => GetRootProperty(p)));

            _handler = (_, e) =>
            {
                if (_disposed) return;
                if (string.IsNullOrEmpty(e.PropertyName) || rootProps.Contains(e.PropertyName))
                    Evaluate();
            };
            notifier.PropertyChanged += _handler;
        }
    }

    /// <summary>
    /// Try to create a TemplateBinding for a text pattern containing {alias.xxx} expressions.
    /// Returns null if the text contains no matching patterns.
    /// </summary>
    public static TemplateBinding? TryCreate(
        Element target,
        string targetProperty,
        bool isAttribute,
        string text,
        string alias,
        object source)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{'))
            return null;

        var pattern = $@"\{{{Regex.Escape(alias)}(?:\.([^}}]+))?\}}";
        var matches = Regex.Matches(text, pattern);
        if (matches.Count == 0)
            return null;

        var propertyPaths = new List<string>();
        var formatTemplate = Regex.Replace(text, pattern, match =>
        {
            var propertyPath = match.Groups[1].Success ? match.Groups[1].Value : null;
            int index = propertyPaths.Count;
            propertyPaths.Add(propertyPath ?? "");
            // Escape existing braces in format string, use {N} for our placeholders
            return $"{{{index}}}";
        });

        // Escape any literal braces that aren't our placeholders
        // (format string uses {{ and }} for literal braces)
        formatTemplate = EscapeLiteralBraces(formatTemplate, propertyPaths.Count);

        return new TemplateBinding(
            target, targetProperty, isAttribute,
            formatTemplate, propertyPaths.ToArray(), source);
    }

    private void Evaluate()
    {
        if (_disposed) return;

        var args = new object?[_propertyPaths.Length];
        for (int i = 0; i < _propertyPaths.Length; i++)
        {
            if (string.IsNullOrEmpty(_propertyPaths[i]))
                args[i] = _source?.ToString() ?? "";
            else
                args[i] = BindingEngine.ResolvePath(_source, _propertyPaths[i])?.ToString() ?? "";
        }

        var result = string.Format(_formatTemplate, args!);
        SetValue(result);
    }

    private void SetValue(string value)
    {
        if (IsAttribute)
        {
            Target.Attributes[TargetProperty] = value;
            Target.MarkDirty();
        }
        else
        {
            switch (TargetProperty)
            {
                case "Text" when Target is TextElement textEl:
                    textEl.Text = value;
                    Target.MarkDirty();
                    break;
                case "Value" when Target is InputElement inputEl:
                    inputEl.Value = value;
                    Target.MarkDirty();
                    break;
                case "InlineStyle":
                    Target.InlineStyle = value;
                    Target.MarkDirty();
                    break;
                case "Source" when Target is ImageElement imgEl:
                    imgEl.Source = value;
                    Target.MarkDirty();
                    break;
            }
        }
    }

    private static string GetRootProperty(string path)
    {
        var dotIndex = path.IndexOf('.');
        return dotIndex < 0 ? path : path[..dotIndex];
    }

    /// <summary>
    /// Escape literal braces that are NOT our {N} placeholders so string.Format
    /// doesn't throw. After Regex.Replace converts {alias.Prop} → {N}, any
    /// remaining braces must be doubled ({{ / }}).
    /// </summary>
    private static string EscapeLiteralBraces(string template, int placeholderCount)
    {
        var validPlaceholders = new HashSet<string>();
        for (int i = 0; i < placeholderCount; i++)
            validPlaceholders.Add($"{{{i}}}");

        var sb = new System.Text.StringBuilder(template.Length);
        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] == '{')
            {
                // Check if this is one of our {N} placeholders
                var remaining = template.AsSpan(i);
                bool isPlaceholder = false;
                foreach (var ph in validPlaceholders)
                {
                    if (remaining.StartsWith(ph.AsSpan()))
                    {
                        sb.Append(ph);
                        // Advance past the entire placeholder including the closing '}'.
                        // -1 compensates for the for-loop's i++ so we land exactly
                        // one past the '}' on the next iteration.
                        i += ph.Length - 1;
                        isPlaceholder = true;
                        break;
                    }
                }
                if (isPlaceholder)
                    continue; // skip to next iteration — closing '}' already consumed
                sb.Append("{{");
            }
            else if (template[i] == '}')
            {
                sb.Append("}}");
            }
            else
            {
                sb.Append(template[i]);
            }
        }
        return sb.ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_handler != null && _source is INotifyPropertyChanged notifier)
        {
            notifier.PropertyChanged -= _handler;
            _handler = null;
        }
    }
}
