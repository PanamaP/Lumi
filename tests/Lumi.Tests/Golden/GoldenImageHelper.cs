using Lumi.Tests.Helpers;
using SkiaSharp;

namespace Lumi.Tests.Golden;

/// <summary>
/// Helpers for golden-image (pixel baseline) regression tests.
///
/// Baselines live under <c>Golden/baselines/&lt;name&gt;.png</c> in the test project, copied
/// next to the test assembly via the project's <c>Content</c> item group.
///
/// <para><b>Opt-in execution.</b> Golden tests are skipped by default because Skia
/// rasterization can vary across operating systems and GPU drivers, and the committed
/// baselines were generated on a single platform. Set the environment variable
/// <c>LUMI_RUN_GOLDENS=1</c> to enable them (e.g. locally or on a CI job that matches
/// the baseline platform). Until per-OS baselines exist, leaving them disabled keeps
/// the cross-platform CI matrix deterministic.</para>
///
/// To regenerate baselines, set the environment variable <c>LUMI_REGEN_GOLDENS=1</c> and
/// run the golden tests. When set, <see cref="AssertGolden"/> writes the actual bitmap
/// to the source baseline path (if it can be discovered by walking up from the assembly
/// location), otherwise to the output directory baseline path, and skips the assertion.
/// Setting <c>LUMI_REGEN_GOLDENS=1</c> implicitly enables execution.
/// After regeneration, visually inspect the PNGs before committing.
/// </summary>
public static class GoldenImageHelper
{
    /// <summary>
    /// Returns <c>true</c> when golden tests should run. Enabled by either
    /// <c>LUMI_RUN_GOLDENS=1</c> or <c>LUMI_REGEN_GOLDENS=1</c>.
    /// </summary>
    public static bool IsEnabled =>
        Environment.GetEnvironmentVariable("LUMI_RUN_GOLDENS") == "1" ||
        Environment.GetEnvironmentVariable("LUMI_REGEN_GOLDENS") == "1";

    /// <summary>
    /// Render the given HTML+CSS through the headless pipeline and return a deep copy
    /// of the resulting bitmap (decoupled from the renderer's pixel buffer lifetime).
    /// </summary>
    public static SKBitmap RenderToBitmap(string html, string css, int w, int h)
    {
        using var pipeline = HeadlessPipeline.Render(html, css, w, h);
        var src = new SKBitmap();
        var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        src.InstallPixels(info, pipeline.Renderer.GetPixels());

        // Copy into a stand-alone bitmap so the caller owns its memory.
        var copy = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul));
        using (var canvas = new SKCanvas(copy))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(src, 0, 0);
        }
        src.Dispose();
        return copy;
    }

    /// <summary>
    /// Compare <paramref name="actual"/> against the baseline image named
    /// <paramref name="baselineName"/>. A pixel is considered matching when each channel
    /// differs by at most <paramref name="tolerancePerChannel"/>. The assertion fails if
    /// the ratio of mismatched pixels exceeds <paramref name="maxDifferingPixelRatio"/>.
    ///
    /// If <c>LUMI_REGEN_GOLDENS=1</c>, this method writes <paramref name="actual"/> to
    /// the baseline path (preferring the source folder) and returns without asserting.
    /// </summary>
    public static void AssertGolden(SKBitmap actual, string baselineName,
                                    int tolerancePerChannel = 2,
                                    double maxDifferingPixelRatio = 0.005)
    {
        var outputBaseline = Path.Combine(AppContext.BaseDirectory, "Golden", "baselines", baselineName + ".png");

        if (Environment.GetEnvironmentVariable("LUMI_REGEN_GOLDENS") == "1")
        {
            var sourceBaseline = TryFindSourceBaselinePath(baselineName);
            var target = sourceBaseline ?? outputBaseline;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var img = SKImage.FromBitmap(actual);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var fs = File.Create(target);
            data.SaveTo(fs);
            return;
        }

        if (!File.Exists(outputBaseline))
            throw new Xunit.Sdk.XunitException(
                $"Baseline '{baselineName}' not found at '{outputBaseline}'. " +
                "Run with LUMI_REGEN_GOLDENS=1 to generate it.");

        using var expected = SKBitmap.Decode(outputBaseline);
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            throw new Xunit.Sdk.XunitException(
                $"Baseline '{baselineName}' size mismatch: expected {expected.Width}x{expected.Height}, " +
                $"actual {actual.Width}x{actual.Height}.");

        long total = (long)actual.Width * actual.Height;
        long mismatched = 0;
        int worstX = -1, worstY = -1, worstDelta = -1;

        for (int y = 0; y < actual.Height; y++)
        {
            for (int x = 0; x < actual.Width; x++)
            {
                var a = actual.GetPixel(x, y);
                var e = expected.GetPixel(x, y);
                int dR = Math.Abs(a.Red - e.Red);
                int dG = Math.Abs(a.Green - e.Green);
                int dB = Math.Abs(a.Blue - e.Blue);
                int dA = Math.Abs(a.Alpha - e.Alpha);
                int max = Math.Max(Math.Max(dR, dG), Math.Max(dB, dA));
                if (max > tolerancePerChannel)
                {
                    mismatched++;
                    if (max > worstDelta)
                    {
                        worstDelta = max;
                        worstX = x;
                        worstY = y;
                    }
                }
            }
        }

        double ratio = (double)mismatched / total;
        if (ratio > maxDifferingPixelRatio)
        {
            var worstActual = actual.GetPixel(worstX, worstY);
            var worstExpected = expected.GetPixel(worstX, worstY);
            // Persist the actual bitmap next to the baseline for triage. If the write fails,
            // capture the error so it appears in the assertion message instead of being hidden.
            var failPath = Path.Combine(AppContext.BaseDirectory, "Golden", "baselines",
                                        baselineName + ".actual.png");
            string actualSuffix;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(failPath)!);
                using var img = SKImage.FromBitmap(actual);
                using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                using var fs = File.Create(failPath);
                data.SaveTo(fs);
                actualSuffix = $" Actual written to '{failPath}'.";
            }
            catch (Exception writeEx)
            {
                actualSuffix = $" Failed to write actual bitmap to '{failPath}': {writeEx.GetType().Name}: {writeEx.Message}.";
            }

            throw new Xunit.Sdk.XunitException(
                $"Golden '{baselineName}' mismatch: {mismatched}/{total} pixels differ " +
                $"(ratio={ratio:P3}, allowed={maxDifferingPixelRatio:P3}). " +
                $"Worst pixel at ({worstX},{worstY}): actual={worstActual} expected={worstExpected} " +
                $"maxChannelDelta={worstDelta}.{actualSuffix}");
        }
    }

    private static string? TryFindSourceBaselinePath(string baselineName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "tests", "Lumi.Tests")))
                return Path.Combine(dir.FullName, "tests", "Lumi.Tests", "Golden", "baselines",
                                    baselineName + ".png");
            dir = dir.Parent;
        }
        return null;
    }
}
