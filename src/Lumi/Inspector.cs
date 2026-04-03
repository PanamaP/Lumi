using Lumi.Core;
using SkiaSharp;

namespace Lumi;

/// <summary>
/// Debug overlay that visualizes element bounds, box model, and computed styles.
/// Toggle with F12 during development.
/// </summary>
public sealed class Inspector
{
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Toggle the inspector overlay on/off.
    /// </summary>
    public void Toggle() => IsEnabled = !IsEnabled;

    /// <summary>
    /// Draw the inspector overlay on top of the rendered scene.
    /// </summary>
    public void Draw(SKCanvas canvas, Element root, Element? hoveredElement, int canvasWidth, int canvasHeight)
    {
        if (!IsEnabled) return;

        int saveCount = canvas.Save();

        // LayoutBox coordinates are ABSOLUTE (computed by YogaLayoutEngine).
        // For elements inside scroll containers, we need to subtract the
        // accumulated scroll offset of all ancestor scroll containers.
        DrawElementBounds(canvas, root, 0, 0);

        if (hoveredElement != null)
        {
            var (scrollX, scrollY) = GetAncestorScrollOffset(hoveredElement);
            DrawBoxModel(canvas, hoveredElement, scrollX, scrollY);
            DrawTooltip(canvas, hoveredElement, canvasWidth, canvasHeight);
        }

        canvas.RestoreToCount(saveCount);
    }

    /// <summary>
    /// Get the total scroll offset from all scroll-container ancestors.
    /// </summary>
    private static (float X, float Y) GetAncestorScrollOffset(Element element)
    {
        float sx = 0, sy = 0;
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent.ComputedStyle.Overflow == Overflow.Scroll)
            {
                sx += parent.ScrollLeft;
                sy += parent.ScrollTop;
            }
            parent = parent.Parent;
        }
        return (sx, sy);
    }

    /// <summary>
    /// Draw semi-transparent outlines around all elements recursively.
    /// LayoutBox coords are absolute, so we only adjust for scroll offsets.
    /// </summary>
    private static void DrawElementBounds(SKCanvas canvas, Element element, float scrollOffsetX, float scrollOffsetY)
    {
        var box = element.LayoutBox;
        float drawX = box.X - scrollOffsetX;
        float drawY = box.Y - scrollOffsetY;

        if (box.Width > 0 && box.Height > 0 && element.ComputedStyle.Display != DisplayMode.None)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(0x38, 0xBD, 0xF8, 60),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };
            canvas.DrawRect(drawX, drawY, box.Width, box.Height, paint);
        }

        // If this element is a scroll container, add its scroll offset for children
        float childScrollX = scrollOffsetX;
        float childScrollY = scrollOffsetY;
        if (element.ComputedStyle.Overflow == Overflow.Scroll)
        {
            childScrollX += element.ScrollLeft;
            childScrollY += element.ScrollTop;
        }

        foreach (var child in element.Children)
        {
            DrawElementBounds(canvas, child, childScrollX, childScrollY);
        }
    }

    /// <summary>
    /// Draw box model visualization for the hovered element:
    /// margin (orange), padding (green), content (blue).
    /// </summary>
    private static void DrawBoxModel(SKCanvas canvas, Element element, float scrollX, float scrollY)
    {
        var box = element.LayoutBox;
        float absX = box.X - scrollX;
        float absY = box.Y - scrollY;

        var style = element.ComputedStyle;
        var margin = style.Margin;
        var padding = style.Padding;

        // Margin area (orange)
        using (var marginPaint = new SKPaint
        {
            Color = new SKColor(0xFF, 0xA5, 0x00, 50),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            // Top margin
            canvas.DrawRect(absX - margin.Left, absY - margin.Top,
                box.Width + margin.Left + margin.Right, margin.Top, marginPaint);
            // Bottom margin
            canvas.DrawRect(absX - margin.Left, absY + box.Height,
                box.Width + margin.Left + margin.Right, margin.Bottom, marginPaint);
            // Left margin
            canvas.DrawRect(absX - margin.Left, absY,
                margin.Left, box.Height, marginPaint);
            // Right margin
            canvas.DrawRect(absX + box.Width, absY,
                margin.Right, box.Height, marginPaint);
        }

        // Padding area (green)
        using (var paddingPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0xC8, 0x53, 50),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            // Top padding
            canvas.DrawRect(absX, absY, box.Width, padding.Top, paddingPaint);
            // Bottom padding
            canvas.DrawRect(absX, absY + box.Height - padding.Bottom,
                box.Width, padding.Bottom, paddingPaint);
            // Left padding
            canvas.DrawRect(absX, absY + padding.Top,
                padding.Left, box.Height - padding.Top - padding.Bottom, paddingPaint);
            // Right padding
            canvas.DrawRect(absX + box.Width - padding.Right, absY + padding.Top,
                padding.Right, box.Height - padding.Top - padding.Bottom, paddingPaint);
        }

        // Content area (blue)
        using (var contentPaint = new SKPaint
        {
            Color = new SKColor(0x42, 0xA5, 0xF5, 50),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            canvas.DrawRect(
                absX + padding.Left,
                absY + padding.Top,
                box.Width - padding.Left - padding.Right,
                box.Height - padding.Top - padding.Bottom,
                contentPaint);
        }

        // Highlight border
        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0x42, 0xA5, 0xF5, 200),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true
        };
        canvas.DrawRect(absX, absY, box.Width, box.Height, borderPaint);
    }

    /// <summary>
    /// Draw a tooltip panel showing element metadata and computed styles.
    /// </summary>
    private static void DrawTooltip(SKCanvas canvas, Element element, int canvasWidth, int canvasHeight)
    {
        var style = element.ComputedStyle;
        var box = element.LayoutBox;

        var lines = new List<string>
        {
            element.ToString(),
            $"size: {box.Width:F0} × {box.Height:F0}",
            $"font: {style.FontFamily} {style.FontSize:F0}px",
            $"color: rgba({style.Color.R},{style.Color.G},{style.Color.B},{style.Color.A})",
            $"margin: {style.Margin.Top:F0} {style.Margin.Right:F0} {style.Margin.Bottom:F0} {style.Margin.Left:F0}",
            $"padding: {style.Padding.Top:F0} {style.Padding.Right:F0} {style.Padding.Bottom:F0} {style.Padding.Left:F0}"
        };

        float lineHeight = 16f;
        float paddingH = 10f;
        float paddingV = 8f;

        using var font = new SKFont(SKTypeface.FromFamilyName("Consolas"), 12f);
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };

        float maxWidth = 0;
        foreach (var line in lines)
        {
            float w = font.MeasureText(line, textPaint);
            if (w > maxWidth) maxWidth = w;
        }

        float tooltipW = maxWidth + paddingH * 2;
        float tooltipH = lines.Count * lineHeight + paddingV * 2;

        // Position tooltip in top-right corner, offset from edges
        float tooltipX = canvasWidth - tooltipW - 10;
        float tooltipY = 10;

        // Clamp to visible area
        if (tooltipX < 10) tooltipX = 10;
        if (tooltipY + tooltipH > canvasHeight - 10) tooltipY = canvasHeight - tooltipH - 10;

        // Background
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(0x1E, 0x1E, 0x1E, 230),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(new SKRect(tooltipX, tooltipY, tooltipX + tooltipW, tooltipY + tooltipH), 6, 6, bgPaint);

        // Border
        using var borderPaint = new SKPaint
        {
            Color = new SKColor(0x42, 0xA5, 0xF5, 180),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        canvas.DrawRoundRect(new SKRect(tooltipX, tooltipY, tooltipX + tooltipW, tooltipY + tooltipH), 6, 6, borderPaint);

        // Text
        float textY = tooltipY + paddingV + 12f;
        foreach (var line in lines)
        {
            canvas.DrawText(line, tooltipX + paddingH, textY, SKTextAlign.Left, font, textPaint);
            textY += lineHeight;
        }
    }
}
