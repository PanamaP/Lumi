using Lumi.Core;

namespace Lumi.Input;

/// <summary>
/// Hit testing utility for finding elements at a screen coordinate.
/// </summary>
public static class HitTester
{
    /// <summary>
    /// Find the deepest element at the given point, respecting visibility, display, pointer-events,
    /// and scroll offset. Elements are tested in reverse paint order (topmost first).
    /// </summary>
    public static Element? HitTest(Element root, float x, float y)
    {
        if (root.ComputedStyle.Display == DisplayMode.None) return null;
        if (root.ComputedStyle.Visibility == Visibility.Hidden) return null;

        // Apply inverse transform to convert hit point into element's local space
        float localX = x;
        float localY = y;
        var transform = root.ComputedStyle.Transform;
        if (!transform.IsIdentity)
        {
            var box = root.LayoutBox;
            float originX = root.ComputedStyle.TransformOriginX / 100f * box.Width + box.X;
            float originY = root.ComputedStyle.TransformOriginY / 100f * box.Height + box.Y;

            // Reverse translate
            float dx = localX - originX - transform.TranslateX;
            float dy = localY - originY - transform.TranslateY;

            // Reverse rotate
            if (transform.Rotate != 0)
            {
                float rad = -transform.Rotate * MathF.PI / 180f;
                float cos = MathF.Cos(rad);
                float sin = MathF.Sin(rad);
                float rx = dx * cos - dy * sin;
                float ry = dx * sin + dy * cos;
                dx = rx;
                dy = ry;
            }

            // Reverse scale — degenerate scale means the element is invisible
            if (transform.ScaleX == 0 || transform.ScaleY == 0)
                return null;
            dx /= transform.ScaleX;
            dy /= transform.ScaleY;

            // Reverse skew
            if (transform.SkewX != 0 || transform.SkewY != 0)
            {
                float tanX = MathF.Tan(transform.SkewX * MathF.PI / 180f);
                float tanY = MathF.Tan(transform.SkewY * MathF.PI / 180f);
                float det = 1f - tanX * tanY;
                if (MathF.Abs(det) > 1e-6f)
                {
                    float ux = (dx - tanX * dy) / det;
                    float uy = (dy - tanY * dx) / det;
                    dx = ux;
                    dy = uy;
                }
            }

            localX = dx + originX;
            localY = dy + originY;
        }

        bool isClipping = root.ComputedStyle.Overflow == Overflow.Scroll ||
                          root.ComputedStyle.Overflow == Overflow.Hidden;

        // For scroll/hidden containers, only test children if point is within bounds (clipping)
        bool testChildren = !isClipping || root.LayoutBox.Contains(localX, localY);

        if (testChildren)
        {
            // Adjust coordinates for scroll offset when testing children of scroll containers
            float childX = localX;
            float childY = localY;
            if (root.ComputedStyle.Overflow == Overflow.Scroll)
            {
                childX += root.ScrollLeft;
                childY += root.ScrollTop;
            }

            // Test children in reverse order (last painted = topmost)
            for (int i = root.Children.Count - 1; i >= 0; i--)
            {
                var hit = HitTest(root.Children[i], childX, childY);
                if (hit != null) return hit;
            }
        }

        if (!root.ComputedStyle.PointerEvents) return null;

        return root.LayoutBox.Contains(localX, localY) ? root : null;
    }
}
