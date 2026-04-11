namespace Lumi.Core;

using Lumi.Core.DragDrop;

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

    /// <summary>
    /// The element that currently has focus, or null if nothing is focused.
    /// </summary>
    public Element? FocusedElement => _focusedElement;

    // Drag-and-drop state
    private readonly DragDropState _dragState = new();
    private Element? _potentialDragSource;
    private Element? _lastDragOverElement;
    private float _dragStartX;
    private float _dragStartY;
    private const float DragThreshold = 5f;

    public DragDropState DragState => _dragState;

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
                case FileDropEvent fileDrop:
                    HandleFileDrop(fileDrop);
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
                // Check if we should start a drag
                if (_potentialDragSource != null && !_dragState.IsDragging)
                {
                    float dx = mouse.X - _dragStartX;
                    float dy = mouse.Y - _dragStartY;
                    if (dx * dx + dy * dy >= DragThreshold * DragThreshold)
                    {
                        _dragState.IsDragging = true;
                        _dragState.Source = _potentialDragSource;
                        _dragState.Data = new DragData();
                        _dragState.X = mouse.X;
                        _dragState.Y = mouse.Y;
                        _potentialDragSource.RaiseDragStart(_dragState.Data);
                    }
                }

                if (_dragState.IsDragging)
                {
                    _dragState.X = mouse.X;
                    _dragState.Y = mouse.Y;
                    if (target != _lastDragOverElement)
                    {
                        _lastDragOverElement?.RaiseDragLeave(_dragState.Data!);
                        target?.RaiseDragEnter(_dragState.Data!);
                        _lastDragOverElement = target;
                    }
                    target?.RaiseDragOver(_dragState.Data!);
                }
                else
                {
                    UpdateHover(target, mouse.X, mouse.Y);
                    if (target != null)
                    {
                        EventDispatcher.Dispatch(
                            new RoutedMouseEvent("MouseMove") { X = mouse.X, Y = mouse.Y },
                            target);
                    }
                }
                break;

            case MouseEventType.ButtonDown:
                if (target != null)
                {
                    // Record potential drag source
                    if (target.IsDraggable && mouse.Button == MouseButton.Left)
                    {
                        _potentialDragSource = target;
                        _dragStartX = mouse.X;
                        _dragStartY = mouse.Y;
                    }

                    SetFocus(target);
                    EventDispatcher.Dispatch(
                        new RoutedMouseEvent("MouseDown") { X = mouse.X, Y = mouse.Y, Button = mouse.Button },
                        target);
                }
                break;

            case MouseEventType.ButtonUp:
                if (_dragState.IsDragging)
                {
                    if (target != null)
                        target.RaiseDrop(_dragState.Data!);
                    _dragState.Source?.RaiseDragEnd();

                    // Reset drag state
                    _dragState.IsDragging = false;
                    _dragState.Source = null;
                    _dragState.Data = null;
                    _potentialDragSource = null;
                    _lastDragOverElement = null;
                }
                else
                {
                    _potentialDragSource = null;
                    if (target != null)
                    {
                        EventDispatcher.Dispatch(
                            new RoutedMouseEvent("MouseUp") { X = mouse.X, Y = mouse.Y, Button = mouse.Button },
                            target);
                        EventDispatcher.Dispatch(
                            new RoutedMouseEvent("Click") { X = mouse.X, Y = mouse.Y, Button = mouse.Button },
                            target);
                    }
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

    /// <summary>
    /// Move focus to the given element (or its nearest focusable ancestor).
    /// Pass null to clear focus.
    /// </summary>
    public void SetFocus(Element? element)
    {
        // Walk up to the nearest focusable element
        var target = element;
        while (target != null && !target.IsFocusable)
            target = target.Parent;

        if (_focusedElement == target) return;

        if (_focusedElement != null)
        {
            _focusedElement.IsFocused = false;
            EventDispatcher.Dispatch(new RoutedEvent("Blur"), _focusedElement);
        }

        _focusedElement = target;

        if (target != null)
        {
            target.IsFocused = true;
            EventDispatcher.Dispatch(new RoutedEvent("Focus"), target);
        }
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
                    if (HandleInputKeyDown(input, keyboard))
                        return;
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

    private static bool HandleInputKeyDown(InputElement input, KeyboardEvent keyboard)
    {
        switch (keyboard.Key)
        {
            case KeyCode.Left:
                if (keyboard.Shift)
                {
                    if (!input.HasSelection)
                        input.SelectionStart = input.CursorPosition;
                    int moveBack = 1;
                    if (input.CursorPosition >= 2 && char.IsSurrogatePair(input.Value[input.CursorPosition - 2], input.Value[input.CursorPosition - 1]))
                        moveBack = 2;
                    input.CursorPosition = Math.Max(0, input.CursorPosition - moveBack);
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
                        int moveBack = 1;
                        if (input.CursorPosition >= 2 && char.IsSurrogatePair(input.Value[input.CursorPosition - 2], input.Value[input.CursorPosition - 1]))
                            moveBack = 2;
                        input.CursorPosition = Math.Max(0, input.CursorPosition - moveBack);
                        input.ClearSelection();
                    }
                }
                input.ResetBlink();
                input.MarkDirty();
                return true;

            case KeyCode.Right:
                if (keyboard.Shift)
                {
                    if (!input.HasSelection)
                        input.SelectionStart = input.CursorPosition;
                    int moveForward = 1;
                    if (input.CursorPosition < input.Value.Length - 1 && char.IsSurrogatePair(input.Value[input.CursorPosition], input.Value[input.CursorPosition + 1]))
                        moveForward = 2;
                    input.CursorPosition = Math.Min(input.Value.Length, input.CursorPosition + moveForward);
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
                        int moveForward = 1;
                        if (input.CursorPosition < input.Value.Length - 1 && char.IsSurrogatePair(input.Value[input.CursorPosition], input.Value[input.CursorPosition + 1]))
                            moveForward = 2;
                        input.CursorPosition = Math.Min(input.Value.Length, input.CursorPosition + moveForward);
                        input.ClearSelection();
                    }
                }
                input.ResetBlink();
                input.MarkDirty();
                return true;

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
                return true;

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
                return true;

            case KeyCode.A when keyboard.Ctrl:
                input.SelectionStart = 0;
                input.SelectionEnd = input.Value.Length;
                input.CursorPosition = input.Value.Length;
                input.ResetBlink();
                input.MarkDirty();
                return true;

            case KeyCode.C when keyboard.Ctrl:
            {
                if (!input.HasSelection)
                    return true;
                var textToCopy = input.Value[Math.Min(input.SelectionStart, input.SelectionEnd)..Math.Max(input.SelectionStart, input.SelectionEnd)];
                if (!string.IsNullOrEmpty(textToCopy))
                    Clipboard.SetText(textToCopy);
                return true;
            }

            case KeyCode.V when keyboard.Ctrl:
            {
                var pasteText = Clipboard.GetText();
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
                return true;
            }

            case KeyCode.X when keyboard.Ctrl:
            {
                if (!input.HasSelection)
                    return true;
                var textToCut = input.Value[Math.Min(input.SelectionStart, input.SelectionEnd)..Math.Max(input.SelectionStart, input.SelectionEnd)];
                if (!string.IsNullOrEmpty(textToCut))
                    Clipboard.SetText(textToCut);
                input.DeleteSelection();
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                return true;
            }

            case KeyCode.Backspace:
                if (input.HasSelection)
                {
                    input.DeleteSelection();
                }
                else if (input.CursorPosition > 0)
                {
                    int deleteCount = 1;
                    if (input.CursorPosition >= 2 && char.IsSurrogatePair(input.Value[input.CursorPosition - 2], input.Value[input.CursorPosition - 1]))
                        deleteCount = 2;
                    input.Value = input.Value[..(input.CursorPosition - deleteCount)] + input.Value[input.CursorPosition..];
                    input.CursorPosition -= deleteCount;
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                return true;

            case KeyCode.Delete:
                if (input.HasSelection)
                {
                    input.DeleteSelection();
                }
                else if (input.CursorPosition < input.Value.Length)
                {
                    int deleteCount = 1;
                    if (input.CursorPosition < input.Value.Length - 1 && char.IsSurrogatePair(input.Value[input.CursorPosition], input.Value[input.CursorPosition + 1]))
                        deleteCount = 2;
                    input.Value = input.Value[..input.CursorPosition] + input.Value[(input.CursorPosition + deleteCount)..];
                    input.ClearSelection();
                }
                input.ResetBlink();
                input.MarkDirty();
                EventDispatcher.Dispatch(new RoutedEvent("input"), input);
                return true;

            default:
                return false;
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

    private void HandleFileDrop(FileDropEvent fileDrop)
    {
        var target = HitTest(Root, fileDrop.X, fileDrop.Y);
        if (target != null)
        {
            var data = new DragData { Files = fileDrop.Files };
            target.RaiseDrop(data);
        }
    }

    private void HandleWindow(WindowEvent window)
    {
        switch (window.Type)
        {
            case WindowEventType.Close:
                if (_dragState.IsDragging)
                {
                    _dragState.Source?.RaiseDragEnd();
                    _dragState.IsDragging = false;
                    _dragState.Source = null;
                    _dragState.Data = null;
                    _potentialDragSource = null;
                    _lastDragOverElement = null;
                }
                RequestStop();
                break;
            case WindowEventType.Resized:
                // Resize is handled by LumiApp.UpdateInteractionState to avoid double MarkDirty
                break;
        }
    }

    /// <summary>
    /// Hit test: walk tree in reverse order (topmost first), return deepest hit.
    /// Adjusts for CSS transforms, scroll offset, and clips to overflow:scroll/hidden bounds.
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

            float dx = localX - originX - transform.TranslateX;
            float dy = localY - originY - transform.TranslateY;

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

            if (transform.ScaleX != 0 && transform.ScaleY != 0)
            {
                dx /= transform.ScaleX;
                dy /= transform.ScaleY;
            }

            localX = dx + originX;
            localY = dy + originY;
        }

        bool isClipping = root.ComputedStyle.Overflow == Overflow.Scroll ||
                          root.ComputedStyle.Overflow == Overflow.Hidden;

        bool testChildren = !isClipping || root.LayoutBox.Contains(localX, localY);

        if (testChildren)
        {
            float childX = localX;
            float childY = localY;
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

        return root.LayoutBox.Contains(localX, localY) ? root : null;
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
