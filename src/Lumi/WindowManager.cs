using Lumi.Core;
using Lumi.Platform;
using Lumi.Rendering;
using Lumi.Styling;

namespace Lumi;

/// <summary>
/// Manages secondary windows alongside the main application window.
/// Handles creation, rendering, layout, and disposal of secondary windows.
/// </summary>
public class WindowManager
{
    private readonly List<ManagedWindow> _windows = new();
    private readonly object _lock = new();

    /// <summary>
    /// The number of currently managed secondary windows (including those pending close).
    /// </summary>
    public int Count
    {
        get { lock (_lock) { return _windows.Count; } }
    }

    /// <summary>
    /// Open a secondary window. Creates the platform window, renderer,
    /// and wires up style/layout for the window's element tree.
    /// </summary>
    public SecondaryWindow Open(SecondaryWindow window)
    {
        var platformWindow = new Sdl3Window();
        platformWindow.Create(window.Title, window.Width, window.Height);

        var renderer = new SkiaRenderer();
        bool useGpu = false;
        try
        {
            platformWindow.CreateGLContext();
            renderer.InitializeGpu();
            useGpu = renderer.IsGpuAccelerated;

            if (!useGpu)
            {
                // GL context created but GPU acceleration unavailable: clean up and fall back to CPU
                renderer.Dispose();
                platformWindow.DestroyGLContext();
                renderer = new SkiaRenderer();
            }
        }
        catch
        {
            renderer?.Dispose();
            platformWindow.DestroyGLContext();
            renderer = new SkiaRenderer();
            useGpu = false;
        }

        if (!useGpu)
        {
            // CPU fallback: create SDL renderer and render target
            platformWindow.CreateSdlRenderer();
            platformWindow.SetVSync(VSyncMode.On);
        }

        window.PlatformWindow = platformWindow;
        window.SecondaryRenderer = renderer;
        window.Renderer = renderer;
        window.IsOpen = true;

        platformWindow.Show();
        window.OnReady();
        window.Root.MarkDirty();

        var app = new Application { Root = window.Root };
        app.Start();
        window.App = app;

        lock (_lock)
        {
            var renderTarget = useGpu ? null : new Sdl3RenderTarget(platformWindow.RendererPtr);
            _windows.Add(new ManagedWindow(window, platformWindow, renderer, useGpu, renderTarget));
        }
        return window;
    }

    /// <summary>
    /// Close and dispose all managed secondary windows.
    /// </summary>
    public void CloseAll()
    {
        lock (_lock)
        {
            foreach (var managed in _windows)
                DisposeManaged(managed);
            _windows.Clear();
        }
    }

    /// <summary>
    /// Called each frame from the main application loop.
    /// Performs style resolution, layout, and rendering for each open secondary window,
    /// and cleans up any windows that have been closed.
    /// </summary>
    internal void Update()
    {
        lock (_lock)
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                var managed = _windows[i];

                if (!managed.Window.IsOpen || !managed.PlatformWindow.IsOpen)
                {
                    DisposeManaged(managed);
                    _windows.RemoveAt(i);
                    continue;
                }

                // Poll events for this secondary window
                var events = managed.PlatformWindow.PollEvents();
                foreach (var evt in events)
                {
                    if (evt is WindowEvent { Type: WindowEventType.Close })
                    {
                        managed.Window.IsOpen = false;
                        DisposeManaged(managed);
                        _windows.RemoveAt(i);
                        goto next;
                    }
                }

                // Route input events through the secondary window's Application
                managed.Window.App?.ProcessInput(events);

                var win = managed.Window;

                // Update the window
                win.OnUpdate();
                managed.Window.App?.Update();

                // Re-check after update — callbacks may have closed the window
                if (!managed.Window.IsOpen || !managed.PlatformWindow.IsOpen)
                {
                    DisposeManaged(managed);
                    _windows.RemoveAt(i);
                    continue;
                }

                if (win.Root.IsDirty)
                {
                    var (w, h) = managed.PlatformWindow.GetPixelSize();

                    // Style resolution
                    var pseudoState = new PseudoClassState(false, false, false);
                    PropertyApplier.SetViewportContext(w, h);
                    win.StyleResolver.SetViewport(w, h);
                    win.StyleResolver.ResolveStyles(win.Root, pseudoState);

                    // Layout
                    win.LayoutEngine.MeasureFunc = MeasureElement;
                    win.LayoutEngine.CalculateLayout(win.Root, w, h);

                    // Render — make this window's GL context current first
                    if (managed.UseGpu)
                        managed.PlatformWindow.MakeCurrent();

                    managed.Renderer.EnsureSize(w, h);
                    managed.Renderer.Paint(win.Root);

                    if (managed.UseGpu)
                    {
                        managed.PlatformWindow.SwapBuffers();
                    }
                    else if (managed.RenderTarget is { } rt)
                    {
                        rt.EnsureSize(w, h);
                        rt.UpdatePixels(managed.Renderer.GetPixels(), managed.Renderer.Pitch);
                        rt.Present();
                    }

                    managed.Window.App?.MarkClean();
                }

                next:;
            }
        }
    }

    private static (float Width, float Height) MeasureElement(
        Element element, float availableWidth, float availableHeight)
    {
        if (element is TextElement textElement && !string.IsNullOrEmpty(textElement.Text))
            return TextLayout.Measure(textElement.Text, availableWidth, element.ComputedStyle);
        return (0, 0);
    }

    private static void DisposeManaged(ManagedWindow managed)
    {
        managed.Window.IsOpen = false;
        managed.Window.App?.RequestStop();
        managed.RenderTarget?.Dispose();
        managed.Renderer.Dispose();
        managed.PlatformWindow.Dispose();
        managed.Window.PlatformWindow = null;
        managed.Window.SecondaryRenderer = null;
        managed.Window.App = null;
    }

    internal sealed record ManagedWindow(
        SecondaryWindow Window,
        Sdl3Window PlatformWindow,
        SkiaRenderer Renderer,
        bool UseGpu,
        Sdl3RenderTarget? RenderTarget);
}
