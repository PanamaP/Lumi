using Lumi.Core;

namespace Lumi.Platform;

public enum VSyncMode
{
    Off = 0,
    On = 1,
    Adaptive = -1
}

public interface IPlatformWindow : IDisposable
{
    void Create(string title, int width, int height);
    void Show();
    void Hide();
    void SetTitle(string title);
    void Resize(int width, int height);
    float GetDpiScale();
    (int Width, int Height) GetSize();
    (int Width, int Height) GetPixelSize();

    /// <summary>
    /// Poll all pending events, returns list of normalized InputEvents.
    /// </summary>
    List<InputEvent> PollEvents();

    /// <summary>
    /// Block until at least one event arrives, then drain all pending events.
    /// Used when idle (nothing dirty) to avoid burning CPU.
    /// </summary>
    List<InputEvent> WaitForEvents();

    /// <summary>
    /// Get the native window pointer (for Skia surface creation).
    /// </summary>
    IntPtr NativeHandle { get; }

    bool IsOpen { get; }

    /// <summary>
    /// Returns the display's native refresh rate in Hz (e.g. 60, 120, 144).
    /// </summary>
    int DisplayRefreshRate { get; }

    /// <summary>
    /// Enable or disable vertical sync.
    /// </summary>
    void SetVSync(VSyncMode mode);

    /// <summary>
    /// Create an OpenGL context for GPU-accelerated rendering.
    /// Must be called after Create() and before any GL operations.
    /// </summary>
    void CreateGLContext();

    /// <summary>
    /// Make this window's GL context current for the calling thread.
    /// </summary>
    void MakeCurrent();

    /// <summary>
    /// Destroy the OpenGL context without disposing the window.
    /// </summary>
    void DestroyGLContext();

    /// <summary>
    /// Swap the OpenGL front/back buffers (present the frame).
    /// </summary>
    void SwapBuffers();

    /// <summary>
    /// Whether an OpenGL context has been created.
    /// </summary>
    bool HasGLContext { get; }

    /// <summary>
    /// Register a callback for live window resize rendering.
    /// Called from the OS modal resize loop so the application can repaint at each new size.
    /// </summary>
    void SetLiveResizeCallback(Action<int, int> callback);

    /// <summary>
    /// OS-level display preferences (dark mode, high contrast).
    /// </summary>
    SystemPreferences SystemPreferences { get; }

    /// <summary>
    /// Copy text to the OS clipboard.
    /// </summary>
    void SetClipboardText(string text);

    /// <summary>
    /// Get text from the OS clipboard. Returns null if no text is available.
    /// </summary>
    string? GetClipboardText();

    /// <summary>
    /// Check whether the OS clipboard contains text.
    /// </summary>
    bool HasClipboardText();
}
