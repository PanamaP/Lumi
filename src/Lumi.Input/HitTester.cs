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

        bool isClipping = root.ComputedStyle.Overflow == Overflow.Scroll ||
                          root.ComputedStyle.Overflow == Overflow.Hidden;

        // For scroll/hidden containers, only test children if point is within bounds (clipping)
        bool testChildren = !isClipping || root.LayoutBox.Contains(x, y);

        if (testChildren)
        {
            // Adjust coordinates for scroll offset when testing children of scroll containers
            float childX = x;
            float childY = y;
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

        return root.LayoutBox.Contains(x, y) ? root : null;
    }
}
