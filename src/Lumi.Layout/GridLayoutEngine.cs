using System.Globalization;
using System.Text.RegularExpressions;
using Lumi.Core;

namespace Lumi.Layout;

/// <summary>
/// Standalone CSS Grid layout engine supporting auto-placement (row-major).
/// Works alongside Yoga for elements with <c>display: grid</c>.
/// </summary>
public static class GridLayoutEngine
{
    private static readonly Regex RepeatRegex = new(@"repeat\(\s*(\d+)\s*,\s*(.+?)\s*\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    internal enum TrackSizeKind { Pixel, Fractional, Auto }

    internal readonly record struct TrackSize(TrackSizeKind Kind, float Value);

    /// <summary>
    /// Calculate grid layout for an element and position its children.
    /// </summary>
    public static void CalculateGridLayout(Element element, float availableWidth, float availableHeight)
    {
        var style = element.ComputedStyle;
        var children = element.Children;
        if (children.Count == 0) return;

        float gap = ResolveGap(style);
        var columns = ParseTrackDefinitions(style.GridTemplateColumns);
        var rows = ParseTrackDefinitions(style.GridTemplateRows);

        // Default to a single column if none specified
        if (columns.Count == 0)
            columns.Add(new TrackSize(TrackSizeKind.Fractional, 1));

        // Auto-generate rows to fit all children
        int columnCount = columns.Count;
        int neededRows = (children.Count + columnCount - 1) / columnCount;
        while (rows.Count < neededRows)
            rows.Add(new TrackSize(TrackSizeKind.Auto, 0));

        float paddingH = style.Padding.Left + style.Padding.Right;
        float paddingV = style.Padding.Top + style.Padding.Bottom;
        float innerWidth = availableWidth - paddingH;
        float innerHeight = availableHeight - paddingV;

        float[] colWidths = ResolveTracks(columns, innerWidth, gap, columnCount, children, isColumn: true);
        float[] rowHeights = ResolveTracks(rows, innerHeight, gap, columnCount, children, isColumn: false);

        // Place children in row-major order
        float offsetY = element.LayoutBox.Y + style.Padding.Top;
        int childIndex = 0;

        for (int r = 0; r < rowHeights.Length && childIndex < children.Count; r++)
        {
            float offsetX = element.LayoutBox.X + style.Padding.Left;

            for (int c = 0; c < colWidths.Length && childIndex < children.Count; c++)
            {
                var child = children[childIndex++];
                child.LayoutBox = new LayoutBox(offsetX, offsetY, colWidths[c], rowHeights[r]);
                offsetX += colWidths[c] + gap;
            }

            offsetY += rowHeights[r] + gap;
        }
    }

    internal static float ResolveGap(ComputedStyle style)
    {
        if (style.GridGap > 0) return style.GridGap;
        if (style.Gap > 0) return style.Gap;
        return 0;
    }

    internal static List<TrackSize> ParseTrackDefinitions(string? template)
    {
        var result = new List<TrackSize>();
        if (string.IsNullOrWhiteSpace(template)) return result;

        template = template.Trim();

        // Expand repeat() functions first
        template = ExpandRepeat(template);

        foreach (var token in SplitTokens(template))
        {
            if (TryParseTrackToken(token, out var track))
                result.Add(track);
        }

        return result;
    }

    private static string ExpandRepeat(string template)
    {
        try
        {
            // Match repeat(N, trackDef) — supports nested tokens like "1fr 100px"
            return RepeatRegex.Replace(template, match =>
            {
                int count = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (count > 10000)
                {
                    System.Diagnostics.Debug.WriteLine($"[Lumi.Layout] CSS repeat() count {count} exceeds maximum; capped to 10000");
                    count = 10000;
                }
                string trackDef = match.Groups[2].Value.Trim();
                return string.Join(" ", Enumerable.Repeat(trackDef, count));
            });
        }
        catch (RegexMatchTimeoutException)
        {
            System.Diagnostics.Debug.WriteLine("[Lumi.Layout] CSS repeat() regex timed out; using template as-is");
            return template;
        }
    }

    private static IEnumerable<string> SplitTokens(string template) =>
        template.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool TryParseTrackToken(string token, out TrackSize track)
    {
        token = token.Trim().ToLowerInvariant();

        if (token == "auto")
        {
            track = new TrackSize(TrackSizeKind.Auto, 0);
            return true;
        }

        if (token.EndsWith("fr") &&
            float.TryParse(token[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out float fr))
        {
            track = new TrackSize(TrackSizeKind.Fractional, fr);
            return true;
        }

        if (token.EndsWith("px") &&
            float.TryParse(token[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px))
        {
            track = new TrackSize(TrackSizeKind.Pixel, px);
            return true;
        }

        // Bare number treated as pixels
        if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float bare))
        {
            track = new TrackSize(TrackSizeKind.Pixel, bare);
            return true;
        }

        track = default;
        return false;
    }

    private static float[] ResolveTracks(
        List<TrackSize> definitions, float available, float gap,
        int columnCount, IReadOnlyList<Element> children, bool isColumn)
    {
        int count = definitions.Count;
        float[] sizes = new float[count];
        float totalGap = gap * Math.Max(0, count - 1);
        float remaining = available - totalGap;

        // First pass: resolve fixed and auto tracks
        float totalFr = 0;
        for (int i = 0; i < count; i++)
        {
            var def = definitions[i];
            switch (def.Kind)
            {
                case TrackSizeKind.Pixel:
                    sizes[i] = def.Value;
                    remaining -= def.Value;
                    break;
                case TrackSizeKind.Auto:
                    float autoSize = MeasureAutoTrack(i, count, columnCount, children, isColumn);
                    sizes[i] = autoSize;
                    remaining -= autoSize;
                    break;
                case TrackSizeKind.Fractional:
                    totalFr += def.Value;
                    break;
            }
        }

        // Second pass: distribute remaining space to fr tracks
        remaining = Math.Max(0, remaining);
        if (totalFr > 0)
        {
            float perFr = remaining / totalFr;
            for (int i = 0; i < count; i++)
            {
                if (definitions[i].Kind == TrackSizeKind.Fractional)
                    sizes[i] = perFr * definitions[i].Value;
            }
        }

        return sizes;
    }

    /// <summary>
    /// Measure the natural size of children occupying a given auto track.
    /// For auto columns, looks at the max width of children in that column.
    /// For auto rows, looks at the max height of children in that row.
    /// Falls back to 50 if no children provide sizing info.
    /// </summary>
    private static float MeasureAutoTrack(
        int trackIndex, int trackCount, int columnCount,
        IReadOnlyList<Element> children, bool isColumn)
    {
        float maxSize = 0;
        for (int i = 0; i < children.Count; i++)
        {
            int childTrack = isColumn ? i % columnCount : i / columnCount;
            if (childTrack != trackIndex) continue;

            var cs = children[i].ComputedStyle;
            float size = isColumn
                ? (float.IsNaN(children[i].LayoutBox.Width) || children[i].LayoutBox.Width <= 0
                    ? (float.IsNaN(cs.Width) ? 0 : cs.Width)
                    : children[i].LayoutBox.Width)
                : (float.IsNaN(children[i].LayoutBox.Height) || children[i].LayoutBox.Height <= 0
                    ? (float.IsNaN(cs.Height) ? 0 : cs.Height)
                    : children[i].LayoutBox.Height);
            maxSize = Math.Max(maxSize, size);
        }

        return maxSize > 0 ? maxSize : 50;
    }
}
