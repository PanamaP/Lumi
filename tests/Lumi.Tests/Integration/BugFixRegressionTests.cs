using Lumi.Core;
using Lumi.Layout;
using Lumi.Styling;
using Xunit;

namespace Lumi.Tests.Integration;

/// <summary>
/// Regression tests for bugs fixed during development.
/// Each test targets a specific bug to prevent recurrence.
/// </summary>
[Collection("Integration")]
public class BugFixRegressionTests
{
    // ── CSS Percentage Width ──────────────────────────────────────────

    [Fact]
    public void PercentWidth_ResolvesToParentWidth()
    {
        var root = HtmlTemplateParser.Parse("<div class='parent'><div class='child'></div></div>");
        var css = CssParser.Parse(".parent { width: 400px; height: 200px; } .child { width: 50%; height: 100%; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var child = root.Children[0].Children[0]; // body > parent > child
        Assert.Equal(200f, child.LayoutBox.Width, 1f);  // 50% of 400
        Assert.Equal(200f, child.LayoutBox.Height, 1f);  // 100% of 200
    }

    [Fact]
    public void PercentWidth_100Percent_FillsParent()
    {
        var root = HtmlTemplateParser.Parse("<div class='outer'><div class='inner'></div></div>");
        var css = CssParser.Parse(".outer { width: 600px; } .inner { width: 100%; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var inner = root.Children[0].Children[0];
        Assert.Equal(600f, inner.LayoutBox.Width, 1f);
    }

    // ── CSS Unit Resolution (em, rem, pt) ─────────────────────────────

    [Fact]
    public void EmUnit_ResolvesRelativeToFontSize()
    {
        var root = HtmlTemplateParser.Parse("<div class='box'></div>");
        var css = CssParser.Parse(".box { font-size: 20px; width: 10em; height: 5em; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var box = root.Children[0]; // body > box
        // em is resolved against parent font-size context (default 16px since parent has no font-size)
        // But the element itself sets font-size: 20px
        // font-size: 20px resolves using PARENT context (16px default)
        // width: 10em resolves using PARENT font-size context (16px default)
        Assert.Equal(160f, box.LayoutBox.Width, 1f);  // 10 * 16px (parent context)
    }

    [Fact]
    public void RemUnit_ResolvesRelativeToRootFontSize()
    {
        var root = HtmlTemplateParser.Parse("<div class='box'></div>");
        var css = CssParser.Parse(".box { width: 10rem; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var box = root.Children[0];
        Assert.Equal(160f, box.LayoutBox.Width, 1f);  // 10 * 16px (root default)
    }

    [Fact]
    public void PtUnit_ConvertsToPx()
    {
        var root = HtmlTemplateParser.Parse("<div class='box'></div>");
        var css = CssParser.Parse(".box { width: 72pt; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var box = root.Children[0];
        // 72pt * (96/72) = 96px
        Assert.Equal(96f, box.LayoutBox.Width, 1f);
    }

    [Fact]
    public void PxUnit_PassesThrough()
    {
        var root = HtmlTemplateParser.Parse("<div class='box'></div>");
        var css = CssParser.Parse(".box { width: 250px; height: 100px; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var box = root.Children[0];
        Assert.Equal(250f, box.LayoutBox.Width, 1f);
        Assert.Equal(100f, box.LayoutBox.Height, 1f);
    }

    // ── Button Click via Application ──────────────────────────────────

    [Fact]
    public void ButtonClick_FiresViaBubbling()
    {
        var root = HtmlTemplateParser.Parse("<div><button id='btn'>Click</button></div>");
        var resolver = new StyleResolver();
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var btn = FindById(root, "btn")!;
        bool clicked = false;
        btn.On("Click", (_, _) => clicked = true);

        var app = new Application();
        app.Root = root;
        app.Start();

        float cx = btn.LayoutBox.X + btn.LayoutBox.Width / 2;
        float cy = btn.LayoutBox.Y + btn.LayoutBox.Height / 2;

        app.ProcessInput([
            new MouseEvent { Type = MouseEventType.ButtonDown, X = cx, Y = cy, Button = MouseButton.Left },
            new MouseEvent { Type = MouseEventType.ButtonUp, X = cx, Y = cy, Button = MouseButton.Left }
        ]);

        Assert.True(clicked, "Click event should bubble from text child to button parent");
    }

    [Fact]
    public void ButtonClick_CounterIncrements()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><button id='btn' style='width:100px;height:40px'>+</button></div>");
        var resolver = new StyleResolver();
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var btn = FindById(root, "btn")!;
        int count = 0;
        btn.On("Click", (_, _) => count++);

        // Dispatch Click directly (bypasses HitTest)
        EventDispatcher.Dispatch(
            new RoutedMouseEvent("Click") { X = 50, Y = 20, Button = MouseButton.Left },
            btn);

        Assert.Equal(1, count);

        // Now via Application
        var app = new Application();
        app.Root = root;
        app.Start();

        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonUp, X = 50, Y = 20, Button = MouseButton.Left }]);

        Assert.Equal(2, count);
    }

    // ── Resize Marks Dirty ────────────────────────────────────────────

    [Fact]
    public void WindowResize_MarksDirty()
    {
        var root = HtmlTemplateParser.Parse("<div>Hello</div>");
        var app = new Application();
        app.Root = root;
        app.Start();
        app.MarkClean();

        Assert.False(app.IsDirty);

        // Simulate resize via direct MarkDirty (as LumiApp does on WindowEvent.Resized)
        root.MarkDirty();
        Assert.True(app.IsDirty);
    }

    // ── Hot Reload Re-registers Handlers ──────────────────────────────

    [Fact]
    public void HotReload_HtmlReload_SetsFlag()
    {
        var window = new TestWindow();
        window.LoadTemplateString("<div>Hello</div>");

        var hotReload = new HotReload(window, null, null);
        Assert.False(hotReload.HtmlWasReloaded);
    }

    // ── Inspector Uses Absolute Coordinates ───────────────────────────

    [Fact]
    public void LayoutBox_StoresAbsoluteCoordinates()
    {
        var root = HtmlTemplateParser.Parse(
            "<div class='outer'><div class='inner'><div class='deep'></div></div></div>");
        var css = CssParser.Parse(
            ".outer { padding: 10px; } .inner { padding: 20px; } .deep { width: 50px; height: 50px; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        // body > outer > inner > deep
        var deep = root.Children[0].Children[0].Children[0];

        // deep should be at absolute position: outer padding (10) + inner padding (20) = 30
        Assert.True(deep.LayoutBox.X >= 30f, $"Deep element X should be >= 30 (absolute), got {deep.LayoutBox.X}");
        Assert.True(deep.LayoutBox.Y >= 30f, $"Deep element Y should be >= 30 (absolute), got {deep.LayoutBox.Y}");
    }

    // ── Click on Child Text — Tunnel Phase Fix ─────────────────────────

    [Fact]
    public void ClickOnChildText_FiresParentHandlerOnce()
    {
        // Bug: clicking text inside a button fired the handler twice
        // (once during Tunnel, once during Bubble). Fixed by skipping
        // standard handlers during Tunnel phase.
        var root = HtmlTemplateParser.Parse(
            "<div><button id='btn' style='width:100px;height:40px'>+</button></div>");
        var resolver = new StyleResolver();
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var btn = FindById(root, "btn")!;
        // The "+" text is a TextElement child of the button
        var textChild = btn.Children[0];
        Assert.IsType<TextElement>(textChild);

        int count = 0;
        btn.On("Click", (_, _) => count++);

        // Dispatch Click targeting the TEXT child (simulates clicking on the "+" text)
        EventDispatcher.Dispatch(
            new RoutedMouseEvent("Click") { X = 50, Y = 20, Button = MouseButton.Left },
            textChild);

        // Handler on button should fire exactly ONCE (during Bubble), not twice
        Assert.Equal(1, count);
    }

    [Fact]
    public void ClickOnChildText_ViaApplication_FiresOnce()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><button id='btn' style='width:100px;height:40px'>+</button></div>");
        var resolver = new StyleResolver();
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var btn = FindById(root, "btn")!;
        int count = 0;
        btn.On("Click", (_, _) => count++);

        var app = new Application();
        app.Root = root;
        app.Start();

        // Click center of button (hits TextElement child)
        float cx = btn.LayoutBox.X + btn.LayoutBox.Width / 2;
        float cy = btn.LayoutBox.Y + btn.LayoutBox.Height / 2;

        app.ProcessInput([
            new MouseEvent { Type = MouseEventType.ButtonDown, X = cx, Y = cy, Button = MouseButton.Left },
            new MouseEvent { Type = MouseEventType.ButtonUp, X = cx, Y = cy, Button = MouseButton.Left }
        ]);

        Assert.Equal(1, count);
    }

    // ── Overflow Scroll — Content Not Squished ───────────────────────

    [Fact]
    public void OverflowScroll_ChildrenMaintainSize()
    {
        // Bug: when window is too small, flex children get squished.
        // With overflow:scroll, children should keep their natural size.
        var root = HtmlTemplateParser.Parse(
            "<div class='container'><div class='child1'></div><div class='child2'></div></div>");
        var css = CssParser.Parse(
            ".container { width: 200px; height: 100px; overflow: scroll; display: flex; flex-direction: column; } " +
            ".child1 { width: 200px; height: 80px; flex-shrink: 0; } " +
            ".child2 { width: 200px; height: 80px; flex-shrink: 0; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var container = root.Children[0];
        var child1 = container.Children[0];
        var child2 = container.Children[1];

        // Children should maintain their 80px height even though container is only 100px
        Assert.Equal(80f, child1.LayoutBox.Height, 1f);
        Assert.Equal(80f, child2.LayoutBox.Height, 1f);

        // Container should report scroll dimensions > its own height
        Assert.True(container.ScrollHeight >= 160f,
            $"ScrollHeight should be >= 160 (2 × 80px children), got {container.ScrollHeight}");
    }

    // ── Gap Layout ─────────────────────────────────────────────────

    [Fact]
    public void Gap_AddsSpacingBetweenFlexItems()
    {
        var root = HtmlTemplateParser.Parse(
            "<div class='row'><div class='a'></div><div class='b'></div><div class='c'></div></div>");
        var css = CssParser.Parse(
            ".row { display: flex; flex-direction: row; gap: 20px; width: 400px; } " +
            ".a, .b, .c { width: 50px; height: 50px; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 400, 200);

        var row = root.Children[0];
        var a = row.Children[0];
        var b = row.Children[1];
        var c = row.Children[2];

        // Items should be spaced by 20px gap
        float expectedBStart = a.LayoutBox.X + a.LayoutBox.Width + 20;
        float expectedCStart = b.LayoutBox.X + b.LayoutBox.Width + 20;

        Assert.Equal(expectedBStart, b.LayoutBox.X, 1f);
        Assert.Equal(expectedCStart, c.LayoutBox.X, 1f);
    }

    // ── Border Style Parsing ──────────────────────────────────────

    [Fact]
    public void BorderStyle_ParsesDashedAndDotted()
    {
        var root = HtmlTemplateParser.Parse("<div class='dashed'></div><div class='dotted'></div>");
        var css = CssParser.Parse(
            ".dashed { border-style: dashed; } .dotted { border-style: dotted; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var dashed = root.Children[0];
        var dotted = root.Children[1];

        Assert.Equal(BorderStyle.Dashed, dashed.ComputedStyle.BorderStyle);
        Assert.Equal(BorderStyle.Dotted, dotted.ComputedStyle.BorderStyle);
    }

    // ── Per-Corner Border Radius ──────────────────────────────────

    [Fact]
    public void BorderRadius_PerCorner_ParsesShorthand()
    {
        var root = HtmlTemplateParser.Parse("<div class='pill'></div>");
        var css = CssParser.Parse(".pill { border-radius: 10px 20px 30px 40px; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var pill = root.Children[0];
        Assert.Equal(10f, pill.ComputedStyle.BorderCornerRadius.TopLeft, 1f);
        Assert.Equal(20f, pill.ComputedStyle.BorderCornerRadius.TopRight, 1f);
        Assert.Equal(30f, pill.ComputedStyle.BorderCornerRadius.BottomRight, 1f);
        Assert.Equal(40f, pill.ComputedStyle.BorderCornerRadius.BottomLeft, 1f);
        Assert.True(pill.ComputedStyle.BorderCornerRadius.HasPerCorner);
    }

    [Fact]
    public void BorderRadius_TwoValues_ParsesSymmetrically()
    {
        var root = HtmlTemplateParser.Parse("<div class='box'></div>");
        var css = CssParser.Parse(".box { border-radius: 10px 20px; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var box = root.Children[0];
        // TL=10, TR=20, BR=10, BL=20
        Assert.Equal(10f, box.ComputedStyle.BorderCornerRadius.TopLeft, 1f);
        Assert.Equal(20f, box.ComputedStyle.BorderCornerRadius.TopRight, 1f);
        Assert.Equal(10f, box.ComputedStyle.BorderCornerRadius.BottomRight, 1f);
        Assert.Equal(20f, box.ComputedStyle.BorderCornerRadius.BottomLeft, 1f);
    }

    // ── CSS Variables Pre-processor ───────────────────────────────

    [Fact]
    public void CssVariables_ResolveInProperties()
    {
        var root = HtmlTemplateParser.Parse("<div class='app'><div class='child'></div></div>");
        var css = CssParser.Parse(
            ".app { --accent: #FF0000; } .child { color: var(--accent); }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        // var(--accent) should have been pre-processed to #FF0000
        var child = root.Children[0].Children[0];
        Assert.True(child.ComputedStyle.Color.R > 200, "Red channel should be high from var(--accent)");
    }

    // ── Screenshot Export ────────────────────────────────────────────

    [Fact]
    public void HeadlessScreenshot_SavesPng()
    {
        var root = HtmlTemplateParser.Parse(
            "<div style='width:200px;height:100px;background-color:#FF0000'><span>Hello</span></div>");
        var resolver = new StyleResolver();
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 400, 300);

        var path = Path.Combine(Path.GetTempPath(), $"lumi_test_{Guid.NewGuid()}.png");
        try
        {
            bool saved = Lumi.Rendering.SkiaRenderer.RenderToPng(root, 400, 300, path);
            Assert.True(saved, "RenderToPng should succeed");
            Assert.True(File.Exists(path), "PNG file should exist");
            Assert.True(new FileInfo(path).Length > 100, "PNG file should have content");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static Element? FindById(Element el, string id)
    {
        if (el.Id == id) return el;
        foreach (var child in el.Children)
        {
            var found = FindById(child, id);
            if (found != null) return found;
        }
        return null;
    }

    private class TestWindow : Lumi.Window
    {
        public TestWindow() { Title = "Test"; Width = 800; Height = 600; }
    }

    // ── InlineStyle Survives StyleResolver Cascade ─────────────────────

    [Fact]
    public void InlineStyle_SurvivesStyleResolution()
    {
        var root = HtmlTemplateParser.Parse("<div class='box'></div>");
        var css = CssParser.Parse(".box { width: 100px; height: 50px; background-color: red; }");

        // Set inline style that overrides CSS
        var box = root.Children[0];
        box.InlineStyle = "width: 200px; background-color: blue";

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        // InlineStyle should win over CSS
        Assert.Equal(200f, box.ComputedStyle.Width);
        Assert.Equal(50f, box.ComputedStyle.Height); // CSS still applied for non-overridden props
    }

    [Fact]
    public void InlineStyle_ProgressBarWidth_SurvivesCascade()
    {
        var root = HtmlTemplateParser.Parse("<div class='track'><div class='fill'></div></div>");
        var css = CssParser.Parse(".track { width: 180px; height: 16px; } .fill { width: 0px; height: 16px; }");

        var fill = root.Children[0].Children[0];
        fill.InlineStyle = "width: 90px"; // simulates progress bar animation

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        Assert.Equal(90f, fill.LayoutBox.Width);
    }

    [Fact]
    public void ComponentStyles_InlineStyle_SurvivesCascade()
    {
        var root = HtmlTemplateParser.Parse("<div id='host'></div>");
        var css = CssParser.Parse("#host { display: flex; }");

        var host = root.Children[0];
        var btn = new Lumi.Core.Components.LumiButton { Text = "Test" };
        host.AddChild(btn.Root);

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        // Button's InlineStyle should have applied display: flex
        Assert.Equal(DisplayMode.Flex, btn.Root.ComputedStyle.Display);
    }

    [Fact]
    public void ScrollContainer_ContentOverflows_HasScrollableHeight()
    {
        var root = HtmlTemplateParser.Parse(
            "<div class='container'><div class='item'></div><div class='item'></div><div class='item'></div></div>");
        var css = CssParser.Parse(
            ".container { overflow: scroll; max-height: 50px; width: 200px; } .item { height: 40px; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(css);
        resolver.ResolveStyles(root, new PseudoClassState(false, false, false));

        var layout = new YogaLayoutEngine();
        layout.CalculateLayout(root, 800, 600);

        var container = root.Children[0];
        // 3 items × 40px = 120px content > 50px max-height → should be scrollable
        Assert.True(container.ScrollHeight > container.LayoutBox.Height,
            $"ScrollHeight ({container.ScrollHeight}) should exceed LayoutBox.Height ({container.LayoutBox.Height})");
    }

    // ── Event Name Case Insensitivity ─────────────────────────────────

    [Fact]
    public void EventHandler_CaseInsensitive_BothCasesFireSameHandler()
    {
        var element = new BoxElement("button");
        int fireCount = 0;

        // Register with lowercase (as components do)
        element.On("click", (_, _) => fireCount++);

        // Dispatch with PascalCase (as the framework does)
        EventDispatcher.Dispatch(new RoutedMouseEvent("Click"), element);

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void EventHandler_CaseInsensitive_MouseEvents()
    {
        var element = new BoxElement("div");
        int downCount = 0;
        int moveCount = 0;
        int upCount = 0;

        element.On("mousedown", (_, _) => downCount++);
        element.On("mousemove", (_, _) => moveCount++);
        element.On("mouseup", (_, _) => upCount++);

        EventDispatcher.Dispatch(new RoutedMouseEvent("MouseDown"), element);
        EventDispatcher.Dispatch(new RoutedMouseEvent("MouseMove"), element);
        EventDispatcher.Dispatch(new RoutedMouseEvent("MouseUp"), element);

        Assert.Equal(1, downCount);
        Assert.Equal(1, moveCount);
        Assert.Equal(1, upCount);
    }

    // ── Overflow Longhand Expansion ───────────────────────────────────

    [Fact]
    public void OverflowLonghands_AppliedByPropertyApplier()
    {
        var style = new ComputedStyle();

        // PropertyApplier supports overflow longhands (overflow-x/overflow-y)
        PropertyApplier.Apply(style, "overflow-x", "hidden");
        Assert.Equal(Overflow.Hidden, style.Overflow);

        style.Overflow = Overflow.Visible; // reset
        PropertyApplier.Apply(style, "overflow-y", "scroll");
        Assert.Equal(Overflow.Scroll, style.Overflow);
    }

    // ── Text Decoration Longhand Expansion ────────────────────────────

    [Fact]
    public void TextDecorationLine_AppliedByPropertyApplier()
    {
        var style = new ComputedStyle();

        // PropertyApplier supports text-decoration-line longhand
        PropertyApplier.Apply(style, "text-decoration-line", "underline");
        Assert.Equal(TextDecoration.Underline, style.TextDecoration);

        PropertyApplier.Apply(style, "text-decoration-line", "line-through");
        Assert.Equal(TextDecoration.LineThrough, style.TextDecoration);
    }

    // ── Hot Reload WakeUp Callback (regression for idle-loop fix) ─────

    [Fact]
    public void HotReload_QueueCssReload_InvokesWakeUp()
    {
        var window = new TestWindow();
        window.LoadTemplateString("<div class='app'>Hello</div>");

        var tmpCss = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpCss, ".app { background-color: red; }");

            bool wakeUpCalled = false;
            var hotReload = new HotReload(window, null, tmpCss, wakeUp: () => wakeUpCalled = true);

            hotReload.QueueCssReload();

            Assert.True(wakeUpCalled, "WakeUp callback should be invoked when CSS reload is queued");
            Assert.True(hotReload.HasPendingChanges, "Should have pending changes after QueueCssReload");
        }
        finally
        {
            File.Delete(tmpCss);
        }
    }

    [Fact]
    public void HotReload_QueueHtmlReload_InvokesWakeUp()
    {
        var window = new TestWindow();
        window.LoadTemplateString("<div>Original</div>");
        window.LoadStyleSheetString(".app { color: red; }");

        var tmpHtml = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpHtml, "<div class='app'>Updated</div>");

            bool wakeUpCalled = false;
            var hotReload = new HotReload(window, tmpHtml, null, wakeUp: () => wakeUpCalled = true);

            hotReload.QueueHtmlReload();

            Assert.True(wakeUpCalled, "WakeUp callback should be invoked when HTML reload is queued");
            Assert.True(hotReload.HasPendingChanges, "Should have pending changes after QueueHtmlReload");
        }
        finally
        {
            File.Delete(tmpHtml);
        }
    }

    [Fact]
    public void HotReload_NoWakeUpCallback_DoesNotCrash()
    {
        var window = new TestWindow();
        window.LoadTemplateString("<div class='app'>Hello</div>");

        var tmpCss = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpCss, ".app { background-color: blue; }");

            // No wakeUp callback (backward compat — default null)
            var hotReload = new HotReload(window, null, tmpCss);

            var ex = Record.Exception(() => hotReload.QueueCssReload());

            Assert.Null(ex);
            Assert.True(hotReload.HasPendingChanges, "Should have pending changes even without wakeUp callback");
        }
        finally
        {
            File.Delete(tmpCss);
        }
    }
}
