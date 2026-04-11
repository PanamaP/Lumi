namespace Lumi.Text;

using HarfBuzzSharp;

/// <summary>
/// Broad Unicode script categories used by the text shaping pipeline
/// to select direction, HarfBuzz script tag, and fallback fonts.
/// </summary>
public enum ScriptCategory
{
    Latin,
    Arabic,
    Hebrew,
    CJK,
    Devanagari,
    Thai,
    Emoji,
    Symbol,
    Common,
    Unknown
}

/// <summary>
/// Provides lightweight Unicode script classification for text shaping.
/// Covers the major script blocks — not exhaustive, but sufficient for
/// choosing direction, HarfBuzz script tags, and fallback fonts.
/// </summary>
public static class UnicodeScript
{
    // Cached HarfBuzz Script objects to avoid repeated allocation.
    private static readonly Script ScriptLatin      = Script.Parse("Latn");
    private static readonly Script ScriptArabic     = Script.Parse("Arab");
    private static readonly Script ScriptHebrew     = Script.Parse("Hebr");
    private static readonly Script ScriptHani       = Script.Parse("Hani");
    private static readonly Script ScriptDevanagari = Script.Parse("Deva");
    private static readonly Script ScriptThai       = Script.Parse("Thai");
    private static readonly Script ScriptCommon     = Script.Parse("Zyyy");

    /// <summary>
    /// Classify a Unicode codepoint into a broad script category.
    /// </summary>
    public static ScriptCategory Classify(int codepoint)
    {
        // ── Special-case individual codepoints ──────────────────────────
        if (codepoint == 0xFE0F) return ScriptCategory.Emoji;  // Variation Selector-16
        if (codepoint == 0x200D) return ScriptCategory.Emoji;  // Zero Width Joiner

        // ── Basic Latin (U+0000 – U+007F) ───────────────────────────────
        if (codepoint <= 0x007F)
        {
            // Digits
            if (codepoint >= 0x30 && codepoint <= 0x39)
                return ScriptCategory.Common;

            // ASCII punctuation & control characters
            if (codepoint < 0x41 ||                            // before 'A'
                (codepoint > 0x5A && codepoint < 0x61) ||      // between 'Z' and 'a'
                codepoint > 0x7A)                              // after 'z'
                return ScriptCategory.Common;

            return ScriptCategory.Latin;
        }

        // ── Latin Extended blocks (U+0080 – U+024F) ─────────────────────
        if (codepoint <= 0x024F) return ScriptCategory.Latin;

        // ── Greek (U+0370 – U+03FF) — treated as Latin for LTR shaping ──
        if (codepoint >= 0x0370 && codepoint <= 0x03FF) return ScriptCategory.Latin;

        // ── Cyrillic (U+0400 – U+04FF) — LTR, similar shaping ───────────
        if (codepoint >= 0x0400 && codepoint <= 0x04FF) return ScriptCategory.Latin;

        // ── Hebrew (U+0590 – U+05FF) ────────────────────────────────────
        if (codepoint >= 0x0590 && codepoint <= 0x05FF) return ScriptCategory.Hebrew;

        // ── Arabic (U+0600 – U+06FF) ────────────────────────────────────
        if (codepoint >= 0x0600 && codepoint <= 0x06FF) return ScriptCategory.Arabic;

        // ── Devanagari (U+0900 – U+097F) ────────────────────────────────
        if (codepoint >= 0x0900 && codepoint <= 0x097F) return ScriptCategory.Devanagari;

        // ── Thai (U+0E00 – U+0E7F) ─────────────────────────────────────
        if (codepoint >= 0x0E00 && codepoint <= 0x0E7F) return ScriptCategory.Thai;

        // ── General Punctuation (U+2000 – U+206F) ───────────────────────
        if (codepoint >= 0x2000 && codepoint <= 0x206F) return ScriptCategory.Common;

        // ── Arrows, Math Operators, Technical, Misc Symbols (U+2190 – U+27FF) ──
        if (codepoint >= 0x2190 && codepoint <= 0x27FF) return ScriptCategory.Symbol;

        // ── CJK unified range (U+2E80 – U+9FFF) ────────────────────────
        if (codepoint >= 0x2E80 && codepoint <= 0x9FFF) return ScriptCategory.CJK;

        // ── CJK Compatibility Forms (U+FE30 – U+FE4F) ──────────────────
        if (codepoint >= 0xFE30 && codepoint <= 0xFE4F) return ScriptCategory.CJK;

        // ── Emoji & pictographic symbols (U+1F000 – U+1FAFF) ────────────
        if (codepoint >= 0x1F000 && codepoint <= 0x1FAFF) return ScriptCategory.Emoji;

        return ScriptCategory.Common;
    }

    /// <summary>
    /// Classify the character (or surrogate pair) at <paramref name="index"/> in
    /// <paramref name="text"/>.
    /// </summary>
    public static ScriptCategory Classify(string text, int index)
    {
        int codepoint = char.IsHighSurrogate(text[index]) && index + 1 < text.Length
            ? char.ConvertToUtf32(text[index], text[index + 1])
            : text[index];
        return Classify(codepoint);
    }

    /// <summary>
    /// Returns the text direction appropriate for the given script.
    /// </summary>
    public static Direction GetDirection(ScriptCategory script)
    {
        return script switch
        {
            ScriptCategory.Arabic => Direction.RightToLeft,
            ScriptCategory.Hebrew => Direction.RightToLeft,
            _                     => Direction.LeftToRight
        };
    }

    /// <summary>
    /// Maps a <see cref="ScriptCategory"/> to the corresponding HarfBuzz
    /// <see cref="Script"/> tag used during shaping.
    /// </summary>
    public static Script GetHarfBuzzScript(ScriptCategory script)
    {
        return script switch
        {
            ScriptCategory.Latin      => ScriptLatin,
            ScriptCategory.Arabic     => ScriptArabic,
            ScriptCategory.Hebrew     => ScriptHebrew,
            ScriptCategory.CJK        => ScriptHani,
            ScriptCategory.Devanagari => ScriptDevanagari,
            ScriptCategory.Thai       => ScriptThai,
            _                         => ScriptCommon
        };
    }

    /// <summary>
    /// Quick check whether a codepoint falls in a known emoji range.
    /// Useful for fallback font selection.
    /// </summary>
    public static bool IsEmoji(int codepoint)
    {
        if (codepoint == 0xFE0F || codepoint == 0x200D)
            return true;

        return codepoint >= 0x1F000 && codepoint <= 0x1FAFF;
    }
}
