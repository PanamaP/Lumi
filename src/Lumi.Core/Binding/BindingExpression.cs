namespace Lumi.Core.Binding;

/// <summary>
/// Specifies the direction of data flow in a binding.
/// </summary>
public enum BindingMode
{
    OneWay,
    TwoWay,
    OneTime
}

/// <summary>
/// Represents a parsed binding expression such as {Binding PropertyPath, Mode=TwoWay}.
/// </summary>
public class BindingExpression
{
    public string Path { get; set; } = "";
    public BindingMode Mode { get; set; } = BindingMode.OneWay;
    public string? Converter { get; set; }
    public string? FallbackValue { get; set; }
    public string? Template { get; set; }

    /// <summary>
    /// Parse a binding expression string like "{Binding Name}" or
    /// "{Binding Name, Mode=TwoWay, Converter=upper, FallbackValue=N/A}".
    /// </summary>
    public static BindingExpression Parse(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);

        var trimmed = expr.Trim();
        if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
            throw new FormatException($"Binding expression must be enclosed in curly braces: {expr}");

        // Strip outer braces
        var inner = trimmed[1..^1].Trim();

        // Must start with "Binding "
        if (!inner.StartsWith("Binding ", StringComparison.Ordinal))
            throw new FormatException($"Binding expression must start with 'Binding': {expr}");

        inner = inner["Binding ".Length..].Trim();
        if (string.IsNullOrEmpty(inner))
            throw new FormatException($"Binding expression must specify a property path: {expr}");

        var result = new BindingExpression();

        // Split on commas to get path and optional key=value pairs
        var parts = SplitParts(inner);

        // First part is always the property path
        result.Path = parts[0].Trim();

        // Remaining parts are key=value pairs
        for (int i = 1; i < parts.Count; i++)
        {
            var kv = parts[i].Trim();
            var eqIdx = kv.IndexOf('=');
            if (eqIdx < 0)
                throw new FormatException($"Invalid binding parameter (expected Key=Value): '{kv}'");

            var key = kv[..eqIdx].Trim();
            var value = kv[(eqIdx + 1)..].Trim();

            switch (key)
            {
                case "Mode":
                    result.Mode = Enum.Parse<BindingMode>(value, ignoreCase: true);
                    break;
                case "Converter":
                    result.Converter = value;
                    break;
                case "FallbackValue":
                    result.FallbackValue = value;
                    break;
                case "Template":
                    result.Template = value;
                    break;
                default:
                    throw new FormatException($"Unknown binding parameter: '{key}'");
            }
        }

        return result;
    }

    /// <summary>
    /// Splits the inner expression on commas while respecting nested braces.
    /// </summary>
    private static List<string> SplitParts(string input)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            switch (input[i])
            {
                case '{': depth++; break;
                case '}': depth--; break;
                case ',' when depth == 0:
                    parts.Add(input[start..i]);
                    start = i + 1;
                    break;
            }
        }

        parts.Add(input[start..]);
        return parts;
    }

    /// <summary>
    /// Returns true if the given string looks like a binding expression.
    /// </summary>
    public static bool IsBindingExpression(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var t = value.Trim();
        return t.StartsWith("{Binding ", StringComparison.Ordinal) && t.EndsWith('}');
    }
}
