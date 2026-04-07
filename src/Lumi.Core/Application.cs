namespace Lumi.Core;

/// <summary>
/// Main application class that owns the element tree and drives the update/paint loop.
/// </summary>
public class Application
{
    public Element Root { get; set; }
    public bool IsDirty => Root.IsDirty;
    public bool IsRunning { get; private set; }

    private Element? _hoveredElement;
    private Element? _focusedElement;

    public Application()
    {
        Root = new BoxElement("body");
    }

    /// <summary>
    /// Processes raw input events and routes them through the element tree.
    /// </summary>
    public void ProcessInput(List<InputEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case MouseEvent mouse:
                    HandleMouse(mouse);
                    break;
                case KeyboardEvent keyboard:
                    HandleKeyboard(keyboard);
                    break;
                case TextInputEvent textInput:
                    HandleTextInput(textInput);
                    break;
                case ScrollEvent scroll:
                    HandleScroll(scroll);
                    break;
                case WindowEvent window:
                    HandleWindow(window);
                    break;
            }
        }
    }

    /// <summary>
    /// Update step — run animations, bindings, etc. (stub for now).
    /// </summary>
    public void Update()
    {
        // Future: animation tick, binding updates
    }

    /// <summary>
    /// Mark the tree as clean after a paint pass.
    /// </summary>
    public void MarkClean()
    {
        MarkCleanAndSnapshot(Root);
    }

    public void RequestStop() => IsRunning = false;

    public void Start() => IsRunning = true;

    private void HandleMouse(MouseEvent mouse)
    {
        var target = HitTest(Root, mouse.X, mouse.Y);

        switch (mouse.Type)
        {
            case MouseEventType.Move:
                UpdateHover(target, mouse.X, mouse.Y);
                if (target != null)
                {
                    EventDispatcher.Dispatch(
                        new RoutedMouseEvent("MouseMove") { X = mouse.X, Y = mouse.Y },
                        target);
                }
                break;

            case MouseEventType.ButtonDown:
                if (target != null)
                {
                    SetFocus(target);
                    EventDispatcher.Dispatch(
                        new RoutedMouseEvent("MouseDown") { X = mouse.X, Y = mouse.Y, Button = mouse.Button },
                        target);
                }
                break;

            case MouseEventType.ButtonUp:
                if (target != null)
                {
                    EventDispatcher.Dispatch(
                        new RoutedMouseEvent("MouseUp") { X = mouse.X, Y = mouse.Y, Button = mouse.Button },
                        target);
                    EventDispatcher.Dispatch(
                        new RoutedMouseEvent("Click") { X = mouse.X, Y = mouse.Y, Button = mouse.Button },
                        target);
                }
                break;
        }
    }

    private void UpdateHover(Element? newHovered, float x, float y)
    {
        if (newHovered == _hoveredElement) return;

        if (_hoveredElement != null)
        {
            EventDispatcher.Dispatch(
                new RoutedMouseEvent("MouseLeave") { X = x, Y = y },
                _hoveredElement);
        }

        _hoveredElement = newHovered;

        if (_hoveredElement != null)
        {
            EventDispatcher.Dispatch(
                new RoutedMouseEvent("MouseEnter") { X = x, Y = y },
                _hoveredElement);
        }
    }

    private void SetFocus(Element element)
    {
        if (_focusedElement == element) return;

        if (_focusedElement != null)
            EventDispatcher.Dispatch(new RoutedEvent("Blur"), _focusedElement);

        _focusedElement = element;
        EventDispatcher.Dispatch(new RoutedEvent("Focus"), element);
    }

    private void HandleKeyboard(KeyboardEvent keyboard)
    {
        var target = _focusedElement ?? Root;
        var eventName = keyboard.Type == KeyboardEventType.KeyDown ? "KeyDown" : "KeyUp";
        EventDispatcher.Dispatch(
            new RoutedKeyEvent(eventName)
            {
                Key = keyboard.Key,
                Shift = keyboard.Shift,
                Ctrl = keyboard.Ctrl,
                Alt = keyboard.Alt
            },
            target);
    }

    private void HandleTextInput(TextInputEvent textInput)
    {
        // Future: route to focused input element
    }

    private void HandleScroll(ScrollEvent scroll)
    {
        var target = HitTest(Root, scroll.X, scroll.Y);
        if (target != null)
        {
            EventDispatcher.Dispatch(new RoutedEvent("Scroll"), target);
        }
    }

    private void HandleWindow(WindowEvent window)
    {
        switch (window.Type)
        {
            case WindowEventType.Close:
                RequestStop();
                break;
            case WindowEventType.Resized:
                // Resize is handled by LumiApp.UpdateInteractionState to avoid double MarkDirty
                break;
        }
    }

    /// <summary>
    /// Hit test: walk tree in reverse order (topmost first), return deepest hit.
    /// Adjusts for scroll offset and clips to overflow:scroll/hidden bounds.
    /// </summary>
    public static Element? HitTest(Element root, float x, float y)
    {
        if (root.ComputedStyle.Display == DisplayMode.None) return null;
        if (root.ComputedStyle.Visibility == Visibility.Hidden) return null;

        bool isClipping = root.ComputedStyle.Overflow == Overflow.Scroll ||
                          root.ComputedStyle.Overflow == Overflow.Hidden;

        // For scroll/hidden containers, only test children if point is within bounds
        bool testChildren = !isClipping || root.LayoutBox.Contains(x, y);

        if (testChildren)
        {
            // Adjust coordinates for scroll offset
            float childX = x;
            float childY = y;
            if (root.ComputedStyle.Overflow == Overflow.Scroll)
            {
                childX += root.ScrollLeft;
                childY += root.ScrollTop;
            }

            for (int i = root.Children.Count - 1; i >= 0; i--)
            {
                var hit = HitTest(root.Children[i], childX, childY);
                if (hit != null) return hit;
            }
        }

        if (!root.ComputedStyle.PointerEvents) return null;

        if (root.LayoutBox.Contains(x, y))
            return root;

        return null;
    }

    /// <summary>
    /// Mark all elements clean and snapshot their layout boxes in a single tree walk.
    /// </summary>
    private static void MarkCleanAndSnapshot(Element element)
    {
        element.IsDirty = false;
        element.IsLayoutDirty = false;

        // Snapshot current absolute box for next frame's dirty region detection
        element.PreviousLayoutBox = ComputeAbsoluteBox(element);

        foreach (var child in element.Children)
            MarkCleanAndSnapshot(child);
    }

    private static LayoutBox ComputeAbsoluteBox(Element element)
    {
        float x = 0, y = 0;
        var current = element;
        while (current != null)
        {
            x += current.LayoutBox.X;
            y += current.LayoutBox.Y;
            current = current.Parent;
        }
        return new LayoutBox(x, y, element.LayoutBox.Width, element.LayoutBox.Height);
    }
}
