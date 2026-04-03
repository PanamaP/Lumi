namespace Lumi.Rendering;

using SkiaSharp;
using System.Collections.Concurrent;

/// <summary>
/// Loads and caches decoded image bitmaps for use by the renderer.
/// Images are cached by absolute file path.
/// </summary>
public sealed class ImageCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CachedImage?> _cache = new();

    /// <summary>
    /// A cached image entry with its decoded bitmap and natural dimensions.
    /// </summary>
    public sealed class CachedImage : IDisposable
    {
        public SKBitmap Bitmap { get; }
        public int NaturalWidth => Bitmap.Width;
        public int NaturalHeight => Bitmap.Height;

        public CachedImage(SKBitmap bitmap) => Bitmap = bitmap;

        public void Dispose() => Bitmap.Dispose();
    }

    /// <summary>
    /// Get or load an image from a file path. Returns null if the image cannot be loaded.
    /// </summary>
    public CachedImage? Get(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        return _cache.GetOrAdd(path, LoadImage);
    }

    /// <summary>
    /// Evict a specific image from the cache.
    /// </summary>
    public void Evict(string path)
    {
        if (_cache.TryRemove(path, out var cached))
            cached?.Dispose();
    }

    /// <summary>
    /// Clear the entire cache and dispose all bitmaps.
    /// </summary>
    public void Clear()
    {
        foreach (var entry in _cache.Values)
            entry?.Dispose();
        _cache.Clear();
    }

    private static CachedImage? LoadImage(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var bitmap = SKBitmap.Decode(path);
            return bitmap != null ? new CachedImage(bitmap) : null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }
}
