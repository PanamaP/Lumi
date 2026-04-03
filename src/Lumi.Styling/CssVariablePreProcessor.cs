using System.Text.RegularExpressions;

namespace Lumi.Styling;

/// <summary>
/// Pre-processes CSS to resolve var() references before parsing.
/// Custom properties (--name) are resolved at parse time and the declarations
/// are removed, since the style pipeline doesn't yet support scoped variables.
/// </summary>
public static partial class CssVariablePreProcessor
{
    /// <summary>
    /// Resolve all CSS custom property references (var()) in the raw CSS string.
    /// Called before parsing since the style pipeline resolves variables at parse time.
    /// </summary>
    public static string Process(string css)
    {
        // Step 1: Extract all --custom-property: value definitions
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in CustomPropertyPattern().Matches(css))
        {
            var name = match.Groups[1].Value.Trim();
            var value = match.Groups[2].Value.Trim();
            variables[name] = value;
        }

        if (variables.Count == 0)
            return css;

        // Step 2: Resolve nested variable references within variable definitions
        // e.g., --border-color: var(--accent);
        int maxPasses = 5;
        bool changed = true;
        while (changed && maxPasses-- > 0)
        {
            changed = false;
            foreach (var key in variables.Keys.ToList())
            {
                var resolved = ResolveVarReferences(variables[key], variables);
                if (resolved != variables[key])
                {
                    variables[key] = resolved;
                    changed = true;
                }
            }
        }

        // Step 3: Replace all var(--name) and var(--name, fallback) in property values
        css = VarReferencePattern().Replace(css, match =>
        {
            var varName = match.Groups[1].Value.Trim();
            var fallback = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            if (variables.TryGetValue(varName, out var value))
                return value;

            return fallback ?? "inherit";
        });

        // Step 4: Remove custom property declarations (resolved above, not needed downstream)
        css = CustomPropertyDeclarationPattern().Replace(css, "");

        return css;
    }

    private static string ResolveVarReferences(string value, Dictionary<string, string> variables)
    {
        return VarReferencePattern().Replace(value, match =>
        {
            var varName = match.Groups[1].Value.Trim();
            var fallback = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            if (variables.TryGetValue(varName, out var resolved))
                return resolved;

            return fallback ?? value;
        });
    }

    // Matches: --name: value; (captures name and value)
    [GeneratedRegex(@"(--[a-zA-Z0-9_-]+)\s*:\s*([^;]+);", RegexOptions.Compiled)]
    private static partial Regex CustomPropertyPattern();

    // Matches: var(--name) or var(--name, fallback)
    [GeneratedRegex(@"var\(\s*(--[a-zA-Z0-9_-]+)\s*(?:,\s*([^)]+))?\s*\)", RegexOptions.Compiled)]
    private static partial Regex VarReferencePattern();

    // Matches full custom property declaration lines to remove them
    [GeneratedRegex(@"\s*--[a-zA-Z0-9_-]+\s*:\s*[^;]+;\s*", RegexOptions.Compiled)]
    private static partial Regex CustomPropertyDeclarationPattern();
}
