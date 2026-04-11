namespace Lumi.Text;

using SkiaSharp;
using System.Collections.Concurrent;

/// <summary>
/// Shared system typeface cache used by TextMeasurer and ShapedTextRenderer.
/// Consolidates typeface allocation and ensures proper disposal on shutdown.
/// </summary>
public static class TypefaceCache
{
    private static readonly ConcurrentDictionary<(string Family, bool Bold, bool Italic), SKTypeface> s_cache = new();
    private static readonly object s_clearLock = new();

    /// <summary>
    /// Get or create a system typeface for the given font description.
    /// </summary>
    public static SKTypeface GetOrCreate(string family, bool bold, bool italic)
    {
        return s_cache.GetOrAdd((family, bold, italic), key =>
        {
            var skStyle = key.Bold
                ? (key.Italic ? SKFontStyle.BoldItalic : SKFontStyle.Bold)
                : (key.Italic ? SKFontStyle.Italic : SKFontStyle.Normal);
            return SKTypeface.FromFamilyName(key.Family, skStyle) ?? SKTypeface.Default;
        });
    }

    /// <summary>
    /// Dispose all cached typefaces and clear the cache.
    /// Call on application shutdown to release native resources.
    /// </summary>
    public static void Clear()
    {
        // Swap the cache contents out atomically, then dispose offline
        // so no concurrent reader can obtain a disposed typeface.
        KeyValuePair<(string Family, bool Bold, bool Italic), SKTypeface>[] snapshot;
        lock (s_clearLock)
        {
            snapshot = s_cache.ToArray();
            s_cache.Clear();
        }

        foreach (var kvp in snapshot)
        {
            if (kvp.Value != SKTypeface.Default)
                kvp.Value.Dispose();
        }
    }
}
