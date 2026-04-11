using Lumi.Core;
using Lumi.Core.Animation;
using Lumi.Input;
using Lumi.Platform;
using Lumi.Rendering;
using Lumi.Styling;

namespace Lumi;

/// <summary>
/// Hosts a Lumi window and runs the application loop.
/// This is the main entry point for Lumi applications.
/// </summary>
public sealed class LumiApp : IDisposable
{
    private readonly Window _window;
    private readonly Sdl3Window _platformWindow;
    private readonly SkiaRenderer _renderer;
    private readonly Sdl3RenderTarget _renderTarget;
    private readonly Application _app;
    private readonly InteractionState _interaction = new();
    private readonly FrameClock _frameClock;
    private readonly FrameMetrics _frameMetrics = new();
    private readonly DirtyRegionTracker _dirtyTracker = new();
    private readonly TransitionManager _transitionManager = new();
    private readonly Inspector _inspector = new();
    private readonly WindowManager _windowManager = new();
    private HotReload? _hotReload;
    private bool _disposed;
    private bool _useGpuRendering;
    private bool _resizeOnly;
    private bool _liveResizeRendered;
    private bool _wasActiveLastFrame = true;
    private bool _liveResizeRegistered;

    private LumiApp(Window window)
    {
        _window = window;
        _app = new Application();

        // Wire up TemplateEngine's HTML parser so template directives can parse HTML
        Core.Binding.TemplateEngine.HtmlParser ??= HtmlTemplateParser.Parse;

        _platformWindow = new Sdl3Window();
        _platformWindow.Create(window.Title, window.Width, window.Height);

        // Wire up clipboard delegates so Lumi.Core can access the OS clipboard
        Clipboard.Initialize(
            () => _platformWindow.GetClipboardText(),
            text => _platformWindow.SetClipboardText(text));

        // Initialize frame clock with detected display refresh rate
        _frameClock = new FrameClock(_platformWindow.DisplayRefreshRate);

        // Expose frame metrics and window manager to the window for app-level access
        _window.FrameMetrics = _frameMetrics;
        _window.Renderer = _renderer;
        _window.Windows = _windowManager;

        // Try GPU-accelerated rendering
        _renderer = new SkiaRenderer();
        try
        {
            _platformWindow.CreateGLContext();
            _renderer.InitializeGpu();
            _useGpuRendering = _renderer.IsGpuAccelerated;
        }
        catch
        {
            _useGpuRendering = false;
        }

        if (_useGpuRendering)
        {
            // VSync handled by GL swap interval (set in CreateGLContext)
            _renderTarget = null!;
        }
        else
        {
            // Fallback: CPU rendering with SDL streaming texture
            _renderTarget = new Sdl3RenderTarget(_platformWindow.RendererPtr);
            _platformWindow.SetVSync(VSyncMode.On);
        }
    }

    /// <summary>
    /// Create and run a Lumi application. Blocks until the window is closed.
    /// </summary>
    public static void Run(Window window)
    {
        using var app = new LumiApp(window);
        app.Start();
    }

    private void Start()
    {
        // Detect OS dark-mode preference and apply initial theme
        var prefs = new SystemPreferences();
        prefs.Detect();
        _window.Theme.SetSystemPreference(prefs.IsDarkMode);
        _window.Theme.ApplyTo(_window.Root);

        _app.Root = _window.Root;
        _window.OnReady();
        _app.Root = _window.Root;

        // Wire up element measurement for text auto-sizing in the layout engine
        _window.LayoutEngine.MeasureFunc = MeasureElement;

        _platformWindow.Show();
        _app.Start();
        _app.Root.MarkDirty();

        Console.WriteLine($"[Lumi] Target refresh rate: {_frameClock.TargetRefreshRate}Hz " +
                          $"| GPU: {_useGpuRendering} | VSync: adaptive");

        // Start hot reload if enabled
        if (_window.EnableHotReload && (_window.HtmlPath != null || _window.CssPath != null))
        {
            _hotReload = new HotReload(_window, _window.HtmlPath, _window.CssPath,
                wakeUp: () => _platformWindow.WakeUp());
            _hotReload.Start();
        }

        while (_app.IsRunning && _platformWindow.IsOpen)
        {
            _frameClock.BeginFrame();
            _frameMetrics.BeginFrame();

            _frameMetrics.BeginStage();
            // When idle, block on SDL_WaitEvent for zero CPU usage.
            // Stay active (poll) if: something is dirty, we rendered last frame
            // (OnUpdate may re-dirty for animations), or tweens/transitions are running.
            bool stayActive = _app.IsDirty || _inspector.IsEnabled || _wasActiveLastFrame
                || AnimationExtensions.GlobalTweenEngine.ActiveCount > 0
                || _window.StyleResolver.KeyframePlayer.ActiveCount > 0;
            var events = stayActive
                ? _platformWindow.PollEvents()
                : _platformWindow.WaitForEvents();
            _resizeOnly = false;
            _liveResizeRendered = false;
            _app.ProcessInput(events);
            UpdateInteractionState(events);
            _frameMetrics.RecordPoll();

            // Apply any pending hot reload changes on the main thread
            if (_hotReload != null && _hotReload.HasPendingChanges)
            {
                _hotReload.ApplyPendingChanges();
                _app.Root = _window.Root;

                // HTML reload replaces element tree — re-register event handlers
                if (_hotReload.HtmlWasReloaded)
                {
                    _hotReload.HtmlWasReloaded = false;
                    _window.OnHtmlReloaded();
                }
            }

            _frameMetrics.BeginStage();
            _window.OnUpdate();
            _windowManager.Update();
            _app.Update();
            _transitionManager.Update(_frameClock.DeltaTime);
            AnimationExtensions.GlobalTweenEngine.Update((float)_frameClock.DeltaTime);
            _window.StyleResolver.KeyframePlayer.Update((float)_frameClock.DeltaTime);
            _frameMetrics.RecordUpdate();

            bool needsRepaint = _app.IsDirty || _inspector.IsEnabled;

            if (_app.IsDirty)
            {
                var (w, h) = _platformWindow.GetPixelSize();

                // Set viewport context for vh/vw/calc() resolution
                PropertyApplier.SetViewportContext(w, h);

                // Set viewport for @media query evaluation
                _window.StyleResolver.SetViewport(w, h);

                _frameMetrics.BeginStage();
                if (!_resizeOnly)
                {
                    var pseudoState = new PseudoClassState(
                        IsHovered: _interaction.HoveredElement != null,
                        IsFocused: _interaction.FocusedElement != null,
                        IsActive: _interaction.ActiveElement != null);
                    _window.StyleResolver.ResolveStyles(_window.Root, pseudoState);
                }
                _frameMetrics.RecordStyle();

                _frameMetrics.BeginStage();
                _window.LayoutEngine.CalculateLayout(_window.Root, w, h);

                // Collect dirty regions from elements whose layout changed
                _dirtyTracker.Clear();
                CollectDirtyRegions(_window.Root, _dirtyTracker);
                _frameMetrics.RecordLayout();
            }

            if (needsRepaint)
            {
                var (w, h) = _platformWindow.GetPixelSize();

                _frameMetrics.BeginStage();
                _renderer.EnsureSize(w, h);
                if (_dirtyTracker.HasDirtyRegions)
                    _renderer.PaintDirtyRegions(_window.Root, _dirtyTracker);
                else
                    _renderer.Paint(_window.Root);

                // Draw inspector overlay after scene paint, before present
                if (_inspector.IsEnabled && _renderer.Canvas != null)
                {
                    _inspector.Draw(_renderer.Canvas, _window.Root,
                        _interaction.HoveredElement, w, h);
                    if (_useGpuRendering && _renderer.Canvas != null)
                        _renderer.Canvas.Flush();
                }

                _frameMetrics.RecordPaint();

                _frameMetrics.BeginStage();
                if (_useGpuRendering)
                {
                    _platformWindow.SwapBuffers();
                }
                else
                {
                    _renderTarget.EnsureSize(w, h);
                    _renderTarget.UpdatePixels(_renderer.GetPixels(), _renderer.Pitch);
                    _renderTarget.Present();
                }
                _frameMetrics.RecordPresent();

                // MarkClean also snapshots layout boxes (single tree walk)
                _app.MarkClean();
            }

            // Register live resize callback after the first styled paint so the initial
            // Exposed event is handled by the main loop (with style resolution) instead of
            // the lightweight live-resize path that skips styles.
            if (needsRepaint && !_liveResizeRegistered)
            {
                _platformWindow.SetLiveResizeCallback(OnLiveResize);
                _liveResizeRegistered = true;
            }

            _frameMetrics.EndFrame();
            _wasActiveLastFrame = needsRepaint;

            // VSync (via SwapBuffers) handles frame pacing for active frames.
            // WaitForEvents handles idle frames (blocks until input arrives).
        }
    }

    /// <summary>
    /// Called from the SDL event watcher during the OS modal resize loop.
    /// Performs a minimal render pass so the window content updates live while dragging.
    /// </summary>
    private void OnLiveResize(int widthPixels, int heightPixels)
    {
        var (w, h) = _platformWindow.GetPixelSize();

        _window.Root.MarkDirty();
        _window.LayoutEngine.CalculateLayout(_window.Root, w, h);

        _renderer.EnsureSize(w, h);
        _renderer.Paint(_window.Root);

        if (_useGpuRendering)
        {
            _platformWindow.SwapBuffers();
        }
        else
        {
            _renderTarget.EnsureSize(w, h);
            _renderTarget.UpdatePixels(_renderer.GetPixels(), _renderer.Pitch);
            _renderTarget.Present();
        }

        _app.MarkClean();
        _liveResizeRendered = true;
    }

    private void UpdateInteractionState(List<InputEvent> events)
    {
        bool hadResize = false;
        bool wasDirtyBefore = _app.IsDirty;

        foreach (var evt in events)
        {
            switch (evt)
            {
                case MouseEvent { Type: MouseEventType.Move } mouse:
                    _interaction.SetHovered(
                        HitTester.HitTest(_window.Root, mouse.X, mouse.Y));
                    break;
                case MouseEvent { Type: MouseEventType.ButtonDown } down:
                    var target = HitTester.HitTest(_window.Root, down.X, down.Y);
                    _interaction.SetActive(target);
                    SetFocusedElement(target);
                    break;
                case MouseEvent { Type: MouseEventType.ButtonUp }:
                    _interaction.SetActive(null);
                    break;
                case ScrollEvent scroll:
                    HandleScrollWheel(scroll);
                    break;
                case KeyboardEvent { Type: KeyboardEventType.KeyDown } key:
                    if (key.Key == KeyCode.F12)
                    {
                        _inspector.Toggle();
                        _window.Root.MarkDirty();
                    }
                    else if (key.Key == KeyCode.F5)
                    {
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var screenshotDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "LumiScreenshots");
                        Directory.CreateDirectory(screenshotDir);
                        var path = Path.Combine(screenshotDir, $"screenshot_{timestamp}.png");
                        if (_renderer.ExportPng(path))
                            Console.WriteLine($"[Lumi] Screenshot saved: {path}");
                    }
                    else
                        HandleKeyboardNav(key);
                    break;
                case WindowEvent { Type: WindowEventType.Resized }:
                case WindowEvent { Type: WindowEventType.Exposed }:
                    // Coalesce: just record that a resize/expose happened; MarkDirty once after the loop
                    hadResize = true;
                    break;
            }
        }

        if (hadResize && !_liveResizeRendered)
        {
            _window.Root.MarkDirty();
            // If the tree was clean before and only resize events dirtied it, skip style resolution
            if (!wasDirtyBefore)
                _resizeOnly = true;
        }
    }

    private const float ScrollSpeed = 40f;

    private void HandleScrollWheel(ScrollEvent scroll)
    {
        var target = HitTester.HitTest(_window.Root, scroll.X, scroll.Y);
        var scrollable = FindScrollableAncestor(target);
        if (scrollable != null)
        {
            scrollable.ScrollBy(-scroll.DeltaX * ScrollSpeed, -scroll.DeltaY * ScrollSpeed);
        }
    }

    private static Element? FindScrollableAncestor(Element? element)
    {
        var current = element;
        while (current != null)
        {
            if (current.ComputedStyle.Overflow == Overflow.Scroll)
                return current;
            current = current.Parent;
        }
        return null;
    }

    private void HandleKeyboardNav(KeyboardEvent key)
    {
        if (key.Key == KeyCode.Tab)
        {
            var focusables = CollectFocusableElements(_window.Root);
            if (focusables.Count == 0) return;

            int currentIndex = _interaction.FocusedElement != null
                ? focusables.IndexOf(_interaction.FocusedElement)
                : -1;

            int nextIndex;
            if (key.Shift)
                nextIndex = currentIndex <= 0 ? focusables.Count - 1 : currentIndex - 1;
            else
                nextIndex = currentIndex >= focusables.Count - 1 ? 0 : currentIndex + 1;

            SetFocusedElement(focusables[nextIndex]);
        }
        else if (key.Key is KeyCode.Enter or KeyCode.Space)
        {
            if (_interaction.FocusedElement != null)
            {
                EventDispatcher.Dispatch(
                    new RoutedMouseEvent("Click") { Button = MouseButton.None },
                    _interaction.FocusedElement);
            }
        }
    }

    private void SetFocusedElement(Element? element)
    {
        // Clear previous focus
        if (_interaction.FocusedElement != null)
            _interaction.FocusedElement.IsFocused = false;

        // Walk up to find a focusable element if the target isn't focusable
        var focusTarget = element;
        while (focusTarget != null && !focusTarget.IsFocusable)
            focusTarget = focusTarget.Parent;

        _interaction.SetFocused(focusTarget);

        if (focusTarget != null)
        {
            focusTarget.IsFocused = true;
            focusTarget.MarkDirty();
        }
    }

    private static List<Element> CollectFocusableElements(Element root)
    {
        var result = new List<Element>();
        CollectFocusables(root, result);
        // Sort by TabIndex (0 last in tab order per HTML spec), then DOM order
        result.Sort((a, b) =>
        {
            int aIdx = a.TabIndex <= 0 ? int.MaxValue : a.TabIndex;
            int bIdx = b.TabIndex <= 0 ? int.MaxValue : b.TabIndex;
            return aIdx.CompareTo(bIdx);
        });
        return result;
    }

    private static void CollectFocusables(Element element, List<Element> result)
    {
        if (element.ComputedStyle.Display == DisplayMode.None) return;
        if (element.IsFocusable)
            result.Add(element);
        foreach (var child in element.Children)
            CollectFocusables(child, result);
    }

    /// <summary>
    /// Walk the element tree and collect dirty regions by comparing current vs previous layout boxes.
    /// </summary>
    private static void CollectDirtyRegions(Element element, DirtyRegionTracker tracker)
    {
        if (element.IsDirty)
        {
            // Add both old and new positions as dirty (handles element movement)
            var abs = GetAbsoluteBox(element);
            tracker.Add(abs);

            if (element.PreviousLayoutBox.Width > 0 || element.PreviousLayoutBox.Height > 0)
            {
                tracker.Add(element.PreviousLayoutBox);
            }
        }

        foreach (var child in element.Children)
            CollectDirtyRegions(child, tracker);
    }

    /// <summary>
    /// Compute the absolute (screen-space) bounding box for an element.
    /// </summary>
    private static LayoutBox GetAbsoluteBox(Element element)
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

    /// <summary>
    /// Measure an element's intrinsic size. Used as a Yoga measure callback
    /// for leaf elements (text, images).
    /// </summary>
    private static (float Width, float Height) MeasureElement(Element element, float availableWidth, float availableHeight)
    {
        if (element is TextElement textElement && !string.IsNullOrEmpty(textElement.Text))
        {
            return TextLayout.Measure(textElement.Text, availableWidth, element.ComputedStyle);
        }

        if (element is ImageElement imageElement)
        {
            // Use natural image dimensions if available, otherwise default
            float natW = imageElement.NaturalWidth > 0 ? imageElement.NaturalWidth : 150;
            float natH = imageElement.NaturalHeight > 0 ? imageElement.NaturalHeight : 150;

            // Scale to fit within available width while maintaining aspect ratio
            if (availableWidth < natW && availableWidth > 0 && availableWidth < float.MaxValue)
            {
                float scale = availableWidth / natW;
                natW = availableWidth;
                natH *= scale;
            }

            return (natW, natH);
        }

        return (0, 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _windowManager.CloseAll();
        _hotReload?.Dispose();
        _window.LayoutEngine.Dispose();
        _renderTarget?.Dispose();
        _renderer.Dispose();
        _platformWindow.Dispose();
    }
}
