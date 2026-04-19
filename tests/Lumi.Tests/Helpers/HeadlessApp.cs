using Lumi.Core;
using Lumi.Core.Time;
using Lumi.Layout;
using Lumi.Rendering;
using Lumi.Styling;
using Lumi.Text;
using SkiaSharp;

namespace Lumi.Tests.Helpers;

/// <summary>
/// In-process test harness combining <see cref="HeadlessPipeline"/>, an <see cref="Application"/>,
/// a <see cref="ManualTimeSource"/> and a scripted input queue.
///
/// The harness installs the manual clock as <see cref="TimeSource.Default"/> on construction
/// and restores the previous source on <see cref="Dispose"/>, so caret-blink and other
/// <see cref="TimeSource.Default"/> consumers become deterministic for the duration of a test.
/// </summary>
public sealed class HeadlessApp : IDisposable
{
    public HeadlessPipeline Pipeline { get; }
    public Application App { get; }
    public ManualTimeSource Clock { get; }
    public Element Root => Pipeline.Root;

    private readonly Queue<InputEvent> _queue = new();
    private readonly ITimeSource _previousDefault;
    private bool _disposed;

    public HeadlessApp(string html, string css, int width = 800, int height = 600)
    {
        Clock = new ManualTimeSource();
        _previousDefault = TimeSource.Default;
        TimeSource.Default = Clock;

        Pipeline = new HeadlessPipeline(width, height);
        Pipeline.Load(html, css);
        Pipeline.Execute();

        App = new Application { Root = Pipeline.Root };
    }

    /// <summary>Append a single input event to the scripted queue (drained on next <see cref="Tick"/>).</summary>
    public void EnqueueInput(InputEvent ev) => _queue.Enqueue(ev);

    /// <summary>
    /// Advance the manual clock by <paramref name="dt"/> seconds, drain queued input through
    /// <see cref="Application.ProcessInputEvent(InputEvent)"/>, and run an Update pass.
    /// Returns the number of input events drained.
    /// </summary>
    public int Tick(double dt = 1.0 / 60.0)
    {
        Clock.Advance(dt);
        int drained = 0;
        while (_queue.Count > 0)
        {
            App.ProcessInputEvent(_queue.Dequeue());
            drained++;
        }
        App.Update();
        return drained;
    }

    /// <summary>
    /// Render (or re-render) the current root through the pipeline.
    /// </summary>
    public void Render() => Pipeline.Rerender();

    /// <summary>
    /// Tick repeatedly until <paramref name="predicate"/> returns true or
    /// <paramref name="maxFrames"/> is reached. Returns true if predicate was satisfied.
    /// </summary>
    public bool RunUntil(Func<bool> predicate, int maxFrames = 1000, double dt = 1.0 / 60.0)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (predicate()) return true;
            Tick(dt);
        }
        return predicate();
    }

    /// <summary>
    /// Cycle focus through focusable elements in DOM order, mirroring LumiApp's Tab-handling.
    /// </summary>
    public void Tab(bool shift = false)
    {
        var focusables = CollectFocusables(App.Root);
        if (focusables.Count == 0) return;

        int currentIndex = App.FocusedElement != null
            ? focusables.IndexOf(App.FocusedElement)
            : -1;

        int nextIndex = shift
            ? (currentIndex <= 0 ? focusables.Count - 1 : currentIndex - 1)
            : (currentIndex >= focusables.Count - 1 ? 0 : currentIndex + 1);

        App.SetFocus(focusables[nextIndex]);
    }

    private static List<Element> CollectFocusables(Element root)
    {
        var result = new List<Element>();
        Walk(root, result);
        result.Sort((a, b) =>
        {
            int aIdx = a.TabIndex <= 0 ? int.MaxValue : a.TabIndex;
            int bIdx = b.TabIndex <= 0 ? int.MaxValue : b.TabIndex;
            return aIdx.CompareTo(bIdx);
        });
        return result;

        static void Walk(Element e, List<Element> acc)
        {
            if (e.IsFocusable) acc.Add(e);
            foreach (var c in e.Children) Walk(c, acc);
        }
    }

    /// <summary>
    /// Snapshot of all element ids and their layout boxes plus all rendered pixels.
    /// Used by tests to assert deterministic replay.
    /// </summary>
    public (string LayoutDigest, byte[] Pixels) Snapshot()
    {
        var sb = new System.Text.StringBuilder();
        Walk(App.Root, sb);
        var pixels = new byte[Pipeline.Width * Pipeline.Height * 4];
        var ptr = Pipeline.Renderer.GetPixels();
        if (ptr != IntPtr.Zero)
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
        return (sb.ToString(), pixels);

        static void Walk(Element e, System.Text.StringBuilder sb)
        {
            sb.Append(e.TagName).Append('|')
              .Append(e.LayoutBox.X).Append(',').Append(e.LayoutBox.Y).Append(',')
              .Append(e.LayoutBox.Width).Append(',').Append(e.LayoutBox.Height).Append(';');
            foreach (var c in e.Children) Walk(c, sb);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        TimeSource.Default = _previousDefault;
        Pipeline.Dispose();
    }
}
