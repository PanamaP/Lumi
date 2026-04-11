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
    /// Default OpenType features applied during shaping. Modify to control ligatures,
    /// kerning, and other typographic features globally.
    /// Common tags: "liga" (standard ligatures), "kern" (kerning), "dlig" (discretionary ligatures),
    /// "calt" (contextual alternates), "smcp" (small caps).
    /// </summary>
    private static volatile Feature[] _defaultFeatures =
    [
        Feature.Parse("+liga"),
        Feature.Parse("+calt"),
        Feature.Parse("+kern"),
    ];

    /// <summary>
    /// Gets the default OpenType features applied during shaping.
    /// </summary>
    public static IReadOnlyList<Feature> DefaultFeatures => _defaultFeatures;

    /// <summary>
    /// Replaces the default OpenType features applied during shaping.
    /// </summary>
    public static void SetDefaultFeatures(params Feature[] features)
    {
        ArgumentNullException.ThrowIfNull(features);
        _defaultFeatures = (Feature[])features.Clone();
    }

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

    /// <summary>
    /// Optional resolver for fallback typefaces by category. Called when the primary font
    /// doesn't cover a script segment (e.g., emoji, symbol).
    /// Signature: (category) → SKTypeface?
    /// </summary>
    private static volatile Func<string, SKTypeface?>? _fallbackTypefaceResolver;

    public static Func<string, SKTypeface?>? FallbackTypefaceResolver
    {
        get => _fallbackTypefaceResolver;
        set => _fallbackTypefaceResolver = value;
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
    public ShapedGlyphRun Shape(string text, string fontFamily, float fontSize, int fontWeight, bool italic, Feature[]? features = null)
    {
        if (string.IsNullOrEmpty(text))
            return new ShapedGlyphRun([], [], [], 0, fontFamily, fontSize, fontWeight, italic);

        var dominantScript = GetDominantScript(text);
        return ShapeWithScript(text, fontFamily, fontSize, fontWeight, italic, dominantScript, features);
    }

    /// <summary>
    /// Shape text with automatic script detection and font fallback.
    /// Returns multiple glyph runs — one per script segment — each with the
    /// correct HarfBuzz script, direction, and potentially a different font family.
    /// <para>
    /// <b>Limitation:</b> Segments are positioned left-to-right in sequence order.
    /// RTL segments (Arabic, Hebrew) are shaped correctly internally, but mixed
    /// LTR/RTL text in a single line requires the Unicode BiDi algorithm (UAX #9)
    /// for proper visual reordering, which is not yet implemented.
    /// </para>
    /// </summary>
    public List<ShapedGlyphRun> ShapeMultiScript(string text, string fontFamily, float fontSize, int fontWeight, bool italic)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var segments = TextSegmenter.Segment(text);
        if (segments.Count == 0)
            return [];

        // Fast path: single segment = single run
        if (segments.Count == 1)
            return [ShapeSegment(segments[0], fontFamily, fontSize, fontWeight, italic)];

        var runs = new List<ShapedGlyphRun>(segments.Count);
        float offsetX = 0;

        foreach (var segment in segments)
        {
            var run = ShapeSegment(segment, fontFamily, fontSize, fontWeight, italic);

            // Offset positions by accumulated advance
            if (offsetX > 0 && run.Positions.Length > 0)
            {
                var adjustedPositions = new float[run.Positions.Length];
                Array.Copy(run.Positions, adjustedPositions, run.Positions.Length);
                for (int i = 0; i < adjustedPositions.Length; i += 2)
                    adjustedPositions[i] += offsetX;
                run = new ShapedGlyphRun(run.GlyphIds, adjustedPositions, run.Advances, run.TotalWidth,
                    run.FontFamily, run.FontSize, run.FontWeight, run.Italic);
            }

            runs.Add(run);
            offsetX += run.TotalWidth;
        }

        return runs;
    }

    private ShapedGlyphRun ShapeSegment(TextSegment segment, string primaryFamily, float fontSize, int fontWeight, bool italic)
    {
        // Determine if we should use a fallback font for this script category
        string fontFamily = primaryFamily;
        string? fallbackCategory = segment.Script switch
        {
            ScriptCategory.Emoji => "emoji",
            ScriptCategory.Symbol => "symbol",
            _ => null
        };

        // For emoji/symbol segments, try the fallback font
        if (fallbackCategory != null)
        {
            var fallbackTypeface = FallbackTypefaceResolver?.Invoke(fallbackCategory);
            if (fallbackTypeface != null)
                fontFamily = fallbackTypeface.FamilyName;
        }

        // Shape with the correct script and direction
        return ShapeWithScript(segment.Text, fontFamily, fontSize, fontWeight, italic, segment.Script);
    }

    /// <summary>
    /// Shape text with an explicit script category (used by multi-script pipeline).
    /// </summary>
    private ShapedGlyphRun ShapeWithScript(string text, string fontFamily, float fontSize, int fontWeight, bool italic, ScriptCategory script, Feature[]? features = null)
    {
        if (string.IsNullOrEmpty(text))
            return new ShapedGlyphRun([], [], [], 0, fontFamily, fontSize, fontWeight, italic);

        var cached = GetOrCreateFont(fontFamily, fontWeight, italic);

        int upem = cached.Face.UnitsPerEm;
        if (upem <= 0) upem = 1000;
        float scale = fontSize / upem;

        using var buffer = new HarfBuzzSharp.Buffer();
        buffer.AddUtf16(text);
        buffer.Direction = UnicodeScript.GetDirection(script);
        buffer.Script = UnicodeScript.GetHarfBuzzScript(script);
        buffer.Language = Language.Default;

        var effectiveFeatures = features ?? _defaultFeatures;
        cached.Font.Shape(buffer, effectiveFeatures);

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
            positions[i * 2 + 1] = cursorY - yOffset;
            advances[i] = xAdvance;

            cursorX += xAdvance;
            cursorY += yAdvance;
        }

        return new ShapedGlyphRun(glyphIds, positions, advances, cursorX,
            fontFamily, fontSize, fontWeight, italic);
    }

    /// <summary>
    /// Determine the dominant (most frequent non-Common) script in a text string.
    /// </summary>
    private static ScriptCategory GetDominantScript(string text)
    {
        ScriptCategory dominant = ScriptCategory.Latin;
        int i = 0;
        while (i < text.Length)
        {
            var sc = UnicodeScript.Classify(text, i);
            if (sc != ScriptCategory.Common && sc != ScriptCategory.Unknown)
            {
                dominant = sc;
                break;
            }
            i += char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? 2 : 1;
        }
        return dominant;
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

            // Do NOT use Blob.FromStream — it has a memory safety bug in HarfBuzzSharp 8.3.1.3:
            // it pins the byte array only during a fixed block, then returns a blob referencing
            // now-unpinned (GC-movable) memory, causing access violations or garbled glyphs.
            // Instead, pin the array for the blob's lifetime via GCHandle.
            var pinned = GCHandle.Alloc(fontData, GCHandleType.Pinned);
            Blob blob;
            try
            {
                blob = new Blob(
                    pinned.AddrOfPinnedObject(),
                    fontData.Length,
                    MemoryMode.ReadOnly,
                    () => pinned.Free());
            }
            catch
            {
                pinned.Free();
                throw;
            }
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
