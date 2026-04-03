namespace Lumi.Rendering;

using SkiaSharp;
using Lumi.Core;

public static class ColorExtensions
{
    public static SKColor ToSkColor(this Color color) => new(color.R, color.G, color.B, color.A);
}
