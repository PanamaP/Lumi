namespace Lumi.Rendering;

using SkiaSharp;

/// <summary>
/// Manages custom font registration and lookup. Registered fonts are resolved
/// before falling back to system-installed fonts via <see cref="TextMeasurer"/>.
/// </summary>
public static class FontManager
{
    private sealed record FontEntry(SKTypeface Typeface, int Weight, bool IsItalic);

    private static readonly Dictionary<string, List<FontEntry>> s_fonts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object s_lock = new();

    /// <summary>
    /// Register a font file. Returns the family name extracted from the font metadata.
    /// </summary>
    public static string RegisterFont(string filePath)
    {
        var typeface = SKTypeface.FromFile(filePath)
            ?? throw new ArgumentException($"Failed to load font from '{filePath}'.", nameof(filePath));

        var familyName = typeface.FamilyName;
        var weight = typeface.FontWeight;
        var italic = typeface.FontSlant != SKFontStyleSlant.Upright;

        AddEntry(familyName, new FontEntry(typeface, weight, italic));
        return familyName;
    }

    /// <summary>
    /// Register a font file with an explicit family name.
    /// </summary>
    public static void RegisterFont(string familyName, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        var typeface = SKTypeface.FromFile(filePath)
            ?? throw new ArgumentException($"Failed to load font from '{filePath}'.", nameof(filePath));

        var weight = typeface.FontWeight;
        var italic = typeface.FontSlant != SKFontStyleSlant.Upright;

        AddEntry(familyName, new FontEntry(typeface, weight, italic));
    }

    /// <summary>
    /// Register a font from a byte array (e.g. an embedded resource).
    /// </summary>
    public static void RegisterFont(string familyName, byte[] fontData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(fontData);

        using var data = SKData.CreateCopy(fontData);
        var typeface = SKTypeface.FromData(data)
            ?? throw new ArgumentException("Failed to create typeface from provided font data.", nameof(fontData));

        var weight = typeface.FontWeight;
        var italic = typeface.FontSlant != SKFontStyleSlant.Upright;

        AddEntry(familyName, new FontEntry(typeface, weight, italic));
    }

    /// <summary>
    /// Register a pre-created typeface under a family name. Useful for testing.
    /// </summary>
    public static void RegisterTypeface(string familyName, SKTypeface typeface, int weight = 400, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(typeface);
        AddEntry(familyName, new FontEntry(typeface, weight, italic));
    }

    /// <summary>
    /// Try to get a registered custom typeface that best matches the requested weight and style.
    /// Returns null if no font is registered under the given family name.
    /// </summary>
    public static SKTypeface? GetTypeface(string familyName, int weight = 400, bool italic = false)
    {
        lock (s_lock)
        {
            if (!s_fonts.TryGetValue(familyName, out var entries) || entries.Count == 0)
                return null;

            // Best-match: prefer exact italic match, then closest weight
            FontEntry? best = null;
            int bestScore = int.MaxValue;

            foreach (var entry in entries)
            {
                int italicPenalty = entry.IsItalic == italic ? 0 : 1000;
                int weightDist = Math.Abs(entry.Weight - weight);
                int score = italicPenalty + weightDist;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = entry;
                }
            }

            return best?.Typeface;
        }
    }

    /// <summary>
    /// Check whether a family name has been registered as a custom font.
    /// </summary>
    public static bool IsRegistered(string familyName)
    {
        lock (s_lock)
        {
            return s_fonts.ContainsKey(familyName);
        }
    }

    /// <summary>
    /// Remove all registered fonts. Primarily useful for test isolation.
    /// </summary>
    public static void Clear()
    {
        lock (s_lock)
        {
            s_fonts.Clear();
        }
    }

    private static void AddEntry(string familyName, FontEntry entry)
    {
        lock (s_lock)
        {
            if (!s_fonts.TryGetValue(familyName, out var list))
            {
                list = [];
                s_fonts[familyName] = list;
            }

            list.Add(entry);
        }
    }
}
