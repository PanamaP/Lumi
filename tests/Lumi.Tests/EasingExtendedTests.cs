using Lumi.Core;
using Lumi.Core.Animation;

namespace Lumi.Tests;

public class EasingExtendedTests
{
    // --- EaseInOutCubic ---

    [Fact]
    public void EaseInOutCubic_Boundaries()
    {
        Assert.Equal(0f, Easing.EaseInOutCubic(0f));
        Assert.Equal(1f, Easing.EaseInOutCubic(1f), 4);
    }

    [Fact]
    public void EaseInOutCubic_Midpoint()
    {
        float result = Easing.EaseInOutCubic(0.5f);
        Assert.Equal(0.5f, result, 4);
    }

    [Fact]
    public void EaseInOutCubic_FirstHalf_IsAccelerating()
    {
        float q1 = Easing.EaseInOutCubic(0.25f);
        Assert.True(q1 < 0.25f, $"First half should ease-in (slower start), got {q1}");
    }

    [Fact]
    public void EaseInOutCubic_SecondHalf_IsDecelerating()
    {
        float q3 = Easing.EaseInOutCubic(0.75f);
        Assert.True(q3 > 0.75f, $"Second half should ease-out (faster progress), got {q3}");
    }

    // --- EaseInQuad ---

    [Fact]
    public void EaseInQuad_Boundaries()
    {
        Assert.Equal(0f, Easing.EaseInQuad(0f));
        Assert.Equal(1f, Easing.EaseInQuad(1f));
    }

    [Fact]
    public void EaseInQuad_MidValue()
    {
        float result = Easing.EaseInQuad(0.5f);
        Assert.Equal(0.25f, result, 4); // 0.5^2 = 0.25
    }

    // --- EaseOutQuad ---

    [Fact]
    public void EaseOutQuad_Boundaries()
    {
        Assert.Equal(0f, Easing.EaseOutQuad(0f));
        Assert.Equal(1f, Easing.EaseOutQuad(1f));
    }

    [Fact]
    public void EaseOutQuad_MidValue()
    {
        float result = Easing.EaseOutQuad(0.5f);
        Assert.Equal(0.75f, result, 4); // 0.5 * (2 - 0.5) = 0.75
    }

    // --- FromName ---

    [Theory]
    [InlineData("linear")]
    [InlineData("ease")]
    [InlineData("ease-in")]
    [InlineData("ease-out")]
    [InlineData("ease-in-out")]
    public void FromName_ReturnsNonNullFunction(string name)
    {
        var func = Easing.FromName(name);
        Assert.NotNull(func);
    }

    [Fact]
    public void FromName_Linear_ReturnsLinearFunction()
    {
        var func = Easing.FromName("linear");
        Assert.Equal(0.5f, func(0.5f));
    }

    [Fact]
    public void FromName_Ease_ReturnsEaseInOutCubic()
    {
        var func = Easing.FromName("ease");
        // "ease" maps to EaseInOutCubic
        Assert.Equal(Easing.EaseInOutCubic(0.3f), func(0.3f));
    }

    [Fact]
    public void FromName_EaseIn_ReturnsEaseInCubic()
    {
        var func = Easing.FromName("ease-in");
        Assert.Equal(Easing.EaseInCubic(0.4f), func(0.4f));
    }

    [Fact]
    public void FromName_EaseOut_ReturnsEaseOutCubic()
    {
        var func = Easing.FromName("ease-out");
        Assert.Equal(Easing.EaseOutCubic(0.6f), func(0.6f));
    }

    [Fact]
    public void FromName_EaseInOut_ReturnsEaseInOutCubic()
    {
        var func = Easing.FromName("ease-in-out");
        Assert.Equal(Easing.EaseInOutCubic(0.7f), func(0.7f));
    }

    [Fact]
    public void FromName_Unknown_DefaultsToEaseInOutCubic()
    {
        var func = Easing.FromName("unknown-easing");
        Assert.Equal(Easing.EaseInOutCubic(0.5f), func(0.5f));
    }

    [Fact]
    public void FromName_CaseInsensitive()
    {
        var funcLower = Easing.FromName("linear");
        var funcUpper = Easing.FromName("LINEAR");
        var funcMixed = Easing.FromName("Linear");

        Assert.Equal(funcLower(0.5f), funcUpper(0.5f));
        Assert.Equal(funcLower(0.5f), funcMixed(0.5f));
    }

    // --- Monotonicity ---

    [Theory]
    [InlineData(nameof(Easing.Linear))]
    [InlineData(nameof(Easing.EaseInCubic))]
    [InlineData(nameof(Easing.EaseOutCubic))]
    [InlineData(nameof(Easing.EaseInOutCubic))]
    [InlineData(nameof(Easing.EaseInQuad))]
    [InlineData(nameof(Easing.EaseOutQuad))]
    public void Easing_IsMonotonic(string easingName)
    {
        Func<float, float> func = easingName switch
        {
            nameof(Easing.Linear) => Easing.Linear,
            nameof(Easing.EaseInCubic) => Easing.EaseInCubic,
            nameof(Easing.EaseOutCubic) => Easing.EaseOutCubic,
            nameof(Easing.EaseInOutCubic) => Easing.EaseInOutCubic,
            nameof(Easing.EaseInQuad) => Easing.EaseInQuad,
            nameof(Easing.EaseOutQuad) => Easing.EaseOutQuad,
            _ => throw new ArgumentException()
        };

        float prev = func(0f);
        for (int i = 1; i <= 100; i++)
        {
            float t = i / 100f;
            float val = func(t);
            Assert.True(val >= prev - 0.001f, $"{easingName}({t}) = {val} should be >= {prev}");
            prev = val;
        }
    }
}
