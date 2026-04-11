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

    /// <summary>
    /// The number of currently managed secondary windows (including those pending close).
    /// </summary>
    public int Count => _windows.Count;

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
        }
        catch
        {
            renderer?.Dispose();
            renderer = new SkiaRenderer();
            useGpu = false;
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

        _windows.Add(new ManagedWindow(window, platformWindow, renderer, useGpu));
        return window;
    }

    /// <summary>
    /// Close and dispose all managed secondary windows.
    /// </summary>
    public void CloseAll()
    {
        foreach (var managed in _windows)
            DisposeManaged(managed);
        _windows.Clear();
    }

    /// <summary>
    /// Called each frame from the main application loop.
    /// Performs style resolution, layout, and rendering for each open secondary window,
    /// and cleans up any windows that have been closed.
    /// </summary>
    internal void Update()
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

            if (win.Root.IsDirty)
            {
                var (w, h) = managed.PlatformWindow.GetPixelSize();

                // Style resolution
                var pseudoState = new PseudoClassState(false, false, false);
                win.StyleResolver.SetViewport(w, h);
                win.StyleResolver.ResolveStyles(win.Root, pseudoState);

                // Layout
                win.LayoutEngine.MeasureFunc = MeasureElement;
                win.LayoutEngine.CalculateLayout(win.Root, w, h);

                // Render
                managed.Renderer.EnsureSize(w, h);
                managed.Renderer.Paint(win.Root);

                if (managed.UseGpu)
                {
                    managed.PlatformWindow.SwapBuffers();
                }

                managed.Window.App?.MarkClean();
            }

            next:;
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
        managed.Window.LayoutEngine.Dispose();
        managed.Renderer.Dispose();
        managed.PlatformWindow.Dispose();
        managed.Window.PlatformWindow = null;
        managed.Window.SecondaryRenderer = null;
    }

    internal sealed record ManagedWindow(
        SecondaryWindow Window,
        Sdl3Window PlatformWindow,
        SkiaRenderer Renderer,
        bool UseGpu);
}
