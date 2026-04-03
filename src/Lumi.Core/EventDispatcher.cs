namespace Lumi.Core;

/// <summary>
/// Dispatches routed events through the element tree using tunnel/direct/bubble phases.
/// </summary>
public static class EventDispatcher
{
    /// <summary>
    /// Dispatches a routed event through the element tree.
    /// Phase 1 (Tunnel): Root → Target (preview)
    /// Phase 2 (Direct): Target only
    /// Phase 3 (Bubble): Target → Root
    /// </summary>
    public static void Dispatch(RoutedEvent e, Element target)
    {
        e.Target = target;
        e.Source = target;

        // Build path from root to target
        var path = BuildPath(target);

        // Phase 1: Tunnel (root → target, excluding target)
        e.Phase = RoutingPhase.Tunnel;
        for (int i = 0; i < path.Count - 1; i++)
        {
            path[i].RaiseEvent(e);
            if (e.Handled) return;
        }

        // Phase 2: Direct (target only)
        e.Phase = RoutingPhase.Direct;
        target.RaiseEvent(e);
        if (e.Handled) return;

        // Phase 3: Bubble (target → root, excluding target)
        e.Phase = RoutingPhase.Bubble;
        for (int i = path.Count - 2; i >= 0; i--)
        {
            path[i].RaiseEvent(e);
            if (e.Handled) return;
        }
    }

    private static List<Element> BuildPath(Element element)
    {
        var path = new List<Element>();
        var current = element;
        while (current != null)
        {
            path.Add(current);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }
}
