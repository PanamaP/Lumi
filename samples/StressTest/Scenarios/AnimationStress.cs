using Lumi.Core;
using Lumi.Core.Animation;

namespace StressTest.Scenarios;

/// <summary>
/// Runs 200 concurrent opacity animations + 50 position tweens.
/// Measures: animation tick time, paint time.
/// </summary>
public class AnimationStress : IStressScenario
{
    public string Name => "Animation Stress";
    public string Description => "200 opacity tweens + 50 position tweens — stress animation engine";

    private readonly List<Element> _boxes = [];
    private readonly Random _rng = new(42);

    public void Setup(StressWindow window, Element container)
    {
        // Grid container
        var grid = new BoxElement("div");
        grid.ComputedStyle.Display = DisplayMode.Flex;
        grid.ComputedStyle.FlexDirection = FlexDirection.Row;
        grid.ComputedStyle.FlexWrap = FlexWrap.Wrap;
        grid.ComputedStyle.Padding = new EdgeValues(8, 8, 8, 8);

        // Create 200 small colored boxes
        for (int i = 0; i < 200; i++)
        {
            var box = new BoxElement("div");
            box.ComputedStyle.Width = 32;
            box.ComputedStyle.Height = 32;
            box.ComputedStyle.Margin = new EdgeValues(4, 4, 4, 4);
            box.ComputedStyle.BorderRadius = 4;
            box.ComputedStyle.BackgroundColor = RandomColor();

            grid.AddChild(box);
            _boxes.Add(box);

            // Staggered opacity animation (0 → 1) repeating
            float delay = (i % 20) * 0.05f;
            box.Animate()
                .Property("opacity", 0f, 1f)
                .Duration(1.0f)
                .Easing(Easing.EaseInOutCubic)
                .Delay(delay)
                .OnComplete(() => RestartOpacityAnimation(box))
                .Start();
        }

        // 50 position tweens (margin-left oscillation)
        for (int i = 0; i < 50; i++)
        {
            var box = _boxes[i];
            float targetMargin = 20 + _rng.Next(40);
            box.Animate()
                .Property("margin-left", 4f, targetMargin)
                .Duration(1.5f)
                .Easing(Easing.EaseOutCubic)
                .Delay(i * 0.03f)
                .OnComplete(() => RestartPositionAnimation(box, targetMargin))
                .Start();
        }

        container.AddChild(grid);
    }

    public void Update(int frameNumber)
    {
        // Animations are driven by the TweenEngine automatically.
        // No manual per-frame work needed — the engine ticks them.
    }

    private void RestartOpacityAnimation(Element box)
    {
        // Reverse: 1 → 0, then restart
        box.Animate()
            .Property("opacity", box.ComputedStyle.Opacity, box.ComputedStyle.Opacity < 0.5f ? 1f : 0f)
            .Duration(1.0f)
            .Easing(Easing.EaseInOutCubic)
            .OnComplete(() => RestartOpacityAnimation(box))
            .Start();
    }

    private void RestartPositionAnimation(Element box, float prevTarget)
    {
        float newTarget = prevTarget > 20 ? 4f : 20f + _rng.Next(40);
        box.Animate()
            .Property("margin-left", prevTarget, newTarget)
            .Duration(1.5f)
            .Easing(Easing.EaseOutCubic)
            .OnComplete(() => RestartPositionAnimation(box, newTarget))
            .Start();
    }

    private Color RandomColor()
    {
        byte r = (byte)_rng.Next(100, 256);
        byte g = (byte)_rng.Next(100, 256);
        byte b = (byte)_rng.Next(100, 256);
        return new Color(r, g, b, 255);
    }
}
