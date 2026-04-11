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
        {
            _focusedElement.IsFocused = false;
            EventDispatcher.Dispatch(new RoutedEvent("Blur"), _focusedElement);
        }

        _focusedElement = element;
        element.IsFocused = true;
        EventDispatcher.Dispatch(new RoutedEvent("Focus"), element);
    }

    private void HandleKeyboard(KeyboardEvent keyboard)
    {
        var target = _focusedElement ?? Root;

        // Handle keyboard for focused InputElement
        if (keyboard.Type == KeyboardEventType.KeyDown)
        {
            var inputTarget = _focusedElement;
            while (inputTarget != null)
            {
                if (inputTarget is InputElement input && !input.IsDisabled)
                {
                    HandleInputKeyDown(input, keyboard);
                    break;
                }
                inputTarget = inputTarget.Parent;
            }
        }

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

    private static void HandleInputKeyDown(InputElement input, KeyboardEvent keyboard)
    {
        switch (keyboard.Key)
        {
            case KeyCode.Left:
                if (keyboard.Shift)
                {
                    if (!input.HasSelection)
                        input.SelectionStart = input.CursorPosition;
                    input.CursorPosition = Math.Max(0, input.CursorPosition - 1);
                    input.SelectionEnd = input.CursorPosition;
                }
                else
                {
                    if (input.HasSelection)
                    {
                        input.CursorPosition = Math.Min(input.SelectionStart, input.SelectionEnd);
                        input.ClearSelection();
                    }
                    else
                    {
                        input.CursorPosition = Math.Max(0, input.CursorPosition - 1);
                        input.ClearSelection();
                    }
                }
                input.ResetBlink();
                input.MarkDirty();
                break;

            case KeyCode.Right:
                if (keyboard.Shift)
                {
                    if (!input.HasSelection)
                        input.SelectionStart = input.CursorPosition;
                    input.CursorPosition = Math.Min(input.Value.Length, input.CursorPosition + 1);
                    input.SelectionEnd = input.CursorPosition;
                }
                else
                {
                    if (input.HasSelection)
                    {
                        input.CursorPosition = Math.Max(input.SelectionStart, input.SelectionEnd);
                        input.ClearSelection();
                    }
                    else
                    {
                        input.CursorPosition = Math.Min(input.Value.Length, input.CursorPosition + 1);
                        input.ClearSelection();
                    }
                }
                input.ResetBlink();
                input.MarkDirty();
                break;

            case KeyCode.Home:
                if (keyboard.Shift)
                {
                    if (!input.HasSelection)
                        input.SelectionStart = input.CursorPosition;
                    input.CursorPosition = 0;
                    input.SelectionEnd = input.CursorPosition;
                }
                else
                {
                    input.CursorPosition = 0;
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                break;

            case KeyCode.End:
                if (keyboard.Shift)
                {
                    if (!input.HasSelection)
                        input.SelectionStart = input.CursorPosition;
                    input.CursorPosition = input.Value.Length;
                    input.SelectionEnd = input.CursorPosition;
                }
                else
                {
                    input.CursorPosition = input.Value.Length;
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                break;

            case KeyCode.A when keyboard.Ctrl:
                input.SelectionStart = 0;
                input.SelectionEnd = input.Value.Length;
                input.CursorPosition = input.Value.Length;
                input.ResetBlink();
                input.MarkDirty();
                break;

            case KeyCode.C when keyboard.Ctrl:
            {
                var textToCopy = input.HasSelection
                    ? input.Value[Math.Min(input.SelectionStart, input.SelectionEnd)..Math.Max(input.SelectionStart, input.SelectionEnd)]
                    : input.Value;
                if (!string.IsNullOrEmpty(textToCopy))
                    Clipboard.SetText?.Invoke(textToCopy);
                break;
            }

            case KeyCode.V when keyboard.Ctrl:
            {
                var pasteText = Clipboard.GetText?.Invoke();
                if (!string.IsNullOrEmpty(pasteText))
                {
                    if (input.HasSelection)
                        input.DeleteSelection();

                    input.Value = input.Value[..input.CursorPosition] + pasteText + input.Value[input.CursorPosition..];
                    input.CursorPosition += pasteText.Length;
                    input.ClearSelection();
                    input.ResetBlink();
                    input.MarkDirty();
                    EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                }
                break;
            }

            case KeyCode.X when keyboard.Ctrl:
            {
                var textToCut = input.HasSelection
                    ? input.Value[Math.Min(input.SelectionStart, input.SelectionEnd)..Math.Max(input.SelectionStart, input.SelectionEnd)]
                    : input.Value;
                if (!string.IsNullOrEmpty(textToCut))
                    Clipboard.SetText?.Invoke(textToCut);

                if (input.HasSelection)
                    input.DeleteSelection();
                else
                {
                    input.Value = "";
                    input.CursorPosition = 0;
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                break;
            }

            case KeyCode.Backspace:
                if (input.HasSelection)
                {
                    input.DeleteSelection();
                }
                else if (input.CursorPosition > 0)
                {
                    input.Value = input.Value[..(input.CursorPosition - 1)] + input.Value[input.CursorPosition..];
                    input.CursorPosition--;
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                break;

            case KeyCode.Delete:
                if (input.HasSelection)
                {
                    input.DeleteSelection();
                }
                else if (input.CursorPosition < input.Value.Length)
                {
                    input.Value = input.Value[..input.CursorPosition] + input.Value[(input.CursorPosition + 1)..];
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                break;
        }
    }

    private void HandleTextInput(TextInputEvent textInput)
    {
        // Route text input to the focused InputElement
        var target = _focusedElement;
        while (target != null)
        {
            if (target is InputElement input && !input.IsDisabled)
            {
                if (input.HasSelection)
                    input.DeleteSelection();

                input.Value = input.Value[..input.CursorPosition] + textInput.Text + input.Value[input.CursorPosition..];
                input.CursorPosition += textInput.Text.Length;
                input.ClearSelection();
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                return;
            }
            target = target.Parent;
        }
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
