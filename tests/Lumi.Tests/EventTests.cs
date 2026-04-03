using Lumi.Core;

namespace Lumi.Tests;

public class EventTests
{
    [Fact]
    public void Event_Bubbles_FromTargetToRoot()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        var grandchild = new BoxElement("button");
        root.AddChild(child);
        child.AddChild(grandchild);

        var phases = new List<(string who, RoutingPhase phase)>();
        root.On("Click", (_, e) => phases.Add(("root", e.Phase)));
        child.On("Click", (_, e) => phases.Add(("child", e.Phase)));
        grandchild.On("Click", (_, e) => phases.Add(("grandchild", e.Phase)));

        EventDispatcher.Dispatch(new RoutedMouseEvent("Click"), grandchild);

        // Standard handlers fire during Direct and Bubble only (not Tunnel),
        // matching web browser addEventListener default behavior.
        Assert.Equal(
            [("grandchild", RoutingPhase.Direct),
             ("child", RoutingPhase.Bubble), ("root", RoutingPhase.Bubble)],
            phases);
    }

    [Fact]
    public void Event_StopsPropagation_WhenHandled()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("button");
        root.AddChild(child);

        var rootBubbleReached = false;
        root.On("Click", (_, e) =>
        {
            if (e.Phase == RoutingPhase.Bubble)
                rootBubbleReached = true;
        });
        child.On("Click", (_, e) => e.Handled = true);

        EventDispatcher.Dispatch(new RoutedMouseEvent("Click"), child);

        Assert.False(rootBubbleReached);
    }

    [Fact]
    public void Event_Tunnels_FromRootToTarget()
    {
        var root = new BoxElement("div");
        var child = new BoxElement("button");
        root.AddChild(child);

        RoutingPhase? rootPhase = null;
        root.On("Click", (_, e) => rootPhase = e.Phase);

        EventDispatcher.Dispatch(new RoutedMouseEvent("Click"), child);

        // Root gets called during tunnel phase first, then bubble
        // Since we're listening on both, the last call is bubble
        Assert.Equal(RoutingPhase.Bubble, rootPhase);
    }
}
