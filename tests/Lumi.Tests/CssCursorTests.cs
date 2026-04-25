using Lumi.Core;
using Lumi.Platform;
using Lumi.Styling;
using SDL;

namespace Lumi.Tests;

/// <summary>
/// Tests for the CSS cursor property: parsing, default value, style resolution,
/// and the SDL3 cursor mapping used by Sdl3Window.
/// </summary>
public class CssCursorTests
{
    // ── ComputedStyle defaults ─────────────────────────────────────────

    [Fact]
    public void ComputedStyle_Cursor_DefaultIsDefault()
    {
        var style = new ComputedStyle();
        Assert.Equal("default", style.Cursor);
    }

    // ── PropertyApplier cursor parsing ────────────────────────────────

    [Theory]
    [InlineData("pointer", "pointer")]
    [InlineData("text", "text")]
    [InlineData("crosshair", "crosshair")]
    [InlineData("move", "move")]
    [InlineData("not-allowed", "not-allowed")]
    [InlineData("wait", "wait")]
    [InlineData("progress", "progress")]
    [InlineData("grab", "grab")]
    [InlineData("grabbing", "grabbing")]
    [InlineData("default", "default")]
    [InlineData("ew-resize", "ew-resize")]
    [InlineData("ns-resize", "ns-resize")]
    [InlineData("col-resize", "col-resize")]
    public void PropertyApplier_Cursor_SetsValue(string cssValue, string expected)
    {
        var style = new ComputedStyle();
        PropertyApplier.Apply(style, "cursor", cssValue);
        Assert.Equal(expected, style.Cursor);
    }

    // ── StyleResolver integration ──────────────────────────────────────

    [Fact]
    public void StyleResolver_AppliesCursorFromClass()
    {
        var sheet = CssParser.Parse(".clickable { cursor: pointer; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        var child = new BoxElement("button");
        child.Classes.Add("clickable");
        root.AddChild(child);

        resolver.ResolveStyles(root);

        Assert.Equal("pointer", child.ComputedStyle.Cursor);
    }

    [Fact]
    public void StyleResolver_CursorInherited_ChildGetsParentCursor()
    {
        var sheet = CssParser.Parse(".parent { cursor: pointer; }");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        root.Classes.Add("parent");
        var child = new BoxElement("div");
        root.AddChild(child);

        resolver.ResolveStyles(root);

        Assert.Equal("pointer", root.ComputedStyle.Cursor);
        Assert.Equal("pointer", child.ComputedStyle.Cursor);
    }

    [Fact]
    public void StyleResolver_CursorInherited_ChildOverridesParent()
    {
        var sheet = CssParser.Parse(@"
            .parent { cursor: pointer; }
            .child  { cursor: default; }
        ");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        root.Classes.Add("parent");
        var child = new BoxElement("div");
        child.Classes.Add("child");
        root.AddChild(child);

        resolver.ResolveStyles(root);

        Assert.Equal("pointer", root.ComputedStyle.Cursor);
        Assert.Equal("default", child.ComputedStyle.Cursor);
    }

    [Fact]
    public void StyleResolver_MultipleCursorValues_LastWins()
    {
        var sheet = CssParser.Parse(@"
            .a { cursor: pointer; }
            .b { cursor: text; }
        ");

        var resolver = new StyleResolver();
        resolver.AddStyleSheet(sheet);

        var root = new BoxElement("div");
        var child = new BoxElement("div");
        child.Classes.Add("a");
        child.Classes.Add("b");
        root.AddChild(child);

        resolver.ResolveStyles(root);

        // Both selectors have equal specificity, last rule wins
        Assert.Equal("text", child.ComputedStyle.Cursor);
    }

    // ── Sdl3Window.MapCssCursorToSdl mapping ──────────────────────────

    [Theory]
    [InlineData("default", SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT)]
    [InlineData("pointer", SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER)]
    [InlineData("text", SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT)]
    [InlineData("crosshair", SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR)]
    [InlineData("move", SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE)]
    [InlineData("wait", SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT)]
    [InlineData("progress", SDL_SystemCursor.SDL_SYSTEM_CURSOR_PROGRESS)]
    [InlineData("not-allowed", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED)]
    [InlineData("no-drop", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED)]
    [InlineData("ew-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE)]
    [InlineData("ns-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE)]
    [InlineData("nwse-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE)]
    [InlineData("nesw-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE)]
    [InlineData("n-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_N_RESIZE)]
    [InlineData("e-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_E_RESIZE)]
    [InlineData("s-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_S_RESIZE)]
    [InlineData("w-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_W_RESIZE)]
    [InlineData("ne-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NE_RESIZE)]
    [InlineData("nw-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NW_RESIZE)]
    [InlineData("se-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_SE_RESIZE)]
    [InlineData("sw-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_SW_RESIZE)]
    [InlineData("col-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE)]
    [InlineData("row-resize", SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE)]
    [InlineData("grab", SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE)]
    [InlineData("grabbing", SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE)]
    [InlineData("all-scroll", SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE)]
    public void MapCssCursorToSdl_MapsCorrectly(string cssValue, SDL_SystemCursor expected)
    {
        var result = Sdl3Window.MapCssCursorToSdl(cssValue);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("")]
    [InlineData("unknown-cursor")]
    [InlineData("help")]
    public void MapCssCursorToSdl_UnknownValues_FallToDefault(string cssValue)
    {
        var result = Sdl3Window.MapCssCursorToSdl(cssValue);
        Assert.Equal(SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT, result);
    }
}
