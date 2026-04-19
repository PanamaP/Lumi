using Lumi.Core;
using Lumi.Core.Time;
using Lumi.Rendering;
using Lumi.Tests.Helpers;
using Xunit;

namespace Lumi.Tests.Dst;

/// <summary>
/// Deterministic input/timing replay tests built on <see cref="HeadlessApp"/>.
/// Mutates <see cref="TimeSource.Default"/>, so this collection is not parallelized.
/// </summary>
[Collection("Dst")]
public class InputReplayTests
{
    private const string BaseCss = @"
        body { font-family: sans-serif; }
        button { width: 80px; height: 30px; }
        .box { position: absolute; width: 100px; height: 100px; }
        #a { left: 50px; top: 50px; background: red; }
        #b { left: 100px; top: 100px; background: green; }
        #c { left: 150px; top: 150px; background: blue; }
        input { width: 200px; height: 30px; }
    ";

    [Fact]
    public void Tab_Cycles_Focusable_In_DOM_Order()
    {
        using var app = new HeadlessApp(
            "<div><button id='b1'>1</button><button id='b2'>2</button><button id='b3'>3</button></div>",
            BaseCss);

        var b1 = app.Pipeline.FindById("b1")!;
        var b2 = app.Pipeline.FindById("b2")!;
        var b3 = app.Pipeline.FindById("b3")!;

        app.Tab();
        Assert.Same(b1, app.App.FocusedElement);
        app.Tab();
        Assert.Same(b2, app.App.FocusedElement);
        app.Tab();
        Assert.Same(b3, app.App.FocusedElement);
        app.Tab();
        Assert.Same(b1, app.App.FocusedElement);
    }

    [Fact]
    public void Click_Topmost_Hits_Z_Ordered_Element()
    {
        using var app = new HeadlessApp(
            "<div><div id='a' class='box'></div><div id='b' class='box'></div><div id='c' class='box'></div></div>",
            BaseCss);

        // All three overlap at (175, 175). Last in DOM order ('c') is topmost.
        var hit = Application.HitTest(app.App.Root, 175, 175);
        Assert.NotNull(hit);
        Assert.Equal("c", hit!.Id);

        // Click on a region only 'a' covers (top-left corner of a).
        var onlyA = Application.HitTest(app.App.Root, 60, 60);
        Assert.Equal("a", onlyA!.Id);
    }

    [Fact]
    public void TextInput_After_KeyDown_Appends_To_Focused_Input()
    {
        using var app = new HeadlessApp(
            "<div><input id='field' /></div>",
            BaseCss);

        var input = (InputElement)app.Pipeline.FindById("field")!;
        app.App.SetFocus(input);

        app.EnqueueInput(new KeyboardEvent { Key = KeyCode.A, Type = KeyboardEventType.KeyDown });
        app.EnqueueInput(new TextInputEvent { Text = "a" });
        app.Tick();

        Assert.Equal("a", input.Value);
        Assert.Equal(1, input.CursorPosition);
    }

    [Fact]
    public void CursorBlink_Uses_TimeSource()
    {
        using var app = new HeadlessApp(
            "<div><input id='field' /></div>",
            BaseCss);

        var input = (InputElement)app.Pipeline.FindById("field")!;
        app.App.SetFocus(input);

        // An edit resets blink to "now" on the manual clock.
        app.EnqueueInput(new TextInputEvent { Text = "x" });
        app.Tick();

        long t0 = app.Clock.TickCount64;
        Assert.Equal(t0, input.LastEditTick);

        const long Period = SkiaRenderer.CaretBlinkPeriodMs;
        const long HalfPeriod = SkiaRenderer.CaretBlinkHalfPeriodMs;

        // Right after edit: caret visible (elapsed=0, 0 % Period < HalfPeriod).
        long elapsed0 = app.Clock.TickCount64 - input.LastEditTick;
        Assert.True((elapsed0 % Period) < HalfPeriod);

        // Advance into the hidden half of the blink cycle.
        app.Clock.Advance((HalfPeriod + 70) / 1000.0);
        long elapsedHidden = app.Clock.TickCount64 - input.LastEditTick;
        Assert.False((elapsedHidden % Period) < HalfPeriod);

        // Advance another half-period: back to visible half.
        app.Clock.Advance(HalfPeriod / 1000.0);
        long elapsedVisible = app.Clock.TickCount64 - input.LastEditTick;
        Assert.True((elapsedVisible % Period) < HalfPeriod);
    }

    [Fact]
    public void Element_Mutation_Mid_Frame_Does_Not_Crash()
    {
        using var app = new HeadlessApp(
            "<div id='host'><button id='btn'>Click</button></div>",
            BaseCss);

        var btn = app.Pipeline.FindById("btn")!;
        bool clicked = false;
        btn.On("Click", (_, _) =>
        {
            clicked = true;
            // Replace the root mid-frame: drop everything and install a fresh tree.
            var newRoot = new BoxElement("body");
            newRoot.AddChild(new TextElement("after"));
            app.App.Root = newRoot;
            app.Pipeline.Renderer.Paint(newRoot);
        });

        // Simulate a click on the button (down + up at its center).
        var box = btn.LayoutBox;
        float cx = box.X + box.Width / 2;
        float cy = box.Y + box.Height / 2;
        app.EnqueueInput(new MouseEvent { X = cx, Y = cy, Button = MouseButton.Left, Type = MouseEventType.ButtonDown });
        app.EnqueueInput(new MouseEvent { X = cx, Y = cy, Button = MouseButton.Left, Type = MouseEventType.ButtonUp });

        var ex = Record.Exception(() =>
        {
            app.Tick();
            // A second tick after mutation must not crash even though the old tree is gone.
            app.EnqueueInput(new MouseEvent { X = 10, Y = 10, Type = MouseEventType.Move });
            app.Tick();
        });

        Assert.Null(ex);
        Assert.True(clicked);
        Assert.Equal("body", app.App.Root.TagName);
    }

    [Fact]
    public void ScriptedInput_With_TimeAdvance_Produces_Deterministic_Snapshot()
    {
        var (digest1, pixels1) = RunScript();
        var (digest2, pixels2) = RunScript();

        Assert.Equal(digest1, digest2);
        Assert.Equal(pixels1, pixels2);

        static (string, byte[]) RunScript()
        {
            using var app = new HeadlessApp(
                "<div><input id='field' /></div>",
                BaseCss,
                width: 320,
                height: 120);

            var input = (InputElement)app.Pipeline.FindById("field")!;
            app.App.SetFocus(input);

            app.EnqueueInput(new TextInputEvent { Text = "h" });
            app.Tick(0.016);
            app.EnqueueInput(new TextInputEvent { Text = "i" });
            app.Tick(0.016);
            app.Clock.Advance(0.250);
            app.Render();

            return app.Snapshot();
        }
    }
}

[CollectionDefinition("Dst", DisableParallelization = true)]
public class DstCollection;
