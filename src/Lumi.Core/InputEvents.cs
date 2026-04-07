namespace Lumi.Core;

/// <summary>
/// Normalized input event from the platform layer.
/// </summary>
public abstract class InputEvent
{
    public bool Handled { get; set; }
    public double Timestamp { get; set; }
}

public class MouseEvent : InputEvent
{
    public float X { get; init; }
    public float Y { get; init; }
    public MouseButton Button { get; init; }
    public MouseEventType Type { get; init; }
}

public class KeyboardEvent : InputEvent
{
    public KeyCode Key { get; init; }
    public KeyboardEventType Type { get; init; }
    public bool Shift { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
}

public class TextInputEvent : InputEvent
{
    public string Text { get; init; } = "";
}

public class ScrollEvent : InputEvent
{
    public float X { get; init; }
    public float Y { get; init; }
    public float DeltaX { get; init; }
    public float DeltaY { get; init; }
}

public class WindowEvent : InputEvent
{
    public WindowEventType Type { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public enum MouseButton { None, Left, Middle, Right }

public enum MouseEventType
{
    Move,
    ButtonDown,
    ButtonUp
}

public enum KeyboardEventType
{
    KeyDown,
    KeyUp
}

public enum WindowEventType
{
    Resized,
    Close,
    FocusGained,
    FocusLost,
    Shown,
    Hidden,
    Exposed
}

public enum KeyCode
{
    Unknown,
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Escape, Tab, CapsLock, Space, Enter, Backspace, Delete,
    Up, Down, Left, Right,
    Home, End, PageUp, PageDown,
    Insert, PrintScreen, Pause,
    LeftShift, RightShift, LeftCtrl, RightCtrl, LeftAlt, RightAlt,
    LeftSuper, RightSuper, Menu
}
