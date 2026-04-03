namespace Lumi.Core;

/// <summary>
/// Routed event phases for the element tree event system.
/// </summary>
public enum RoutingPhase
{
    Tunnel,
    Direct,
    Bubble
}

/// <summary>
/// Base class for routed events that traverse the element tree.
/// </summary>
public class RoutedEvent
{
    public string Name { get; }
    public Element? Target { get; internal set; }
    public Element? Source { get; internal set; }
    public bool Handled { get; set; }
    public RoutingPhase Phase { get; internal set; }

    public RoutedEvent(string name) => Name = name;
}

/// <summary>
/// Delegate for routed event handlers.
/// </summary>
public delegate void RoutedEventHandler(Element sender, RoutedEvent e);

/// <summary>
/// Routed mouse event with position and button info.
/// </summary>
public class RoutedMouseEvent : RoutedEvent
{
    public float X { get; init; }
    public float Y { get; init; }
    public MouseButton Button { get; init; }

    public RoutedMouseEvent(string name) : base(name) { }
}

/// <summary>
/// Routed keyboard event.
/// </summary>
public class RoutedKeyEvent : RoutedEvent
{
    public KeyCode Key { get; init; }
    public bool Shift { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }

    public RoutedKeyEvent(string name) : base(name) { }
}
