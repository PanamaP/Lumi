namespace Lumi.Text;

/// <summary>
/// A contiguous segment of text sharing the same script category.
/// </summary>
public readonly record struct TextSegment(string Text, ScriptCategory Script, int StartIndex);

/// <summary>
/// Splits text into segments by Unicode script so each can be shaped
/// with the correct HarfBuzz script, direction, and font.
/// </summary>
public static class TextSegmenter
{
    /// <summary>
    /// Segment <paramref name="text"/> into runs of uniform script.
    /// Adjacent <see cref="ScriptCategory.Common"/> characters are merged into the
    /// surrounding script (e.g., spaces and punctuation between Latin words stay Latin).
    /// </summary>
    public static List<TextSegment> Segment(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var raw = BuildRawSegments(text);
        ResolveCommon(raw);
        return MergeAdjacent(text, raw);
    }

    /// <summary>
    /// Classify every character (or surrogate pair) into a raw list of
    /// (startIndex, length, script) tuples — one entry per character.
    /// </summary>
    private static List<(int Start, int Len, ScriptCategory Script)> BuildRawSegments(string text)
    {
        var entries = new List<(int, int, ScriptCategory)>(text.Length);
        int i = 0;

        while (i < text.Length)
        {
            var script = UnicodeScript.Classify(text, i);
            int charLen = char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? 2 : 1;
            entries.Add((i, charLen, script));
            i += charLen;
        }

        return entries;
    }

    /// <summary>
    /// Resolve <see cref="ScriptCategory.Common"/> entries by attaching them to the
    /// nearest strong (non-Common, non-Unknown) script. Prefers the preceding script;
    /// if none, uses the following script.
    /// </summary>
    private static void ResolveCommon(List<(int Start, int Len, ScriptCategory Script)> entries)
    {
        // Forward pass: inherit from preceding strong script
        ScriptCategory last = ScriptCategory.Common;
        for (int i = 0; i < entries.Count; i++)
        {
            var (s, l, script) = entries[i];
            if (script == ScriptCategory.Common || script == ScriptCategory.Unknown)
            {
                if (last != ScriptCategory.Common && last != ScriptCategory.Unknown)
                    entries[i] = (s, l, last);
            }
            else
            {
                last = script;
            }
        }

        // Backward pass: fill any remaining Common at the start
        last = ScriptCategory.Common;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var (s, l, script) = entries[i];
            if (script == ScriptCategory.Common || script == ScriptCategory.Unknown)
            {
                if (last != ScriptCategory.Common && last != ScriptCategory.Unknown)
                    entries[i] = (s, l, last);
            }
            else
            {
                last = script;
            }
        }
    }

    /// <summary>
    /// Merge consecutive entries with the same script into <see cref="TextSegment"/>s.
    /// </summary>
    private static List<TextSegment> MergeAdjacent(string text, List<(int Start, int Len, ScriptCategory Script)> entries)
    {
        if (entries.Count == 0)
            return [];

        var result = new List<TextSegment>();
        int segStart = entries[0].Start;
        var segScript = entries[0].Script;

        for (int i = 1; i < entries.Count; i++)
        {
            if (entries[i].Script != segScript)
            {
                int segEnd = entries[i].Start;
                result.Add(new TextSegment(text[segStart..segEnd], segScript, segStart));
                segStart = segEnd;
                segScript = entries[i].Script;
            }
        }

        // Last segment — extends to end of text
        result.Add(new TextSegment(text[segStart..], segScript, segStart));

        return result;
    }
}
