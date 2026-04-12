using Lumi;
using Lumi.Core;
using Lumi.Rendering;
using SkiaSharp;
using System.Xml.Linq;

namespace Lumi.Tests;

public class ToolingTests : IDisposable
{
    private readonly string _tempDir;

    public ToolingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lumi_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // ─── HotReload Tests ───

    [Fact]
    public void HotReload_Constructor_DoesNotCrash_WithValidPaths()
    {
        var htmlPath = Path.Combine(_tempDir, "test.html");
        var cssPath = Path.Combine(_tempDir, "test.css");
        File.WriteAllText(htmlPath, "<div>hello</div>");
        File.WriteAllText(cssPath, "div { color: red; }");

        var window = new Window();
        using var hotReload = new HotReload(window, htmlPath, cssPath);
        // Should not throw
    }

    [Fact]
    public void HotReload_Constructor_HandlesNullPaths()
    {
        var window = new Window();
        using var hotReload = new HotReload(window, null, null);
        hotReload.Start();
        Assert.False(hotReload.HasPendingChanges);
        hotReload.Stop();
    }

    [Fact]
    public async Task HotReload_QueuesCssReload_WhenFileChanges()
    {
        var cssPath = Path.Combine(_tempDir, "style.css");
        File.WriteAllText(cssPath, "body { color: black; }");

        var window = new Window();
        window.LoadTemplateString("<div>test</div>");
        using var hotReload = new HotReload(window, null, cssPath);
        hotReload.Start();

        // Modify the CSS file
        File.WriteAllText(cssPath, "body { color: blue; }");

        // Wait for debounce (200ms) + FileSystemWatcher latency
        await Task.Delay(1000);

        Assert.True(hotReload.HasPendingChanges);
        hotReload.ApplyPendingChanges();
        Assert.False(hotReload.HasPendingChanges);
    }

    [Fact]
    public async Task HotReload_Debounces_RapidChanges()
    {
        var cssPath = Path.Combine(_tempDir, "rapid.css");
        File.WriteAllText(cssPath, "body { color: black; }");

        var window = new Window();
        window.LoadTemplateString("<div>test</div>");
        using var hotReload = new HotReload(window, null, cssPath);
        hotReload.Start();

        // Fire several rapid changes
        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(cssPath, $"body {{ color: #{i}{i}{i}; }}");
            await Task.Delay(50);
        }

        // Wait for debounce to settle
        await Task.Delay(500);

        // Should have coalesced into a single pending action (debounce)
        int applyCount = 0;
        while (hotReload.HasPendingChanges)
        {
            hotReload.ApplyPendingChanges();
            applyCount++;
        }

        // Only 1 apply pass needed — debounce collapsed rapid changes
        Assert.Equal(1, applyCount);
    }

    // ─── Inspector Tests ───

    [Fact]
    public void Inspector_Toggle_FlipsIsEnabled()
    {
        var inspector = new Inspector();
        Assert.False(inspector.IsEnabled);

        inspector.Toggle();
        Assert.True(inspector.IsEnabled);

        inspector.Toggle();
        Assert.False(inspector.IsEnabled);
    }

    [Fact]
    public void Inspector_Draw_DoesNotCrash_WithNullHoveredElement()
    {
        var inspector = new Inspector();
        inspector.Toggle(); // Enable

        var root = new BoxElement("body");
        root.LayoutBox = new LayoutBox(0, 0, 800, 600);

        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);

        inspector.Draw(canvas, root, null, 800, 600);
        // Should not throw
    }

    [Fact]
    public void Inspector_Draw_DoesNotCrash_WithValidElementTree()
    {
        var inspector = new Inspector();
        inspector.Toggle();

        var root = new BoxElement("body");
        root.LayoutBox = new LayoutBox(0, 0, 800, 600);

        var child = new BoxElement("div");
        child.Id = "container";
        child.Classes = ["main", "active"];
        child.LayoutBox = new LayoutBox(10, 10, 200, 100);
        child.ComputedStyle.Margin = new EdgeValues(5, 5, 5, 5);
        child.ComputedStyle.Padding = new EdgeValues(10, 10, 10, 10);
        root.AddChild(child);

        var text = new TextElement("Hello");
        text.LayoutBox = new LayoutBox(20, 20, 50, 16);
        child.AddChild(text);

        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);

        // Draw with child as hovered element
        inspector.Draw(canvas, root, child, 800, 600);
        // Should not throw
    }

    [Fact]
    public void Inspector_Draw_DoesNothingWhenDisabled()
    {
        var inspector = new Inspector();
        Assert.False(inspector.IsEnabled);

        var root = new BoxElement("body");
        root.LayoutBox = new LayoutBox(0, 0, 800, 600);

        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);

        // Clear to a known color
        canvas.Clear(SKColors.Red);

        inspector.Draw(canvas, root, null, 800, 600);

        // Canvas should still be the same since inspector is disabled
        var pixel = bitmap.GetPixel(400, 300);
        Assert.Equal(SKColors.Red, pixel);
    }

    // ─── NuGet Packaging Tests ───

    [Fact]
    public void NuGet_UmbrellaProject_IsPackable()
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, "src", "Lumi", "Lumi.csproj");
        Assert.True(File.Exists(path), $"Project file not found: {path}");

        var doc = XDocument.Load(path);
        var ns = doc.Root!.Name.Namespace;
        var isPackable = doc.Descendants(ns + "IsPackable").FirstOrDefault();
        Assert.True(isPackable != null && isPackable.Value.Equals("true", StringComparison.OrdinalIgnoreCase),
            "src/Lumi/Lumi.csproj should have <IsPackable>true</IsPackable>");
    }

    [Fact]
    public void NuGet_SubProjects_ArePackable()
    {
        var repoRoot = FindRepoRoot();
        var subProjects = new[]
        {
            "src/Lumi.Core/Lumi.Core.csproj",
            "src/Lumi.Rendering/Lumi.Rendering.csproj",
            "src/Lumi.Styling/Lumi.Styling.csproj",
            "src/Lumi.Layout/Lumi.Layout.csproj",
            "src/Lumi.Input/Lumi.Input.csproj",
            "src/Lumi.Platform/Lumi.Platform.csproj",
            "src/Lumi.Text/Lumi.Text.csproj",
        };

        foreach (var project in subProjects)
        {
            var path = Path.Combine(repoRoot, project.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"Project file not found: {path}");

            var doc = XDocument.Load(path);
            var ns = doc.Root!.Name.Namespace;
            var isPackable = doc.Descendants(ns + "IsPackable").FirstOrDefault();
            Assert.True(isPackable != null && isPackable.Value.Equals("true", StringComparison.OrdinalIgnoreCase),
                $"{project} should have <IsPackable>true</IsPackable> — all library projects are published");
        }
    }

    [Fact]
    public void NuGet_TestAndSampleProjects_HaveIsPackableFalse()
    {
        var repoRoot = FindRepoRoot();
        var nonPackableProjects = new[]
        {
            "tests/Lumi.Tests/Lumi.Tests.csproj",
            "samples/HelloWorld/HelloWorld.csproj"
        };

        foreach (var project in nonPackableProjects)
        {
            var path = Path.Combine(repoRoot, project.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"Project file not found: {path}");

            var doc = XDocument.Load(path);
            var ns = doc.Root!.Name.Namespace;
            var isPackable = doc.Descendants(ns + "IsPackable").FirstOrDefault();
            Assert.True(isPackable != null && isPackable.Value.Equals("false", StringComparison.OrdinalIgnoreCase),
                $"{project} should have <IsPackable>false</IsPackable>");
        }
    }

    // ─── Window Integration Tests ───

    [Fact]
    public void Window_HasHotReloadProperties()
    {
        var window = new Window();
        Assert.Null(window.HtmlPath);
        Assert.Null(window.CssPath);
        Assert.False(window.EnableHotReload);

        window.EnableHotReload = true;
        Assert.True(window.EnableHotReload);
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Lumi.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new InvalidOperationException("Could not find repository root (Lumi.slnx)");
    }
}
