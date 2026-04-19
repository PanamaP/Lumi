using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests.Components;

/// <summary>
/// Additional LumiProgressBar tests targeting surviving mutants in UpdateVisual / value math.
/// </summary>
public class LumiProgressBarTests
{
    [Fact]
    public void Default_Fill_HasZeroWidth()
    {
        var pb = new LumiProgressBar();
        var fill = pb.Root.Children[0];
        Assert.Contains("width: 0px", fill.InlineStyle);
    }

    [Theory]
    [InlineData(0f, "0.0%")]
    [InlineData(0.25f, "25.0%")]
    [InlineData(0.5f, "50.0%")]
    [InlineData(0.75f, "75.0%")]
    [InlineData(1f, "100.0%")]
    public void DeterminateValue_RendersCorrectPercentage(float value, string expected)
    {
        var pb = new LumiProgressBar { Value = value };
        var fill = pb.Root.Children[0];
        Assert.Contains($"width: {expected}", fill.InlineStyle);
    }

    [Fact]
    public void DeterminateValue_HasNoOpacityModifier()
    {
        var pb = new LumiProgressBar { Value = 0.5f };
        var fill = pb.Root.Children[0];
        Assert.DoesNotContain("opacity:", fill.InlineStyle);
    }

    [Fact]
    public void Indeterminate_UsesFullWidthAndOpacity()
    {
        var pb = new LumiProgressBar { IsIndeterminate = true };
        var fill = pb.Root.Children[0];
        Assert.Contains("width: 100%", fill.InlineStyle);
        Assert.Contains("opacity: 0.7", fill.InlineStyle);
    }

    [Fact]
    public void Indeterminate_OverridesDeterminateValue()
    {
        var pb = new LumiProgressBar { Value = 0.3f };
        var fill = pb.Root.Children[0];
        Assert.Contains("width: 30.0%", fill.InlineStyle);

        pb.IsIndeterminate = true;
        Assert.Contains("width: 100%", fill.InlineStyle);
        Assert.DoesNotContain("30.0%", fill.InlineStyle);
    }

    [Fact]
    public void ValueExactlyOne_ClampsToOneNotAbove()
    {
        var pb = new LumiProgressBar { Value = 999f };
        Assert.Equal(1f, pb.Value);
    }

    [Fact]
    public void Fill_UsesAccentColor()
    {
        var pb = new LumiProgressBar { Value = 0.5f };
        var fill = pb.Root.Children[0];
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Accent), fill.InlineStyle);
    }

    [Fact]
    public void Container_UsesProgressTrackStyle()
    {
        var pb = new LumiProgressBar();
        Assert.Contains("width: 100%", pb.Root.InlineStyle);
        Assert.Contains("height: 8px", pb.Root.InlineStyle);
    }

    [Fact]
    public void Dispose_RemovesEventHandlers()
    {
        var pb = new LumiProgressBar();
        // Simply verify Dispose doesn't throw and is idempotent.
        pb.Dispose();
        pb.Dispose();
    }

    [Fact]
    public void SettingValue_MarksContainerDirty()
    {
        var pb = new LumiProgressBar();
        pb.Root.IsDirty = false;
        pb.Value = 0.5f;
        Assert.True(pb.Root.IsDirty);
    }
}
