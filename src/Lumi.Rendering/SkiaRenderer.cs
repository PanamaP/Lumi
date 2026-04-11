namespace Lumi.Rendering;

using SkiaSharp;
using Lumi.Core;

public class SkiaRenderer : IDisposable
{
    private SKBitmap? _bitmap;
    private SKCanvas? _canvas;
    private SKSurface? _gpuSurface;
    private GRContext? _grContext;
    private GRGlInterface? _glInterface;
    private GRBackendRenderTarget? _renderTarget;
    private int _width;
    private int _height;
    private bool _useGpu;

    public ImageCache ImageCache { get; } = new();

    public int Width => _width;
    public int Height => _height;
    public int Pitch => _width * 4;
    public bool IsGpuAccelerated => _useGpu && _grContext != null;

    /// <summary>
    /// Exposes the current Skia canvas for overlay drawing (e.g. inspector).
    /// </summary>
    public SKCanvas? Canvas => _canvas;

    /// <summary>
    /// Initialize GPU-accelerated rendering via OpenGL.
    /// Call after an OpenGL context has been made current.
    /// </summary>
    public void InitializeGpu()
    {
        _glInterface = GRGlInterface.Create();
        if (_glInterface == null || !_glInterface.Validate())
        {
            _glInterface?.Dispose();
            _glInterface = null;
            _useGpu = false;
            return;
        }

        _grContext = GRContext.CreateGl(_glInterface);
        if (_grContext == null)
        {
            _glInterface.Dispose();
            _glInterface = null;
            _useGpu = false;
            return;
        }

        _useGpu = true;
    }

    public void EnsureSize(int width, int height)
    {
        if (_width == width && _height == height)
        {
            if (_useGpu && _gpuSurface != null) return;
            if (!_useGpu && _bitmap != null) return;
        }

        _width = width;
        _height = height;

        if (_useGpu && _grContext != null)
        {
            EnsureGpuSurface(width, height);
        }
        else
        {
            EnsureCpuSurface(width, height);
        }
    }

    private void EnsureGpuSurface(int width, int height)
    {
        _gpuSurface?.Dispose();
        _renderTarget?.Dispose();
        _canvas = null;

        // Query the current OpenGL framebuffer ID
        int framebufferId = 0;
        unsafe
        {
            // GL_FRAMEBUFFER_BINDING = 0x8CA6
            typedef_glGetIntegerv(0x8CA6, &framebufferId);
        }

        var fbInfo = new GRGlFramebufferInfo((uint)framebufferId, SKColorType.Rgba8888.ToGlSizedFormat());
        _renderTarget = new GRBackendRenderTarget(width, height, 0, 8, fbInfo);
        _gpuSurface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

        if (_gpuSurface == null)
        {
            // Fallback to CPU if GPU surface creation fails
            _useGpu = false;
            EnsureCpuSurface(width, height);
            return;
        }

        _canvas = _gpuSurface.Canvas;
    }

    // Get glGetIntegerv via the GL interface
    private unsafe void typedef_glGetIntegerv(int pname, int* data)
    {
        // Use a simple P/Invoke fallback — the GL context is already current
        [System.Runtime.InteropServices.DllImport("opengl32.dll")]
        static extern void glGetIntegerv(int pname, int* @params);
        glGetIntegerv(pname, data);
    }

    private void EnsureCpuSurface(int width, int height)
    {
        _canvas?.Dispose();
        _bitmap?.Dispose();
        _gpuSurface?.Dispose();
        _gpuSurface = null;

        _bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_bitmap);
    }

    public void Paint(Element root)
    {
        if (_canvas == null) return;

        _canvas.Clear(SKColors.White);
        PaintElement(_canvas, root, 0, 0);

        if (_useGpu && _grContext != null)
        {
            _canvas.Flush();
            _grContext.Flush();
        }
    }

    /// <summary>
    /// Paint only the dirty regions of the element tree.
    /// Falls back to full repaint if dirty coverage exceeds 50%.
    /// </summary>
    public void PaintDirtyRegions(Element root, DirtyRegionTracker tracker)
    {
        if (_canvas == null) return;

        if (!tracker.HasDirtyRegions || tracker.CoverageRatio(_width, _height) > 0.5f)
        {
            // Full repaint is cheaper when most of the surface is dirty
            Paint(root);
            return;
        }

        foreach (var dirtyRect in tracker.DirtyRects)
        {
            int saveCount = _canvas.Save();
            _canvas.ClipRect(new SKRect(dirtyRect.X, dirtyRect.Y, dirtyRect.Right, dirtyRect.Bottom));
            _canvas.Clear(SKColors.White);
            PaintElement(_canvas, root, 0, 0);
            _canvas.RestoreToCount(saveCount);
        }

        if (_useGpu && _grContext != null)
        {
            _canvas.Flush();
            _grContext.Flush();
        }
    }

    public IntPtr GetPixels() => _bitmap?.GetPixels() ?? IntPtr.Zero;

    /// <summary>
    /// Export the current rendered frame as a PNG file.
    /// Works for both GPU and CPU rendering modes.
    /// </summary>
    public bool ExportPng(string filePath)
    {
        try
        {
            if (_useGpu && _gpuSurface != null)
            {
                using var image = _gpuSurface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
                return true;
            }
            else if (_bitmap != null)
            {
                using var image = SKImage.FromBitmap(_bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Render the element tree to a PNG file without needing a window.
    /// Useful for headless rendering, testing, and CI screenshots.
    /// </summary>
    public static bool RenderToPng(Element root, int width, int height, string filePath)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var renderer = new SkiaRenderer();
        renderer.EnsureSize(width, height);
        renderer.Paint(root);

        if (renderer._bitmap == null) return false;

        using var image = SKImage.FromBitmap(renderer._bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
        renderer.Dispose();
        return true;
    }

    private void PaintElement(SKCanvas canvas, Element element, float parentAbsX, float parentAbsY)
    {
        var style = element.ComputedStyle;

        if (style.Display == DisplayMode.None) return;
        if (style.Visibility == Visibility.Hidden) return;

        var box = element.LayoutBox;
        int saveCount = canvas.Save();

        // Translate by RELATIVE offset (not absolute) to avoid double-counting in nested elements
        float relX = box.X - parentAbsX;
        float relY = box.Y - parentAbsY;
        canvas.Translate(relX, relY);

        // Apply CSS transform (translate, scale, rotate, skew)
        var transform = style.Transform;
        if (!transform.IsIdentity)
        {
            float w0 = box.Width;
            float h0 = box.Height;
            float originX = style.TransformOriginX / 100f * w0;
            float originY = style.TransformOriginY / 100f * h0;

            canvas.Translate(originX, originY);
            canvas.Translate(transform.TranslateX, transform.TranslateY);
            if (transform.Rotate != 0)
                canvas.RotateDegrees(transform.Rotate);
            if (transform.ScaleX != 1 || transform.ScaleY != 1)
                canvas.Scale(transform.ScaleX, transform.ScaleY);
            if (transform.SkewX != 0 || transform.SkewY != 0)
                canvas.Skew(MathF.Tan(transform.SkewX * MathF.PI / 180f),
                            MathF.Tan(transform.SkewY * MathF.PI / 180f));
            canvas.Translate(-originX, -originY);
        }

        // Handle opacity via SaveLayer with alpha paint
        bool hasOpacity = style.Opacity < 1f;
        if (hasOpacity)
        {
            using var opacityPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, (byte)(style.Opacity * 255))
            };
            canvas.SaveLayer(opacityPaint);
        }

        float w = box.Width;
        float h = box.Height;
        var rect = new SKRect(0, 0, w, h);
        var corners = style.BorderCornerRadius;
        float radius = style.BorderRadius;
        bool hasPerCorner = corners.HasPerCorner;
        bool hasRadius = hasPerCorner || radius > 0;

        // Build rounded rect (per-corner or uniform)
        SKRoundRect? rrect = null;
        if (hasRadius)
        {
            rrect = new SKRoundRect();
            if (hasPerCorner)
            {
                rrect.SetRectRadii(rect, [
                    new SKPoint(corners.TopLeft, corners.TopLeft),
                    new SKPoint(corners.TopRight, corners.TopRight),
                    new SKPoint(corners.BottomRight, corners.BottomRight),
                    new SKPoint(corners.BottomLeft, corners.BottomLeft)
                ]);
            }
            else
            {
                rrect.SetRectRadii(rect, [
                    new SKPoint(radius, radius),
                    new SKPoint(radius, radius),
                    new SKPoint(radius, radius),
                    new SKPoint(radius, radius)
                ]);
            }
            canvas.ClipRoundRect(rrect, antialias: true);
        }

        // Paint box-shadow (before background)
        if (!style.BoxShadow.IsNone && !style.BoxShadow.Inset)
        {
            var shadow = style.BoxShadow;
            using var shadowPaint = new SKPaint
            {
                Color = shadow.Color.ToSkColor(),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateDropShadow(
                    shadow.OffsetX, shadow.OffsetY,
                    shadow.BlurRadius / 2f, shadow.BlurRadius / 2f,
                    shadow.Color.ToSkColor())
            };

            // Draw a filled rect that produces the shadow; the actual rect will be
            // overdrawn by the background paint that follows.
            if (hasRadius)
                canvas.DrawRoundRect(rect, radius, radius, shadowPaint);
            else
                canvas.DrawRect(rect, shadowPaint);
        }

        // Paint background
        if (style.BackgroundColor.A > 0)
        {
            using var bgPaint = new SKPaint
            {
                Color = style.BackgroundColor.ToSkColor(),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            if (rrect != null)
                canvas.DrawRoundRect(rrect, bgPaint);
            else
                canvas.DrawRect(rect, bgPaint);
        }

        // Paint background gradient
        if (style.BackgroundGradient is { } gradient)
        {
            var colors = gradient.Stops.Select(s => s.Color.ToSkColor()).ToArray();
            var positions = gradient.Stops.Select(s => s.Position).ToArray();

            if (colors.Length >= 2)
            {
                SKShader shader;
                if (gradient.Type == GradientType.Linear)
                {
                    var (start, end) = ComputeLinearGradientPoints(w, h, gradient.Angle);
                    shader = SKShader.CreateLinearGradient(start, end, colors, positions, SKShaderTileMode.Clamp);
                }
                else
                {
                    var center = new SKPoint(w / 2, h / 2);
                    float gradRadius = MathF.Max(w, h) / 2;
                    shader = SKShader.CreateRadialGradient(center, gradRadius, colors, positions, SKShaderTileMode.Clamp);
                }

                using var gradPaint = new SKPaint
                {
                    Shader = shader,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                if (rrect != null)
                    canvas.DrawRoundRect(rrect, gradPaint);
                else
                    canvas.DrawRect(rect, gradPaint);

                shader.Dispose();
            }
        }

        // Paint background image (after background color, before border)
        if (!string.IsNullOrEmpty(style.BackgroundImage))
        {
            var cached = ImageCache.Get(style.BackgroundImage);
            if (cached != null)
            {
                using var imgPaint = new SKPaint { IsAntialias = true };
                canvas.DrawBitmap(cached.Bitmap, rect, imgPaint);
            }
        }

        // Paint border
        var bw = style.BorderWidth;
        if (style.BorderStyle != BorderStyle.None &&
            style.BorderColor.A > 0 && (bw.Top > 0 || bw.Right > 0 || bw.Bottom > 0 || bw.Left > 0))
        {
            float avgBorder = (bw.Top + bw.Right + bw.Bottom + bw.Left) / 4f;
            float halfBorder = avgBorder / 2f;
            var borderRect = new SKRect(halfBorder, halfBorder, w - halfBorder, h - halfBorder);

            using var borderPaint = new SKPaint
            {
                Color = style.BorderColor.ToSkColor(),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = avgBorder,
                IsAntialias = true
            };

            // Apply dash pattern for dashed/dotted border styles
            switch (style.BorderStyle)
            {
                case BorderStyle.Dashed:
                    borderPaint.PathEffect = SKPathEffect.CreateDash(
                        [avgBorder * 3, avgBorder * 2], 0);
                    break;
                case BorderStyle.Dotted:
                    borderPaint.PathEffect = SKPathEffect.CreateDash(
                        [avgBorder, avgBorder * 2], 0);
                    borderPaint.StrokeCap = SKStrokeCap.Round;
                    break;
            }

            if (hasRadius)
            {
                var innerRRect = new SKRoundRect();
                if (hasPerCorner)
                {
                    innerRRect.SetRectRadii(borderRect, [
                        new SKPoint(Math.Max(0, corners.TopLeft - halfBorder), Math.Max(0, corners.TopLeft - halfBorder)),
                        new SKPoint(Math.Max(0, corners.TopRight - halfBorder), Math.Max(0, corners.TopRight - halfBorder)),
                        new SKPoint(Math.Max(0, corners.BottomRight - halfBorder), Math.Max(0, corners.BottomRight - halfBorder)),
                        new SKPoint(Math.Max(0, corners.BottomLeft - halfBorder), Math.Max(0, corners.BottomLeft - halfBorder))
                    ]);
                }
                else
                {
                    float innerRadius = Math.Max(0, radius - halfBorder);
                    innerRRect.SetRectRadii(borderRect, [
                        new SKPoint(innerRadius, innerRadius),
                        new SKPoint(innerRadius, innerRadius),
                        new SKPoint(innerRadius, innerRadius),
                        new SKPoint(innerRadius, innerRadius)
                    ]);
                }
                canvas.DrawRoundRect(innerRRect, borderPaint);
            }
            else
            {
                canvas.DrawRect(borderRect, borderPaint);
            }
        }

        // Draw focus ring if the element is focused
        if (element.IsFocused)
        {
            float offset = 2f;
            var focusRect = new SKRect(-offset, -offset, w + offset, h + offset);
            using var focusPaint = new SKPaint
            {
                Color = new SKColor(0x38, 0xBD, 0xF8, 255), // #38BDF8
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true
            };

            if (hasRadius)
            {
                float focusRadius = radius + offset;
                canvas.DrawRoundRect(focusRect, focusRadius, focusRadius, focusPaint);
            }
            else
            {
                canvas.DrawRect(focusRect, focusPaint);
            }
        }

        // Apply overflow clipping for children (Hidden and Scroll both clip)
        if (style.Overflow == Overflow.Hidden || style.Overflow == Overflow.Scroll)
        {
            canvas.ClipRect(rect);
        }

        // Apply scroll offset translation
        if (style.Overflow == Overflow.Scroll && (element.ScrollTop != 0 || element.ScrollLeft != 0))
        {
            canvas.Translate(-element.ScrollLeft, -element.ScrollTop);
        }

        // Draw text for TextElement
        if (element is TextElement textElement && !string.IsNullOrEmpty(textElement.Text))
        {
            DrawText(canvas, textElement, style, w, h);
        }

        // Draw text for InputElement (value or placeholder)
        if (element is InputElement inputElement)
        {
            DrawInputText(canvas, inputElement, style, w, h);
        }

        // Draw image for ImageElement
        if (element is ImageElement imageElement && !string.IsNullOrEmpty(imageElement.Source))
        {
            DrawImage(canvas, imageElement, w, h);
        }

        // Recursively paint children — sort by z-index so higher values paint on top
        var children = element.Children;
        bool needsSort = false;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].ComputedStyle.ZIndex != 0) { needsSort = true; break; }
        }

        if (needsSort)
        {
            var sorted = new List<Element>(children);
            sorted.Sort((a, b) => a.ComputedStyle.ZIndex.CompareTo(b.ComputedStyle.ZIndex));
            foreach (var child in sorted)
                PaintElement(canvas, child, box.X, box.Y);
        }
        else
        {
            foreach (var child in children)
                PaintElement(canvas, child, box.X, box.Y);
        }

        if (hasOpacity)
        {
            canvas.Restore();
        }

        canvas.RestoreToCount(saveCount);
    }

    private static void DrawText(SKCanvas canvas, TextElement textElement, ComputedStyle style, float width, float height)
    {
        string text = ApplyTextTransform(textElement.Text, style.TextTransform);

        // Account for padding — text content is drawn inside the padding area
        float padL = style.Padding.Left, padT = style.Padding.Top;
        float padR = style.Padding.Right, padB = style.Padding.Bottom;
        float contentW = width - padL - padR;
        float contentH = height - padT - padB;
        if (contentW <= 0) contentW = width;
        if (contentH <= 0) contentH = height;

        var layout = TextLayout.Layout(text, contentW, contentH, style);
        if (layout.Lines.Count == 0) return;

        using var textPaint = new SKPaint
        {
            Color = style.Color.ToSkColor(),
            IsAntialias = true
        };

        using var font = TextMeasurer.CreateFont(
            style.FontFamily, style.FontSize, style.FontWeight,
            style.FontStyle == FontStyle.Italic);

        // Offset all text drawing by padding
        int textSave = canvas.Save();
        canvas.Translate(padL, padT);

        // Draw each line
        foreach (var line in layout.Lines)
        {
            if (style.LetterSpacing != 0)
            {
                DrawTextWithLetterSpacing(canvas, line, style.LetterSpacing, font, textPaint);
            }
            else
            {
                canvas.DrawText(line.Text, line.X, line.Y, SKTextAlign.Left, font, textPaint);
            }
        }

        // Draw text decoration (underline, line-through)
        if (style.TextDecoration != TextDecoration.None)
        {
            DrawTextDecoration(canvas, layout, style, font, textPaint);
        }

        canvas.RestoreToCount(textSave);
    }

    private static void DrawInputText(SKCanvas canvas, InputElement input, ComputedStyle style, float width, float height)
    {
        bool hasValue = !string.IsNullOrEmpty(input.Value);
        bool hasPlaceholder = !string.IsNullOrEmpty(input.Placeholder);

        if (!hasValue && !hasPlaceholder) return;

        // Determine display text and color
        string displayText;
        SKColor textColor;

        if (hasValue)
        {
            // Mask password fields
            displayText = input.InputType.Equals("password", StringComparison.OrdinalIgnoreCase)
                ? new string('●', input.Value.Length)
                : input.Value;
            textColor = style.Color.ToSkColor();
        }
        else
        {
            displayText = input.Placeholder;
            // Placeholder in dimmed color
            var c = style.Color;
            textColor = new SKColor(c.R, c.G, c.B, 128);
        }

        // Account for border width — Yoga layout doesn't include border, so the
        // renderer draws borders inside the box. Offset text by border + padding.
        var bw = style.BorderWidth;
        float bL = bw.Left, bT = bw.Top, bR = bw.Right, bB = bw.Bottom;
        float insetL = bL + style.Padding.Left;
        float insetT = bT + style.Padding.Top;
        float insetR = bR + style.Padding.Right;
        float insetB = bB + style.Padding.Bottom;
        float contentW = width - insetL - insetR;
        float contentH = height - insetT - insetB;
        if (contentW <= 0) contentW = width;
        if (contentH <= 0) contentH = height;

        using var textPaint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true
        };

        float fontSize = style.FontSize > 0 ? style.FontSize : 14;
        using var font = TextMeasurer.CreateFont(
            style.FontFamily, fontSize, style.FontWeight,
            style.FontStyle == FontStyle.Italic);

        // Vertically center the text within the content area
        font.GetFontMetrics(out var metrics);
        float textY = insetT + (contentH - (metrics.Descent - metrics.Ascent)) / 2 - metrics.Ascent;

        int save = canvas.Save();
        canvas.ClipRect(new SKRect(insetL, insetT, insetL + contentW, insetT + contentH));
        canvas.DrawText(displayText, insetL, textY, SKTextAlign.Left, font, textPaint);

        // Draw caret and selection if focused
        if (input.IsFocused)
        {
            // Caret blink: visible for 530ms, hidden for 530ms
            long elapsed = Environment.TickCount64 - input.LastEditTick;
            bool caretVisible = (elapsed % 1060) < 530;

            string valueText = hasValue
                ? (input.InputType.Equals("password", StringComparison.OrdinalIgnoreCase)
                    ? new string('●', input.Value.Length)
                    : input.Value)
                : "";

            // Draw selection highlight
            if (input.HasSelection && hasValue)
            {
                int lo = Math.Min(input.SelectionStart, input.SelectionEnd);
                int hi = Math.Max(input.SelectionStart, input.SelectionEnd);
                lo = Math.Clamp(lo, 0, valueText.Length);
                hi = Math.Clamp(hi, 0, valueText.Length);

                float selStartX = lo > 0 ? insetL + font.MeasureText(valueText[..lo], textPaint) : insetL;
                float selEndX = hi > 0 ? insetL + font.MeasureText(valueText[..hi], textPaint) : insetL;

                using var selPaint = new SKPaint
                {
                    Color = new SKColor(66, 135, 245, 80),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(selStartX, insetT + 2, selEndX - selStartX, contentH - 4, selPaint);
            }

            // Draw caret
            if (caretVisible)
            {
                int cursorPos = Math.Clamp(input.CursorPosition, 0, valueText.Length);
                float caretX = cursorPos > 0
                    ? insetL + font.MeasureText(valueText[..cursorPos], textPaint)
                    : insetL;

                using var caretPaint = new SKPaint
                {
                    Color = style.Color.ToSkColor(),
                    StrokeWidth = 1.5f,
                    IsAntialias = true
                };
                canvas.DrawLine(caretX, insetT + 2, caretX, insetT + contentH - 2, caretPaint);
            }
        }

        canvas.RestoreToCount(save);
    }

    private static void DrawTextWithLetterSpacing(SKCanvas canvas, TextLine line, float spacing, SKFont font, SKPaint paint)
    {
        float x = line.X;
        foreach (char ch in line.Text)
        {
            string s = ch.ToString();
            canvas.DrawText(s, x, line.Y, SKTextAlign.Left, font, paint);
            x += font.MeasureText(s, paint) + spacing;
        }
    }

    private static void DrawTextDecoration(SKCanvas canvas, TextLayoutResult layout, ComputedStyle style, SKFont font, SKPaint textPaint)
    {
        font.GetFontMetrics(out var metrics);

        using var linePaint = new SKPaint
        {
            Color = textPaint.Color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1, style.FontSize / 12f),
            IsAntialias = true
        };

        foreach (var line in layout.Lines)
        {
            if (style.TextDecoration == TextDecoration.Underline)
            {
                float underlineY = line.Y + metrics.Descent * 0.5f;
                canvas.DrawLine(line.X, underlineY, line.X + line.Width, underlineY, linePaint);
            }
            else if (style.TextDecoration == TextDecoration.LineThrough)
            {
                float strikeY = line.Y + metrics.Ascent * 0.35f;
                canvas.DrawLine(line.X, strikeY, line.X + line.Width, strikeY, linePaint);
            }
        }
    }

    private static string ApplyTextTransform(string text, TextTransform transform) => transform switch
    {
        TextTransform.Uppercase => text.ToUpperInvariant(),
        TextTransform.Lowercase => text.ToLowerInvariant(),
        TextTransform.Capitalize => CapitalizeWords(text),
        _ => text
    };

    private static (SKPoint start, SKPoint end) ComputeLinearGradientPoints(float w, float h, float angleDeg)
    {
        // CSS: 0deg = to top, 90deg = to right, 180deg = to bottom
        float rad = (angleDeg - 90) * MathF.PI / 180f;
        float sin = MathF.Sin(rad);
        float cos = MathF.Cos(rad);

        float halfW = w / 2;
        float halfH = h / 2;
        float length = MathF.Abs(halfW * cos) + MathF.Abs(halfH * sin);

        float cx = halfW;
        float cy = halfH;

        return (
            new SKPoint(cx - cos * length, cy - sin * length),
            new SKPoint(cx + cos * length, cy + sin * length)
        );
    }

    private static string CapitalizeWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var chars = text.ToCharArray();
        bool capitalizeNext = true;
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]))
                capitalizeNext = true;
            else if (capitalizeNext)
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
                capitalizeNext = false;
            }
        }
        return new string(chars);
    }

    private void DrawImage(SKCanvas canvas, ImageElement imageElement, float width, float height)
    {
        var cached = ImageCache.Get(imageElement.Source!);
        if (cached == null) return;

        // Update natural dimensions on the element
        imageElement.NaturalWidth = cached.NaturalWidth;
        imageElement.NaturalHeight = cached.NaturalHeight;

        var destRect = new SKRect(0, 0, width, height);

        using var paint = new SKPaint
        {
            IsAntialias = true
        };

        canvas.DrawBitmap(cached.Bitmap, destRect, paint);
    }

    public void Dispose()
    {
        _canvas = null;
        _gpuSurface?.Dispose();
        _gpuSurface = null;
        _renderTarget?.Dispose();
        _renderTarget = null;
        _bitmap?.Dispose();
        _bitmap = null;
        _grContext?.Dispose();
        _grContext = null;
        _glInterface?.Dispose();
        _glInterface = null;
        ImageCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
