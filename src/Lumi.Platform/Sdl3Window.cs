using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lumi.Core;
using SDL;
using static SDL.SDL3;

namespace Lumi.Platform;

public unsafe class Sdl3Window : IPlatformWindow
{
    private SDL_Window* _window;
    private SDL_Renderer* _renderer;
    private SDL_GLContextState* _glContext;
    private bool _isOpen;
    private bool _disposed;
    private int _displayRefreshRate;

    // Live resize: SDL event watcher fires during the OS modal resize loop
    private static Sdl3Window? _activeInstance;
    private Action<int, int>? _liveResizeCallback;

    public bool IsOpen => _isOpen;

    public IntPtr NativeHandle => (IntPtr)_window;

    public bool HasGLContext => _glContext != null;

    public SystemPreferences SystemPreferences { get; } = new();

    /// <summary>
    /// The SDL renderer associated with this window, for use by <see cref="Sdl3RenderTarget"/>.
    /// </summary>
    public SDL_Renderer* Renderer => _renderer;

    /// <summary>
    /// The SDL renderer pointer as IntPtr, for safe interop.
    /// </summary>
    public IntPtr RendererPtr => (IntPtr)_renderer;

    public int DisplayRefreshRate
    {
        get
        {
            if (_window == null) return 60;
            var displayId = SDL_GetDisplayForWindow(_window);
            var mode = SDL_GetCurrentDisplayMode(displayId);
            if (mode != null && mode->refresh_rate > 0)
                return (int)mode->refresh_rate;
            return _displayRefreshRate > 0 ? _displayRefreshRate : 60;
        }
    }

    public void SetVSync(VSyncMode mode)
    {
        EnsureWindow();
        if (_glContext != null)
        {
            // OpenGL path: use swap interval
            SDL_GL_SetSwapInterval((int)mode);
        }
        else if (_renderer != null)
        {
            // SDL renderer path
            SDL_SetRenderVSync(_renderer, (int)mode);
        }
    }

    public void Create(string title, int width, int height)
    {
        if (_window != null)
            throw new InvalidOperationException("Window has already been created.");

        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
            throw new InvalidOperationException($"SDL_Init failed: {SDL_GetError()}");

        _window = SDL_CreateWindow(title, width, height,
            SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL_WindowFlags.SDL_WINDOW_OPENGL);

        if (_window == null)
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL_GetError()}");

        // Query display refresh rate
        var displayId = SDL_GetDisplayForWindow(_window);
        var displayMode = SDL_GetCurrentDisplayMode(displayId);
        _displayRefreshRate = displayMode != null && displayMode->refresh_rate > 0
            ? (int)displayMode->refresh_rate
            : 60;

        // SDL3 requires explicit opt-in for text input events (unlike SDL2 which enabled them by default).
        SDL_StartTextInput(_window);

        _isOpen = true;
    }

    public void CreateGLContext()
    {
        EnsureWindow();
        if (_glContext != null)
            throw new InvalidOperationException("GL context has already been created.");

        // Request OpenGL 3.3 Core profile with stencil buffer (required by Skia)
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_STENCIL_SIZE, 8);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DEPTH_SIZE, 0);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DOUBLEBUFFER, 1);

        _glContext = SDL_GL_CreateContext(_window);
        if (_glContext == null)
            throw new InvalidOperationException($"SDL_GL_CreateContext failed: {SDL_GetError()}");

        if (!SDL_GL_MakeCurrent(_window, _glContext))
            throw new InvalidOperationException($"SDL_GL_MakeCurrent failed: {SDL_GetError()}");

        // Enable adaptive VSync: presents immediately on missed deadlines,
        // waits for VSync otherwise. Falls back to regular VSync if unsupported.
        if (!SDL_GL_SetSwapInterval(-1))
            SDL_GL_SetSwapInterval(1);
    }

    public void SwapBuffers()
    {
        EnsureWindow();
        if (_glContext == null)
            throw new InvalidOperationException("No GL context. Call CreateGLContext() first.");
        SDL_GL_SwapWindow(_window);
    }

    public void Show()
    {
        EnsureWindow();
        SDL_ShowWindow(_window);
    }

    public void Hide()
    {
        EnsureWindow();
        SDL_HideWindow(_window);
    }

    public void SetTitle(string title)
    {
        EnsureWindow();
        SDL_SetWindowTitle(_window, title);
    }

    public void Resize(int width, int height)
    {
        EnsureWindow();
        SDL_SetWindowSize(_window, width, height);
    }

    /// <summary>
    /// Register a callback for live resize rendering.
    /// On Windows, the OS enters a modal loop during window resize, blocking the main
    /// event loop. This callback fires from an SDL event watcher during that modal loop,
    /// allowing the application to re-render at each new size.
    /// </summary>
    public void SetLiveResizeCallback(Action<int, int> callback)
    {
        _liveResizeCallback = callback;
        _activeInstance = this;
        SDL_AddEventWatch(&LiveResizeEventWatcher, IntPtr.Zero);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static SDLBool LiveResizeEventWatcher(IntPtr userdata, SDL_Event* ev)
    {
        var type = (SDL_EventType)ev->type;
        if (type is SDL_EventType.SDL_EVENT_WINDOW_RESIZED
                  or SDL_EventType.SDL_EVENT_WINDOW_EXPOSED
                  or SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED
                  or SDL_EventType.SDL_EVENT_WINDOW_RESTORED)
        {
            _activeInstance?._liveResizeCallback?.Invoke(ev->window.data1, ev->window.data2);
        }
        return true;
    }

    public float GetDpiScale()
    {
        EnsureWindow();
        return SDL_GetWindowDisplayScale(_window);
    }

    public (int Width, int Height) GetSize()
    {
        EnsureWindow();
        int w, h;
        SDL_GetWindowSize(_window, &w, &h);
        return (w, h);
    }

    public (int Width, int Height) GetPixelSize()
    {
        EnsureWindow();
        int w, h;
        SDL_GetWindowSizeInPixels(_window, &w, &h);
        return (w, h);
    }

    public List<InputEvent> PollEvents()
    {
        EnsureWindow();
        var events = new List<InputEvent>();
        SDL_Event ev;

        while (SDL_PollEvent(&ev))
        {
            var inputEvent = TranslateEvent(&ev);
            if (inputEvent != null)
                events.Add(inputEvent);
        }

        return events;
    }

    /// <summary>
    /// Block until at least one event arrives, then drain all pending events.
    /// Used when idle (nothing dirty) to avoid burning CPU.
    /// </summary>
    public List<InputEvent> WaitForEvents()
    {
        EnsureWindow();
        var events = new List<InputEvent>();
        SDL_Event ev;

        // Block until the first event arrives
        if (SDL_WaitEvent(&ev))
        {
            var inputEvent = TranslateEvent(&ev);
            if (inputEvent != null)
                events.Add(inputEvent);
        }

        // Drain any remaining pending events
        while (SDL_PollEvent(&ev))
        {
            var inputEvent = TranslateEvent(&ev);
            if (inputEvent != null)
                events.Add(inputEvent);
        }

        return events;
    }

    /// <summary>
    /// Push a user event to wake up WaitForEvents from another thread.
    /// Used by hot reload to signal pending file changes without polling.
    /// </summary>
    public void WakeUp()
    {
        SDL_Event ev = default;
        ev.type = (uint)SDL_EventType.SDL_EVENT_USER;
        SDL_PushEvent(&ev);
    }

    private InputEvent? TranslateEvent(SDL_Event* ev)
    {
        switch ((SDL_EventType)ev->type)
        {
            case SDL_EventType.SDL_EVENT_QUIT:
            case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                _isOpen = false;
                return new WindowEvent
                {
                    Type = WindowEventType.Close,
                    Timestamp = ev->common.timestamp
                };

            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
            {
                var winEv = ev->window;
                return new WindowEvent
                {
                    Type = WindowEventType.Resized,
                    Width = winEv.data1,
                    Height = winEv.data2,
                    Timestamp = ev->common.timestamp
                };
            }

            // Content invalidated — needs repaint (e.g., after maximize, restore, or unocclude)
            case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
            case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
            case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                return new WindowEvent
                {
                    Type = WindowEventType.Exposed,
                    Timestamp = ev->common.timestamp
                };

            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                return new WindowEvent
                {
                    Type = WindowEventType.FocusGained,
                    Timestamp = ev->common.timestamp
                };

            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                return new WindowEvent
                {
                    Type = WindowEventType.FocusLost,
                    Timestamp = ev->common.timestamp
                };

            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
            {
                var motion = ev->motion;
                return new MouseEvent
                {
                    Type = MouseEventType.Move,
                    X = motion.x,
                    Y = motion.y,
                    Button = MouseButton.None,
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
            {
                var btn = ev->button;
                return new MouseEvent
                {
                    Type = MouseEventType.ButtonDown,
                    X = btn.x,
                    Y = btn.y,
                    Button = MapMouseButton(btn.button),
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
            {
                var btn = ev->button;
                return new MouseEvent
                {
                    Type = MouseEventType.ButtonUp,
                    X = btn.x,
                    Y = btn.y,
                    Button = MapMouseButton(btn.button),
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_KEY_DOWN:
            {
                var key = ev->key;
                return new KeyboardEvent
                {
                    Type = KeyboardEventType.KeyDown,
                    Key = MapScancode(key.scancode),
                    Shift = (key.mod & SDL_Keymod.SDL_KMOD_SHIFT) != 0,
                    Ctrl = (key.mod & SDL_Keymod.SDL_KMOD_CTRL) != 0,
                    Alt = (key.mod & SDL_Keymod.SDL_KMOD_ALT) != 0,
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_KEY_UP:
            {
                var key = ev->key;
                return new KeyboardEvent
                {
                    Type = KeyboardEventType.KeyUp,
                    Key = MapScancode(key.scancode),
                    Shift = (key.mod & SDL_Keymod.SDL_KMOD_SHIFT) != 0,
                    Ctrl = (key.mod & SDL_Keymod.SDL_KMOD_CTRL) != 0,
                    Alt = (key.mod & SDL_Keymod.SDL_KMOD_ALT) != 0,
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
            {
                var textEv = ev->text;
                string text = Marshal.PtrToStringUTF8((IntPtr)textEv.text) ?? "";
                return new TextInputEvent
                {
                    Text = text,
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
            {
                var wheel = ev->wheel;
                return new ScrollEvent
                {
                    X = wheel.mouse_x,
                    Y = wheel.mouse_y,
                    DeltaX = wheel.x,
                    DeltaY = wheel.y,
                    Timestamp = ev->common.timestamp
                };
            }

            case SDL_EventType.SDL_EVENT_DROP_FILE:
            {
                var drop = ev->drop;
                string? file = Marshal.PtrToStringUTF8((IntPtr)drop.data);
                if (file != null)
                {
                    return new FileDropEvent
                    {
                        Files = [file],
                        X = drop.x,
                        Y = drop.y,
                        Timestamp = ev->common.timestamp
                    };
                }
                return null;
            }

            default:
                return null;
        }
    }

    private static MouseButton MapMouseButton(byte button) => button switch
    {
        1 => MouseButton.Left,
        2 => MouseButton.Middle,
        3 => MouseButton.Right,
        _ => MouseButton.None,
    };

    private static KeyCode MapScancode(SDL_Scancode scancode) => scancode switch
    {
        SDL_Scancode.SDL_SCANCODE_A => KeyCode.A,
        SDL_Scancode.SDL_SCANCODE_B => KeyCode.B,
        SDL_Scancode.SDL_SCANCODE_C => KeyCode.C,
        SDL_Scancode.SDL_SCANCODE_D => KeyCode.D,
        SDL_Scancode.SDL_SCANCODE_E => KeyCode.E,
        SDL_Scancode.SDL_SCANCODE_F => KeyCode.F,
        SDL_Scancode.SDL_SCANCODE_G => KeyCode.G,
        SDL_Scancode.SDL_SCANCODE_H => KeyCode.H,
        SDL_Scancode.SDL_SCANCODE_I => KeyCode.I,
        SDL_Scancode.SDL_SCANCODE_J => KeyCode.J,
        SDL_Scancode.SDL_SCANCODE_K => KeyCode.K,
        SDL_Scancode.SDL_SCANCODE_L => KeyCode.L,
        SDL_Scancode.SDL_SCANCODE_M => KeyCode.M,
        SDL_Scancode.SDL_SCANCODE_N => KeyCode.N,
        SDL_Scancode.SDL_SCANCODE_O => KeyCode.O,
        SDL_Scancode.SDL_SCANCODE_P => KeyCode.P,
        SDL_Scancode.SDL_SCANCODE_Q => KeyCode.Q,
        SDL_Scancode.SDL_SCANCODE_R => KeyCode.R,
        SDL_Scancode.SDL_SCANCODE_S => KeyCode.S,
        SDL_Scancode.SDL_SCANCODE_T => KeyCode.T,
        SDL_Scancode.SDL_SCANCODE_U => KeyCode.U,
        SDL_Scancode.SDL_SCANCODE_V => KeyCode.V,
        SDL_Scancode.SDL_SCANCODE_W => KeyCode.W,
        SDL_Scancode.SDL_SCANCODE_X => KeyCode.X,
        SDL_Scancode.SDL_SCANCODE_Y => KeyCode.Y,
        SDL_Scancode.SDL_SCANCODE_Z => KeyCode.Z,

        SDL_Scancode.SDL_SCANCODE_0 => KeyCode.Num0,
        SDL_Scancode.SDL_SCANCODE_1 => KeyCode.Num1,
        SDL_Scancode.SDL_SCANCODE_2 => KeyCode.Num2,
        SDL_Scancode.SDL_SCANCODE_3 => KeyCode.Num3,
        SDL_Scancode.SDL_SCANCODE_4 => KeyCode.Num4,
        SDL_Scancode.SDL_SCANCODE_5 => KeyCode.Num5,
        SDL_Scancode.SDL_SCANCODE_6 => KeyCode.Num6,
        SDL_Scancode.SDL_SCANCODE_7 => KeyCode.Num7,
        SDL_Scancode.SDL_SCANCODE_8 => KeyCode.Num8,
        SDL_Scancode.SDL_SCANCODE_9 => KeyCode.Num9,

        SDL_Scancode.SDL_SCANCODE_F1 => KeyCode.F1,
        SDL_Scancode.SDL_SCANCODE_F2 => KeyCode.F2,
        SDL_Scancode.SDL_SCANCODE_F3 => KeyCode.F3,
        SDL_Scancode.SDL_SCANCODE_F4 => KeyCode.F4,
        SDL_Scancode.SDL_SCANCODE_F5 => KeyCode.F5,
        SDL_Scancode.SDL_SCANCODE_F6 => KeyCode.F6,
        SDL_Scancode.SDL_SCANCODE_F7 => KeyCode.F7,
        SDL_Scancode.SDL_SCANCODE_F8 => KeyCode.F8,
        SDL_Scancode.SDL_SCANCODE_F9 => KeyCode.F9,
        SDL_Scancode.SDL_SCANCODE_F10 => KeyCode.F10,
        SDL_Scancode.SDL_SCANCODE_F11 => KeyCode.F11,
        SDL_Scancode.SDL_SCANCODE_F12 => KeyCode.F12,

        SDL_Scancode.SDL_SCANCODE_ESCAPE => KeyCode.Escape,
        SDL_Scancode.SDL_SCANCODE_TAB => KeyCode.Tab,
        SDL_Scancode.SDL_SCANCODE_CAPSLOCK => KeyCode.CapsLock,
        SDL_Scancode.SDL_SCANCODE_SPACE => KeyCode.Space,
        SDL_Scancode.SDL_SCANCODE_RETURN => KeyCode.Enter,
        SDL_Scancode.SDL_SCANCODE_BACKSPACE => KeyCode.Backspace,
        SDL_Scancode.SDL_SCANCODE_DELETE => KeyCode.Delete,

        SDL_Scancode.SDL_SCANCODE_UP => KeyCode.Up,
        SDL_Scancode.SDL_SCANCODE_DOWN => KeyCode.Down,
        SDL_Scancode.SDL_SCANCODE_LEFT => KeyCode.Left,
        SDL_Scancode.SDL_SCANCODE_RIGHT => KeyCode.Right,

        SDL_Scancode.SDL_SCANCODE_HOME => KeyCode.Home,
        SDL_Scancode.SDL_SCANCODE_END => KeyCode.End,
        SDL_Scancode.SDL_SCANCODE_PAGEUP => KeyCode.PageUp,
        SDL_Scancode.SDL_SCANCODE_PAGEDOWN => KeyCode.PageDown,

        SDL_Scancode.SDL_SCANCODE_INSERT => KeyCode.Insert,
        SDL_Scancode.SDL_SCANCODE_PRINTSCREEN => KeyCode.PrintScreen,
        SDL_Scancode.SDL_SCANCODE_PAUSE => KeyCode.Pause,

        SDL_Scancode.SDL_SCANCODE_LSHIFT => KeyCode.LeftShift,
        SDL_Scancode.SDL_SCANCODE_RSHIFT => KeyCode.RightShift,
        SDL_Scancode.SDL_SCANCODE_LCTRL => KeyCode.LeftCtrl,
        SDL_Scancode.SDL_SCANCODE_RCTRL => KeyCode.RightCtrl,
        SDL_Scancode.SDL_SCANCODE_LALT => KeyCode.LeftAlt,
        SDL_Scancode.SDL_SCANCODE_RALT => KeyCode.RightAlt,
        SDL_Scancode.SDL_SCANCODE_LGUI => KeyCode.LeftSuper,
        SDL_Scancode.SDL_SCANCODE_RGUI => KeyCode.RightSuper,
        SDL_Scancode.SDL_SCANCODE_MENU => KeyCode.Menu,

        _ => KeyCode.Unknown,
    };

    public void SetClipboardText(string text)
    {
        SDL_SetClipboardText(text);
    }

    public string? GetClipboardText()
    {
        if (!SDL_HasClipboardText())
            return null;
        return SDL_GetClipboardText();
    }

    public bool HasClipboardText()
    {
        return SDL_HasClipboardText();
    }

    private void EnsureWindow()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_window == null)
            throw new InvalidOperationException("Window has not been created. Call Create() first.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _isOpen = false;

        if (_liveResizeCallback != null)
        {
            SDL_RemoveEventWatch(&LiveResizeEventWatcher, IntPtr.Zero);
            _liveResizeCallback = null;
            if (_activeInstance == this)
                _activeInstance = null;
        }

        if (_glContext != null)
        {
            SDL_GL_DestroyContext(_glContext);
            _glContext = null;
        }

        if (_renderer != null)
        {
            SDL_DestroyRenderer(_renderer);
            _renderer = null;
        }

        if (_window != null)
        {
            SDL_DestroyWindow(_window);
            _window = null;
        }

        SDL_Quit();
        GC.SuppressFinalize(this);
    }
}
