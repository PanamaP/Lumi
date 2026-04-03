namespace Lumi.Core.Binding;

/// <summary>
/// Provides data context resolution for elements, supporting inheritance
/// through the visual tree (similar to WPF's DataContext).
/// </summary>
public static class BindingContext
{
    /// <summary>
    /// Gets the effective data context for an element by walking up the parent chain.
    /// Returns the first non-null DataContext found, or null if none exists.
    /// </summary>
    public static object? GetEffectiveDataContext(Element element)
    {
        var current = element;
        while (current != null)
        {
            if (current.DataContext != null)
                return current.DataContext;
            current = current.Parent;
        }
        return null;
    }
}
