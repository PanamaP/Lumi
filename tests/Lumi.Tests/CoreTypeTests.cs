using Lumi.Core;

namespace Lumi.Tests;

public class ColorTests
{
    [Fact]
    public void FromHex_3Digit()
    {
        var color = Color.FromHex("#F00");
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromHex_6Digit()
    {
        var color = Color.FromHex("#3B82F6");
        Assert.Equal(0x3B, color.R);
        Assert.Equal(0x82, color.G);
        Assert.Equal(0xF6, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromHex_8Digit_WithAlpha()
    {
        var color = Color.FromHex("#FF000080");
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(128, color.A);
    }

    [Fact]
    public void FromHex_WithoutHash()
    {
        var color = Color.FromHex("3B82F6");
        Assert.Equal(0x3B, color.R);
        Assert.Equal(0x82, color.G);
        Assert.Equal(0xF6, color.B);
    }

    [Fact]
    public void LayoutBox_Contains()
    {
        var box = new LayoutBox(10, 20, 100, 50);
        Assert.True(box.Contains(50, 40));
        Assert.False(box.Contains(5, 40));
        Assert.True(box.Contains(10, 20)); // edge
        Assert.True(box.Contains(110, 70)); // far edge
        Assert.False(box.Contains(111, 40)); // just outside
    }

    [Fact]
    public void EdgeValues_Shorthand_All()
    {
        var edge = new EdgeValues(10);
        Assert.Equal(10, edge.Top);
        Assert.Equal(10, edge.Right);
        Assert.Equal(10, edge.Bottom);
        Assert.Equal(10, edge.Left);
    }

    [Fact]
    public void EdgeValues_Shorthand_VerticalHorizontal()
    {
        var edge = new EdgeValues(10, 20);
        Assert.Equal(10, edge.Top);
        Assert.Equal(20, edge.Right);
        Assert.Equal(10, edge.Bottom);
        Assert.Equal(20, edge.Left);
    }
}
