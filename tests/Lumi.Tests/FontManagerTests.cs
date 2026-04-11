using Lumi.Rendering;
using SkiaSharp;

namespace Lumi.Tests;

public sealed class FontManagerTests : IDisposable
{
    public FontManagerTests()
    {
        FontManager.Clear();
    }

    public void Dispose()
    {
        FontManager.Clear();
    }

    [Fact]
    public void RegisterTypeface_StoresTypeface()
    {
        var typeface = SKTypeface.Default;
        FontManager.RegisterTypeface("TestFont", typeface, 400, false);

        Assert.True(FontManager.IsRegistered("TestFont"));
    }

    [Fact]
    public void IsRegistered_ReturnsFalse_ForUnknownFont()
    {
        Assert.False(FontManager.IsRegistered("NonExistentFont"));
    }

    [Fact]
    public void GetTypeface_ReturnsRegisteredTypeface()
    {
        var typeface = SKTypeface.Default;
        FontManager.RegisterTypeface("MyFont", typeface, 400, false);

        var result = FontManager.GetTypeface("MyFont");

        Assert.NotNull(result);
        Assert.Same(typeface, result);
    }

    [Fact]
    public void GetTypeface_ReturnsNull_ForUnknownFamily()
    {
        var result = FontManager.GetTypeface("NeverRegistered");

        Assert.Null(result);
    }

    [Fact]
    public void IsRegistered_IsCaseInsensitive()
    {
        FontManager.RegisterTypeface("RobotoMono", SKTypeface.Default);

        Assert.True(FontManager.IsRegistered("robotomono"));
        Assert.True(FontManager.IsRegistered("ROBOTOMONO"));
        Assert.True(FontManager.IsRegistered("RobotoMono"));
    }

    [Fact]
    public void RegisterFont_WithByteArray_RegistersSuccessfully()
    {
        // Get byte data from the default typeface
        using var stream = SKTypeface.Default.OpenStream();
        var bytes = new byte[stream.Length];
        stream.Read(bytes, bytes.Length);

        FontManager.RegisterFont("ByteFont", bytes);

        Assert.True(FontManager.IsRegistered("ByteFont"));
        Assert.NotNull(FontManager.GetTypeface("ByteFont"));
    }

    [Fact]
    public void MultipleFonts_SameFamily_DifferentWeights_AreStored()
    {
        var regular = SKTypeface.Default;
        var bold = SKTypeface.Default;

        FontManager.RegisterTypeface("MultiWeight", regular, 400, false);
        FontManager.RegisterTypeface("MultiWeight", bold, 700, false);

        Assert.True(FontManager.IsRegistered("MultiWeight"));

        // Requesting weight 400 should return the regular entry
        var result400 = FontManager.GetTypeface("MultiWeight", weight: 400);
        Assert.Same(regular, result400);

        // Requesting weight 700 should return the bold entry
        var result700 = FontManager.GetTypeface("MultiWeight", weight: 700);
        Assert.Same(bold, result700);
    }

    [Fact]
    public void GetTypeface_MatchesItalicPreference()
    {
        var upright = SKTypeface.Default;
        var italic = SKTypeface.Default;

        FontManager.RegisterTypeface("StyleFont", upright, 400, false);
        FontManager.RegisterTypeface("StyleFont", italic, 400, true);

        var resultUpright = FontManager.GetTypeface("StyleFont", 400, italic: false);
        Assert.Same(upright, resultUpright);

        var resultItalic = FontManager.GetTypeface("StyleFont", 400, italic: true);
        Assert.Same(italic, resultItalic);
    }

    [Fact]
    public void GetTypeface_SelectsClosestWeight()
    {
        var light = SKTypeface.Default;
        var bold = SKTypeface.Default;

        FontManager.RegisterTypeface("WeightFont", light, 300, false);
        FontManager.RegisterTypeface("WeightFont", bold, 700, false);

        // Requesting 400 should prefer 300 (distance 100) over 700 (distance 300)
        var result = FontManager.GetTypeface("WeightFont", weight: 400);
        Assert.Same(light, result);

        // Requesting 600 should prefer 700 (distance 100) over 300 (distance 300)
        var result600 = FontManager.GetTypeface("WeightFont", weight: 600);
        Assert.Same(bold, result600);
    }

    [Fact]
    public void Clear_RemovesAllRegisteredFonts()
    {
        FontManager.RegisterTypeface("Cleared", SKTypeface.Default);
        Assert.True(FontManager.IsRegistered("Cleared"));

        FontManager.Clear();
        Assert.False(FontManager.IsRegistered("Cleared"));
    }

    [Fact]
    public void TextMeasurer_CreateFont_UsesRegisteredFont()
    {
        var typeface = SKTypeface.Default;
        FontManager.RegisterTypeface("CustomForMeasurer", typeface, 400, false);

        using var font = TextMeasurer.CreateFont("CustomForMeasurer", 16f, 400, false);

        Assert.NotNull(font);
        Assert.Equal(16f, font.Size);
        // The typeface should be the one we registered
        Assert.Same(typeface, font.Typeface);
    }
}
