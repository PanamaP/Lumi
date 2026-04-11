namespace Lumi.Rendering;

using SkiaSharp;

/// <summary>
/// Manages custom font registration and lookup. Registered fonts are resolved
/// before falling back to system-installed fonts via <see cref="TextMeasurer"/>.
/// </summary>
public static class FontManager
{
    private sealed record FontEntry(SKTypeface Typeface, int Weight, bool IsItalic, bool Owned);

    private static readonly Dictionary<string, List<FontEntry>> s_fonts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<FontEntry>> s_fallbacks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object s_lock = new();

    /// <summary>
    /// Register a font file. Returns the family name extracted from the font metadata.
    /// </summary>
    public static string RegisterFont(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Font file not found: '{filePath}'", filePath);

        var typeface = SKTypeface.FromFile(filePath)
            ?? throw new ArgumentException($"Failed to load font from '{filePath}'.", nameof(filePath));

        var familyName = typeface.FamilyName;
        var weight = typeface.FontWeight;
        var italic = typeface.FontSlant != SKFontStyleSlant.Upright;

        AddEntry(familyName, new FontEntry(typeface, weight, italic, true));
        return familyName;
    }

    /// <summary>
    /// Register a font file with an explicit family name.
    /// </summary>
    public static void RegisterFont(string familyName, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Font file not found: '{filePath}'", filePath);

        var typeface = SKTypeface.FromFile(filePath)
            ?? throw new ArgumentException($"Failed to load font from '{filePath}'.", nameof(filePath));

        var weight = typeface.FontWeight;
        var italic = typeface.FontSlant != SKFontStyleSlant.Upright;

        AddEntry(familyName, new FontEntry(typeface, weight, italic, true));
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

        AddEntry(familyName, new FontEntry(typeface, weight, italic, true));
    }

    /// <summary>
    /// Register a pre-created typeface under a family name. Useful for testing.
    /// Externally-provided typefaces are NOT disposed when <see cref="Clear"/> is called.
    /// </summary>
    public static void RegisterTypeface(string familyName, SKTypeface typeface, int weight = 400, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(typeface);
        AddEntry(familyName, new FontEntry(typeface, weight, italic, false));
    }

    /// <summary>
    /// Register a font file as a fallback for a given category (e.g., "emoji", "symbol", "cjk").
    /// Fallback fonts are tried in registration order when the primary font is missing a glyph.
    /// </summary>
    public static string RegisterFallbackFont(string category, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Font file not found: '{filePath}'", filePath);

        var typeface = SKTypeface.FromFile(filePath)
            ?? throw new ArgumentException($"Failed to load font from '{filePath}'.", nameof(filePath));

        var familyName = typeface.FamilyName;
        lock (s_lock)
        {
            if (!s_fallbacks.TryGetValue(category, out var list))
            {
                list = [];
                s_fallbacks[category] = list;
            }
            list.Add(new FontEntry(typeface, typeface.FontWeight, typeface.FontSlant != SKFontStyleSlant.Upright, true));
        }
        return familyName;
    }

    /// <summary>
    /// Register a pre-created typeface as a fallback for a category.
    /// </summary>
    public static void RegisterFallbackTypeface(string category, SKTypeface typeface)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(typeface);
        lock (s_lock)
        {
            if (!s_fallbacks.TryGetValue(category, out var list))
            {
                list = [];
                s_fallbacks[category] = list;
            }
            list.Add(new FontEntry(typeface, typeface.FontWeight, false, false));
        }
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
    /// Get the first registered fallback typeface for a category.
    /// Returns null if no fallback is registered for that category.
    /// </summary>
    public static SKTypeface? GetFallbackTypeface(string category)
    {
        lock (s_lock)
        {
            if (s_fallbacks.TryGetValue(category, out var entries) && entries.Count > 0)
                return entries[0].Typeface;
            return null;
        }
    }

    /// <summary>
    /// Check if a typeface contains a glyph for the given Unicode codepoint.
    /// </summary>
    public static bool HasGlyph(SKTypeface typeface, int codepoint)
    {
        if (typeface == null) return false;
        var glyphs = typeface.GetGlyphs(new string(char.ConvertFromUtf32(codepoint)));
        return glyphs.Length > 0 && glyphs[0] != 0;
    }

    /// <summary>
    /// Resolve a typeface for the given text, trying the primary font first,
    /// then falling back to registered fallback fonts by category.
    /// </summary>
    public static SKTypeface? ResolveWithFallback(string familyName, int weight, bool italic, string? fallbackCategory)
    {
        // Try primary font first
        var primary = GetTypeface(familyName, weight, italic);
        if (primary != null)
            return primary;

        // Try fallback by category
        if (fallbackCategory != null)
        {
            var fallback = GetFallbackTypeface(fallbackCategory);
            if (fallback != null)
                return fallback;
        }

        return null;
    }

    /// <summary>
    /// Remove all registered fonts. Disposes typefaces that were loaded from files
    /// or byte arrays (owned by FontManager). Externally-provided typefaces are not disposed.
    /// </summary>
    public static void Clear()
    {
        lock (s_lock)
        {
            foreach (var entries in s_fonts.Values)
            {
                foreach (var entry in entries)
                {
                    if (entry.Owned)
                        entry.Typeface.Dispose();
                }
            }
            s_fonts.Clear();

            foreach (var entries in s_fallbacks.Values)
            {
                foreach (var entry in entries)
                {
                    if (entry.Owned)
                        entry.Typeface.Dispose();
                }
            }
            s_fallbacks.Clear();
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
