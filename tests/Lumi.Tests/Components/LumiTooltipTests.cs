using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets the surviving mutants in <see cref="LumiTooltip"/>: position-fitting
/// branches (right / left / below / above), Show/Hide/Dispose lifecycle and
/// the Text setter. Builds a real element tree with explicit LayoutBoxes so the
/// fitting branches actually fire.
/// </summary>
public class LumiTooltipTests
{
    private static (BoxElement root, BoxElement target) BuildTree(
        float viewW, float viewH,
        float tx, float ty, float tw, float th)
    {
        var root = new BoxElement("body");
        root.LayoutBox = new LayoutBox(0, 0, viewW, viewH);
        var target = new BoxElement("div");
        target.LayoutBox = new LayoutBox(tx, ty, tw, th);
        root.AddChild(target);
        return (root, target);
    }

    private static void Show(LumiTooltip tooltip, Element target) =>
        EventDispatcher.Dispatch(new RoutedEvent("mouseenter"), target);

    private static void Hide(LumiTooltip tooltip, Element target) =>
        EventDispatcher.Dispatch(new RoutedEvent("mouseleave"), target);

    [Fact]
    public void Show_PositionsRightOfTarget_WhenItFits()
    {
        // Target near top-left; tooltip easily fits to the right.
        var (root, target) = BuildTree(800, 600, 50, 50, 100, 30);
        var tooltip = LumiTooltip.Attach(target, "Hi");
        Show(tooltip, target);

        Assert.Same(root, tooltip.Root.Parent);
        Assert.Contains("left: 154px", tooltip.Root.InlineStyle ?? ""); // target.Right=150, +4
        Assert.Contains("top: 50px", tooltip.Root.InlineStyle ?? "");
    }

    [Fact]
    public void Show_PositionsLeftOfTarget_WhenRightOverflows()
    {
        // Target hugs the right edge; right-side won't fit, but left does.
        var (root, target) = BuildTree(200, 600, 180, 50, 18, 30);
        var tooltip = LumiTooltip.Attach(target, "Hi"); // text length 2 -> ~30px estimate
        Show(tooltip, target);

        Assert.Same(root, tooltip.Root.Parent);
        var style = tooltip.Root.InlineStyle ?? "";
        // 180 - 4 - 30 (estimated tooltipW) = 146
        Assert.Contains("left: 146px", style);
        Assert.Contains("top: 50px", style);
    }

    [Fact]
    public void Show_PositionsBelowTarget_WhenLeftAndRightOverflow()
    {
        // Narrow viewport, target spans full width: neither side fits, but below does.
        var (root, target) = BuildTree(40, 600, 0, 50, 40, 30);
        var tooltip = LumiTooltip.Attach(target, "ABCDEFGH"); // length 8 * 7 + 16 = 72 estimated
        Show(tooltip, target);

        var style = tooltip.Root.InlineStyle ?? "";
        Assert.Contains("left: 0px", style);
        Assert.Contains("top: 84px", style); // target.Bottom=80 + 4
    }

    [Fact]
    public void Show_FallsBackAbove_WhenNothingFits()
    {
        // Tiny viewport so right/left/below all overflow.
        var (root, target) = BuildTree(40, 90, 0, 60, 40, 30);
        var tooltip = LumiTooltip.Attach(target, "LONGGGGGG"); // estimated wider than 40
        Show(tooltip, target);

        var style = tooltip.Root.InlineStyle ?? "";
        Assert.Contains("left: 0px", style);
        // y = max(0, target.Y - 4 - 24) = max(0, 60-4-24) = 32
        Assert.Contains("top: 32px", style);
    }

    [Fact]
    public void Show_AboveFallback_ClampsAtZero_WhenTargetAtTop()
    {
        // Target at top, tiny viewport so right/left/below all overflow,
        // forcing the "above" fallback. y = max(0, 5 - 4 - 24) = 0.
        var (root, target) = BuildTree(40, 35, 0, 5, 40, 30); // target.Bottom=35; +4+24=63 > 35
        var tooltip = LumiTooltip.Attach(target, "LONGGGGGG");
        Show(tooltip, target);

        Assert.Contains("top: 0px", tooltip.Root.InlineStyle ?? "");
    }

    [Fact]
    public void Show_AddsTooltipToRoot_AndMarksRootDirty()
    {
        var (root, target) = BuildTree(800, 600, 10, 10, 50, 30);
        root.IsDirty = false;
        var tooltip = LumiTooltip.Attach(target, "T");
        Show(tooltip, target);

        Assert.Same(root, tooltip.Root.Parent);
        Assert.True(root.IsDirty);
    }

    [Fact]
    public void Show_TwiceWithoutHide_KeepsSingleInstanceUnderRoot()
    {
        var (root, target) = BuildTree(800, 600, 10, 10, 50, 30);
        var tooltip = LumiTooltip.Attach(target, "T");
        Show(tooltip, target);
        Show(tooltip, target);

        // Tooltip is parented by root once; second show is a no-op for parenting.
        Assert.Single(root.Children.Where(c => ReferenceEquals(c, tooltip.Root)));
    }

    [Fact]
    public void Hide_DetachesFromTree()
    {
        var (root, target) = BuildTree(800, 600, 10, 10, 50, 30);
        var tooltip = LumiTooltip.Attach(target, "T");
        Show(tooltip, target);
        Assert.NotNull(tooltip.Root.Parent);

        Hide(tooltip, target);
        Assert.Null(tooltip.Root.Parent);
    }

    [Fact]
    public void Dispose_WhenNeverAttached_DoesNotThrow()
    {
        // LumiTooltip created via constructor (not Attach) has _target == null.
        // Dispose must take the guarded branch (`if (_target != null)`) without throwing
        // and must still null out the handlers / target idempotently.
        var tooltip = new LumiTooltip { Text = "x" };

        var ex = Record.Exception(() => tooltip.Dispose());
        Assert.Null(ex);

        // Calling Dispose a second time on the now-cleared instance must also be safe.
        var ex2 = Record.Exception(() => tooltip.Dispose());
        Assert.Null(ex2);

        Assert.Equal("x", tooltip.Text);
    }

    [Fact]
    public void TextSetter_UpdatesInternalTextElement()
    {
        var tooltip = new LumiTooltip { Text = "first" };
        Assert.Equal("first", tooltip.Text);
        tooltip.Text = "second";
        Assert.Equal("second", tooltip.Text);
        // Internal text element is the only TextElement child of the container.
        var te = tooltip.Root.Children.OfType<TextElement>().Single();
        Assert.Equal("second", te.Text);
    }

    [Fact]
    public void Dispose_DetachesHandlersAndStopsResponding()
    {
        var (root, target) = BuildTree(800, 600, 10, 10, 50, 30);
        var tooltip = LumiTooltip.Attach(target, "T");
        Show(tooltip, target);
        Assert.NotNull(tooltip.Root.Parent);

        tooltip.Dispose();
        // After dispose, container removed from tree.
        Assert.Null(tooltip.Root.Parent);

        // Subsequent enter/leave should not re-attach
        EventDispatcher.Dispatch(new RoutedEvent("mouseenter"), target);
        Assert.Null(tooltip.Root.Parent);
    }

    [Fact]
    public void Dispose_OnUnshownTooltip_DoesNotThrow()
    {
        var target = new BoxElement("div");
        var tooltip = LumiTooltip.Attach(target, "T");
        var ex = Record.Exception(() => tooltip.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Show_ReusesContainerLayoutBox_WhenAlreadyMeasured()
    {
        var (root, target) = BuildTree(800, 600, 10, 10, 50, 30);
        var tooltip = LumiTooltip.Attach(target, "abcdefghij"); // 10 * 7 + 16 = 86 (estimate)
        // Pre-set a measured tooltip width, smaller than the estimate.
        tooltip.Root.LayoutBox = new LayoutBox(0, 0, 30, 18);
        Show(tooltip, target);

        // Right-of-target: target.Right=60 + 4 = 64; with measured tooltipW=30 fits in 800.
        Assert.Contains("left: 64px", tooltip.Root.InlineStyle ?? "");
    }
}
