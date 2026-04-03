using Lumi.Rendering;
using SkiaSharp;

namespace Lumi.Tests;

public class ImageCacheTests : IDisposable
{
    private readonly ImageCache _cache = new();
    private readonly List<string> _tempFiles = [];

    private string CreateTempPng(int width = 1, int height = 1)
    {
        var path = Path.Combine(
            Path.GetDirectoryName(typeof(ImageCacheTests).Assembly.Location)!,
            $"test_{Guid.NewGuid():N}.png");
        using var bitmap = new SKBitmap(width, height);
        bitmap.SetPixel(0, 0, SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
        _tempFiles.Add(path);
        return path;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Get_NullOrEmptyPath_ReturnsNull(string? path)
    {
        var result = _cache.Get(path!);
        Assert.Null(result);
    }

    [Fact]
    public void Get_NonexistentFile_ReturnsNull()
    {
        var result = _cache.Get(@"C:\nonexistent\fakefile.png");
        Assert.Null(result);
    }

    [Fact]
    public void Get_ValidPng_ReturnsCachedImageWithCorrectDimensions()
    {
        var path = CreateTempPng(4, 3);
        var result = _cache.Get(path);

        Assert.NotNull(result);
        Assert.Equal(4, result.NaturalWidth);
        Assert.Equal(3, result.NaturalHeight);
    }

    [Fact]
    public void Get_CalledTwice_ReturnsSameInstance()
    {
        var path = CreateTempPng();
        var first = _cache.Get(path);
        var second = _cache.Get(path);

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void Evict_RemovesCachedEntry()
    {
        var path = CreateTempPng();
        var first = _cache.Get(path);
        Assert.NotNull(first);

        _cache.Evict(path);

        // After eviction, a new Get should load a fresh instance
        var second = _cache.Get(path);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var path1 = CreateTempPng();
        var path2 = CreateTempPng();
        _cache.Get(path1);
        _cache.Get(path2);

        _cache.Clear();

        // After clear, gets should return new instances
        var fresh1 = _cache.Get(path1);
        var fresh2 = _cache.Get(path2);
        Assert.NotNull(fresh1);
        Assert.NotNull(fresh2);
    }

    [Fact]
    public void Dispose_CleansUpProperly()
    {
        var path = CreateTempPng();
        _cache.Get(path);

        // Should not throw
        _cache.Dispose();
    }

    public void Dispose()
    {
        _cache.Dispose();
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); } catch { }
        }
    }
}
