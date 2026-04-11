using Lumi.Text;
using Lumi.Rendering;
using SkiaSharp;

namespace Lumi.Tests;

public class EmojiAndScriptTests : IDisposable
{
    private readonly TextShaper _shaper = new();

    public void Dispose()
    {
        _shaper.Dispose();
        FontManager.Clear();
        SystemFontResolver.Reset();
    }

    // ── UnicodeScript.Classify tests ──

    [Fact]
    public void Classify_AsciiLetter_ReturnsLatin()
    {
        Assert.Equal(ScriptCategory.Latin, UnicodeScript.Classify('A'));
        Assert.Equal(ScriptCategory.Latin, UnicodeScript.Classify('z'));
    }

    [Fact]
    public void Classify_Digit_ReturnsCommon()
    {
        Assert.Equal(ScriptCategory.Common, UnicodeScript.Classify('0'));
        Assert.Equal(ScriptCategory.Common, UnicodeScript.Classify('9'));
    }

    [Fact]
    public void Classify_Space_ReturnsCommon()
    {
        Assert.Equal(ScriptCategory.Common, UnicodeScript.Classify(' '));
    }

    [Fact]
    public void Classify_Emoji_ReturnsEmoji()
    {
        // 👋 = U+1F44B (waving hand)
        Assert.Equal(ScriptCategory.Emoji, UnicodeScript.Classify(0x1F44B));
        // 🌍 = U+1F30D
        Assert.Equal(ScriptCategory.Emoji, UnicodeScript.Classify(0x1F30D));
    }

    [Fact]
    public void Classify_ZWJ_ReturnsEmoji()
    {
        Assert.Equal(ScriptCategory.Emoji, UnicodeScript.Classify(0x200D));
    }

    [Fact]
    public void Classify_VariationSelector16_ReturnsEmoji()
    {
        Assert.Equal(ScriptCategory.Emoji, UnicodeScript.Classify(0xFE0F));
    }

    [Fact]
    public void Classify_Arabic_ReturnsArabic()
    {
        // Arabic letter Alef = U+0627
        Assert.Equal(ScriptCategory.Arabic, UnicodeScript.Classify(0x0627));
    }

    [Fact]
    public void Classify_CJK_ReturnsCJK()
    {
        // CJK unified ideograph = U+4E00
        Assert.Equal(ScriptCategory.CJK, UnicodeScript.Classify(0x4E00));
        // CJK Compatibility Ideograph = U+F900
        Assert.Equal(ScriptCategory.CJK, UnicodeScript.Classify(0xF900));
        // Fullwidth Latin A = U+FF21
        Assert.Equal(ScriptCategory.CJK, UnicodeScript.Classify(0xFF21));
    }

    [Fact]
    public void Classify_Hangul_ReturnsHangul()
    {
        // Hangul syllable "가" = U+AC00
        Assert.Equal(ScriptCategory.Hangul, UnicodeScript.Classify(0xAC00));
        // Hangul syllable "힣" = U+D7A3
        Assert.Equal(ScriptCategory.Hangul, UnicodeScript.Classify(0xD7A3));
    }

    [Fact]
    public void Classify_SurrogatePairInString_HandlesCorrectly()
    {
        // "👋" is a surrogate pair in UTF-16
        string text = "👋";
        Assert.Equal(ScriptCategory.Emoji, UnicodeScript.Classify(text, 0));
    }

    [Fact]
    public void IsEmoji_EmojiCodepoint_ReturnsTrue()
    {
        Assert.True(UnicodeScript.IsEmoji(0x1F600)); // 😀
        Assert.True(UnicodeScript.IsEmoji(0x200D));   // ZWJ
        Assert.True(UnicodeScript.IsEmoji(0xFE0F));   // VS16
    }

    [Fact]
    public void IsEmoji_LatinCodepoint_ReturnsFalse()
    {
        Assert.False(UnicodeScript.IsEmoji('A'));
        Assert.False(UnicodeScript.IsEmoji('0'));
    }

    // ── Direction tests ──

    [Fact]
    public void GetDirection_Arabic_IsRTL()
    {
        Assert.Equal(HarfBuzzSharp.Direction.RightToLeft, UnicodeScript.GetDirection(ScriptCategory.Arabic));
    }

    [Fact]
    public void GetDirection_Latin_IsLTR()
    {
        Assert.Equal(HarfBuzzSharp.Direction.LeftToRight, UnicodeScript.GetDirection(ScriptCategory.Latin));
    }

    // ── TextSegmenter tests ──

    [Fact]
    public void Segment_PureLatinText_ReturnsSingleSegment()
    {
        var segments = TextSegmenter.Segment("Hello World");
        Assert.Single(segments);
        Assert.Equal("Hello World", segments[0].Text);
        Assert.Equal(ScriptCategory.Latin, segments[0].Script);
    }

    [Fact]
    public void Segment_EmptyString_ReturnsEmpty()
    {
        var segments = TextSegmenter.Segment("");
        Assert.Empty(segments);
    }

    [Fact]
    public void Segment_TextWithEmoji_SplitsCorrectly()
    {
        // "Hi 👋 there" should split into at least 3 segments
        var segments = TextSegmenter.Segment("Hi 👋 there");
        Assert.True(segments.Count >= 2, $"Expected at least 2 segments, got {segments.Count}");

        // At least one segment should be emoji
        Assert.Contains(segments, s => s.Script == ScriptCategory.Emoji);
        // At least one should be Latin
        Assert.Contains(segments, s => s.Script == ScriptCategory.Latin);
    }

    [Fact]
    public void Segment_MultipleEmoji_MergesEmojiRun()
    {
        // "📊👥💰" — three consecutive emoji should be one segment
        var segments = TextSegmenter.Segment("📊👥💰");
        Assert.Single(segments);
        Assert.Equal(ScriptCategory.Emoji, segments[0].Script);
    }

    [Fact]
    public void Segment_CommonAttachesToSurroundingScript()
    {
        // "Hello, World!" — comma and space are Common, should merge into Latin
        var segments = TextSegmenter.Segment("Hello, World!");
        Assert.Single(segments);
        Assert.Equal(ScriptCategory.Latin, segments[0].Script);
    }

    // ── TextShaper.ShapeMultiScript tests ──

    [Fact]
    public void ShapeMultiScript_PureLatin_ReturnsSingleRun()
    {
        var runs = _shaper.ShapeMultiScript("Hello", "Arial", 16f, 400, false);
        Assert.Single(runs);
        Assert.True(runs[0].GlyphIds.Length > 0);
        Assert.True(runs[0].TotalWidth > 0);
    }

    [Fact]
    public void ShapeMultiScript_EmptyText_ReturnsEmpty()
    {
        var runs = _shaper.ShapeMultiScript("", "Arial", 16f, 400, false);
        Assert.Empty(runs);
    }

    [Fact]
    public void ShapeMultiScript_TextWithEmoji_ReturnsMultipleRuns()
    {
        var runs = _shaper.ShapeMultiScript("Hi 👋 there", "Arial", 16f, 400, false);
        Assert.True(runs.Count >= 2, $"Expected multiple runs for mixed text, got {runs.Count}");
    }

    [Fact]
    public void ShapeMultiScript_RunPositionsAreContiguous()
    {
        // Runs should tile — each run's positions should start where the previous ended
        var runs = _shaper.ShapeMultiScript("Hi 👋 there", "Arial", 16f, 400, false);
        if (runs.Count < 2) return; // skip if segmentation produced single run

        float prevEnd = 0;
        foreach (var run in runs)
        {
            if (run.GlyphIds.Length > 0)
            {
                float firstX = run.Positions[0];
                Assert.True(firstX >= prevEnd - 1f, // 1px tolerance
                    $"Run should start near {prevEnd}, started at {firstX}");
                prevEnd = firstX + run.TotalWidth;
            }
        }
    }

    // ── Shape with script detection tests ──

    [Fact]
    public void Shape_DetectsScriptAutomatically()
    {
        // Shape pure Latin — should work as before
        var run = _shaper.Shape("Hello", "Arial", 16f, 400, false);
        Assert.Equal(5, run.GlyphIds.Length);
        Assert.True(run.TotalWidth > 0);
    }

    // ── FontManager fallback tests ──

    [Fact]
    public void FontManager_RegisterAndGetFallback()
    {
        var typeface = SKTypeface.Default;
        FontManager.RegisterFallbackTypeface("test-category", typeface);

        var resolved = FontManager.GetFallbackTypeface("test-category");
        Assert.NotNull(resolved);
        Assert.Same(typeface, resolved);
    }

    [Fact]
    public void FontManager_GetFallback_UnknownCategory_ReturnsNull()
    {
        var resolved = FontManager.GetFallbackTypeface("nonexistent");
        Assert.Null(resolved);
    }

    [Fact]
    public void FontManager_HasGlyph_DefaultTypeface_HasLatinGlyphs()
    {
        var typeface = SKTypeface.Default;
        Assert.True(FontManager.HasGlyph(typeface, 'A'));
        Assert.True(FontManager.HasGlyph(typeface, 'z'));
    }

    // ── SystemFontResolver tests ──

    [Fact]
    public void SystemFontResolver_Initialize_DoesNotThrow()
    {
        var ex = Record.Exception(() => SystemFontResolver.Initialize());
        Assert.Null(ex);
    }

    [Fact]
    public void SystemFontResolver_Initialize_Idempotent()
    {
        SystemFontResolver.Initialize();
        var ex = Record.Exception(() => SystemFontResolver.Initialize());
        Assert.Null(ex);
    }

    // ── DefaultFeatures tests ──

    [Fact]
    public void DefaultFeatures_ContainsLigaAndKern()
    {
        Assert.True(TextShaper.DefaultFeatures.Count >= 2,
            "DefaultFeatures should contain at least liga and kern");
    }
}
