namespace Lumi.Text;

using HarfBuzzSharp;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

/// <summary>
/// Result of HarfBuzz text shaping: positioned glyph IDs with advances.
/// </summary>
public sealed class ShapedGlyphRun
{
    /// <summary>Glyph IDs produced by shaping.</summary>
    public ushort[] GlyphIds { get; }

    /// <summary>Interleaved x,y position pairs for each glyph (length = GlyphIds.Length * 2).</summary>
    public float[] Positions { get; }

    /// <summary>Horizontal advance for each glyph in pixels.</summary>
    public float[] Advances { get; }

    /// <summary>Total advance width of the entire run in pixels.</summary>
    public float TotalWidth { get; }

    public string FontFamily { get; }
    public float FontSize { get; }
    public int FontWeight { get; }
    public bool Italic { get; }

    public ShapedGlyphRun(
        ushort[] glyphIds, float[] positions, float[] advances, float totalWidth,
        string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        GlyphIds = glyphIds;
        Positions = positions;
        Advances = advances;
        TotalWidth = totalWidth;
        FontFamily = fontFamily;
        FontSize = fontSize;
        FontWeight = fontWeight;
        Italic = italic;
    }
}

/// <summary>
/// Uses HarfBuzz to shape text into positioned glyph runs.
/// Caches HarfBuzz Font objects per (family, weight, italic) to avoid repeated creation.
/// </summary>
public sealed class TextShaper : IDisposable
{
    /// <summary>
    /// Optional external typeface resolver. When set, this is called first to resolve
    /// typefaces (e.g., from FontManager in Lumi.Rendering). If it returns null,
    /// falls back to system font resolution via <see cref="SKTypeface.FromFamilyName"/>.
    /// Signature: (familyName, weight, italic) → SKTypeface?
    /// </summary>
    private static volatile Func<string, int, bool, SKTypeface?>? _customTypefaceResolver;

    public static Func<string, int, bool, SKTypeface?>? CustomTypefaceResolver
    {
        get => _customTypefaceResolver;
        set => _customTypefaceResolver = value;
    }

    private readonly ConcurrentDictionary<FontKey, Lazy<CachedHarfBuzzFont>> _fontCache = new();

    private readonly record struct FontKey(string Family, int Weight, bool Italic);

    private sealed class CachedHarfBuzzFont : IDisposable
    {
        public HarfBuzzSharp.Font Font { get; }
        public Face Face { get; }
        public Blob Blob { get; }

        public CachedHarfBuzzFont(Blob blob, Face face, HarfBuzzSharp.Font font)
        {
            Blob = blob;
            Face = face;
            Font = font;
        }

        public void Dispose()
        {
            Font.Dispose();
            Face.Dispose();
            Blob.Dispose();
        }
    }

    /// <summary>
    /// Shape text using HarfBuzz and produce a glyph run with pixel-space positions.
    /// </summary>
    public ShapedGlyphRun Shape(string text, string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        if (string.IsNullOrEmpty(text))
            return new ShapedGlyphRun([], [], [], 0, fontFamily, fontSize, fontWeight, italic);

        var cached = GetOrCreateFont(fontFamily, fontWeight, italic);

        int upem = cached.Face.UnitsPerEm;
        if (upem <= 0) upem = 1000;
        float scale = fontSize / upem;

        using var buffer = new HarfBuzzSharp.Buffer();
        buffer.AddUtf16(text);
        buffer.Direction = Direction.LeftToRight;
        buffer.Script = Script.Parse("Latn");
        buffer.Language = Language.Default;
        buffer.GuessSegmentProperties();

        cached.Font.Shape(buffer, Array.Empty<Feature>());

        var glyphInfos = buffer.GlyphInfos;
        var glyphPositions = buffer.GlyphPositions;
        int count = glyphInfos.Length;

        var glyphIds = new ushort[count];
        var positions = new float[count * 2];
        var advances = new float[count];
        float cursorX = 0;
        float cursorY = 0;

        for (int i = 0; i < count; i++)
        {
            glyphIds[i] = (ushort)glyphInfos[i].Codepoint;

            float xOffset = glyphPositions[i].XOffset * scale;
            float yOffset = glyphPositions[i].YOffset * scale;
            float xAdvance = glyphPositions[i].XAdvance * scale;
            float yAdvance = glyphPositions[i].YAdvance * scale;

            positions[i * 2] = cursorX + xOffset;
            positions[i * 2 + 1] = cursorY - yOffset; // negate for top-down coordinates
            advances[i] = xAdvance;

            cursorX += xAdvance;
            cursorY += yAdvance;
        }

        return new ShapedGlyphRun(glyphIds, positions, advances, cursorX,
            fontFamily, fontSize, fontWeight, italic);
    }

    private CachedHarfBuzzFont GetOrCreateFont(string family, int weight, bool italic)
    {
        var key = new FontKey(family, weight, italic);
        // Use Lazy<T> to ensure the factory runs exactly once per key,
        // preventing leaked native handles under contention.
        var lazy = _fontCache.GetOrAdd(key, k => new Lazy<CachedHarfBuzzFont>(() => CreateFont(k)));
        return lazy.Value;
    }

    private static CachedHarfBuzzFont CreateFont(FontKey k)
    {
        // Check the custom resolver first (e.g., FontManager-registered fonts)
        SKTypeface? typeface = CustomTypefaceResolver?.Invoke(k.Family, k.Weight, k.Italic);
        bool ownsTypeface = typeface == null;

        if (typeface == null)
        {
            var skStyle = k.Weight >= 700
                ? (k.Italic ? SKFontStyle.BoldItalic : SKFontStyle.Bold)
                : (k.Italic ? SKFontStyle.Italic : SKFontStyle.Normal);

            typeface = SKTypeface.FromFamilyName(k.Family, skStyle) ?? SKTypeface.Default;
        }

        try
        {
            using var skStream = typeface.OpenStream(out var ttcIndex);

            var memBase = skStream.GetMemoryBase();
            int length = (int)skStream.Length;

            if (memBase == IntPtr.Zero || length <= 0)
                throw new InvalidOperationException($"Failed to read font data for '{k.Family}'");

            byte[] fontData = new byte[length];
            Marshal.Copy(memBase, fontData, 0, length);

            using var ms = new MemoryStream(fontData);
            var blob = Blob.FromStream(ms);
            blob.MakeImmutable();

            var face = new Face(blob, (uint)ttcIndex);
            var font = new HarfBuzzSharp.Font(face);
            font.SetFunctionsOpenType();

            return new CachedHarfBuzzFont(blob, face, font);
        }
        finally
        {
            if (ownsTypeface)
                typeface.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var lazy in _fontCache.Values)
        {
            if (lazy.IsValueCreated)
                lazy.Value.Dispose();
        }
        _fontCache.Clear();
    }
}
