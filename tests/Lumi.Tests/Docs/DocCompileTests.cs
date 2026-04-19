using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lumi.Tests.Docs;

/// <summary>
/// Compiles every <c>```csharp</c> fenced block in the project's <c>docs/</c>
/// directory against the current Lumi assemblies. Designed to fail loudly when
/// the public API drifts away from the documented examples.
/// </summary>
/// <remarks>
/// <para>
/// Each block is wrapped in a default set of <c>using</c> directives plus a
/// throwaway namespace/class/method so that bare statements compile. Blocks
/// that contain their own top-level <c>class</c>, <c>namespace</c>, etc. are
/// compiled as compilation units with the same default usings prepended.
/// </para>
/// <para>
/// To opt a snippet out of the check (for intentionally illustrative or
/// partial code), place an HTML comment on the line immediately above the
/// fence:
/// <code>
/// &lt;!-- doc-compile:skip --&gt;
/// ```csharp
/// // illustrative-only, not expected to compile
/// ```
/// </code>
/// </para>
/// </remarks>
public class DocCompileTests
{
    private const string SkipMarker = "<!-- doc-compile:skip -->";

    private static readonly string[] DefaultUsings =
    {
        "System",
        "System.IO",
        "System.Linq",
        "System.Globalization",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "System.ComponentModel",
        "System.Runtime.CompilerServices",
        "Lumi",
        "Lumi.Core",
        "Lumi.Core.Animation",
        "Lumi.Core.Binding",
        "Lumi.Core.Components",
        "Lumi.Core.Navigation",
        "Lumi.Layout",
        "Lumi.Styling",
        "Lumi.Input",
        "Lumi.Rendering",
    };

    [Fact]
    public void CsharpBlocks_InDocs_Compile()
    {
        var docsDir = ResolveDocsDir();
        var failures = new List<string>();
        var totals = new Totals();

        foreach (var md in Directory.EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(md);
            foreach (var block in ExtractCsharpBlocks(content))
            {
                totals.Total++;
                if (block.Skipped)
                {
                    totals.Skipped++;
                    continue;
                }

                var diagnostics = TryCompile(block.Code);
                var errors = diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList();

                if (errors.Count == 0)
                {
                    totals.Compiled++;
                    continue;
                }

                totals.Failed++;
                var msg = string.Join("; ", errors.Select(e => e.GetMessage()));
                failures.Add($"{md}:{block.LineNumber}: {msg}");
            }
        }

        Assert.True(
            totals.Total > 0,
            "Expected to discover csharp doc blocks but found none under " + docsDir);

        Assert.True(
            totals.Total > totals.Skipped,
            $"Expected at least one csharp doc block to be compiled, but all discovered blocks were skipped under {docsDir} (total={totals.Total}, skipped={totals.Skipped}).");

        Assert.True(
            failures.Count == 0,
            $"Doc compile errors (total={totals.Total}, compiled={totals.Compiled}, skipped={totals.Skipped}, failed={totals.Failed}):\n"
            + string.Join("\n", failures));
    }

    private sealed class Totals
    {
        public int Total;
        public int Compiled;
        public int Skipped;
        public int Failed;
    }

    private sealed record CsharpBlock(string Code, int LineNumber, bool Skipped);

    private static IEnumerable<CsharpBlock> ExtractCsharpBlocks(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.TrimStart().StartsWith("```csharp", StringComparison.Ordinal))
                continue;

            int start = i + 1;
            int end = -1;
            for (int j = start; j < lines.Length; j++)
            {
                if (lines[j].TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    end = j;
                    break;
                }
            }
            if (end < 0)
                throw new InvalidOperationException(
                    $"Malformed documentation snippet: missing closing ``` fence for ```csharp block starting at line {start}.");

            bool skipped = false;
            for (int k = i - 1; k >= 0; k--)
            {
                var prev = lines[k].Trim();
                if (prev.Length == 0) continue;
                if (prev.Contains(SkipMarker, StringComparison.Ordinal))
                    skipped = true;
                break;
            }

            var code = string.Join("\n", lines.Skip(start).Take(end - start));
            yield return new CsharpBlock(code, start + 1, skipped);
            i = end;
        }
    }

    private static IReadOnlyList<Diagnostic> TryCompile(string snippet)
    {
        var (hoistedUsings, body) = HoistUsings(snippet);
        var usings = BuildUsings(hoistedUsings);

        // Try three wrap modes and keep the result with the fewest errors.
        // - Method body (default for statements / expressions)
        // - Class body (for snippets that declare members like `public override void OnReady()`)
        // - Compilation unit (for snippets that declare types or namespaces)
        var candidates = new[]
        {
            WrapAsMethodBody(usings, body),
            WrapAsClassBody(usings, body),
            WrapAsCompilationUnit(usings, body),
        };

        IReadOnlyList<Diagnostic> best = Array.Empty<Diagnostic>();
        int bestErrors = int.MaxValue;

        foreach (var source in candidates)
        {
            var diagnostics = Compile(source);
            var errorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
            if (errorCount < bestErrors)
            {
                best = diagnostics;
                bestErrors = errorCount;
                if (bestErrors == 0) break;
            }
        }

        return best;
    }

    private static IReadOnlyList<Diagnostic> Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        var compilation = CSharpCompilation.Create(
            "_DocSnippet_" + Guid.NewGuid().ToString("N"),
            new[] { tree },
            BuildReferences(),
            options);

        return compilation.GetDiagnostics();
    }

    private static string BuildUsings(IEnumerable<string> hoisted)
    {
        var sb = new StringBuilder();
        foreach (var ns in DefaultUsings)
            sb.AppendLine($"using {ns};");
        foreach (var u in hoisted)
            sb.AppendLine(u);
        return sb.ToString();
    }

    /// <summary>
    /// Common context names that doc snippets reference without explicit
    /// declaration (e.g. <c>panel</c>, <c>button</c>). Declared as fields on
    /// the wrapper so snippets compile without a setup preamble.
    /// </summary>
    private const string PredefinedContext = """
        public Lumi.Core.Element element = new Lumi.Core.BoxElement();
        public Lumi.Core.Element panel = new Lumi.Core.BoxElement();
        public Lumi.Core.Element button = new Lumi.Core.BoxElement("button");
        public Lumi.Core.Element container = new Lumi.Core.BoxElement();
        public Lumi.Core.Element toolbar = new Lumi.Core.BoxElement();
        public Lumi.Core.Element form = new Lumi.Core.BoxElement();
        public Lumi.Core.Element hostElement = new Lumi.Core.BoxElement();
        public Lumi.Core.Element scrollContainer = new Lumi.Core.BoxElement();
        public Lumi.Core.Element myPanel = new Lumi.Core.BoxElement();
        public Lumi.Core.Element parentElement = new Lumi.Core.BoxElement();
        public Lumi.Core.Element header = new Lumi.Core.BoxElement();
        public Lumi.Core.Element settings = new Lumi.Core.BoxElement();
        public Lumi.Core.Element sidebar = new Lumi.Core.BoxElement();
        public Lumi.Core.Element root = new Lumi.Core.BoxElement();
        public Lumi.Core.InputElement input = new Lumi.Core.InputElement();
        """;

    private static string WrapAsMethodBody(string usings, string body) =>
        usings + "\nnamespace _DocSnippet { public class _Snippet : Lumi.Window {\n"
        + PredefinedContext + "\n"
        + "void _Method() {\n" + body + "\n} } }";

    private static string WrapAsClassBody(string usings, string body) =>
        usings + "\nnamespace _DocSnippet { public class _Snippet : Lumi.Window {\n"
        + PredefinedContext + "\n"
        + body + "\n} }";

    private static string WrapAsCompilationUnit(string usings, string body) =>
        usings + "\n" + body;

    private static readonly Regex UsingDirectiveRegex = new(
        @"^\s*using\s+(?:static\s+)?[A-Za-z_][\w.]*\s*;\s*$",
        RegexOptions.Compiled);

    private static (List<string> Usings, string Body) HoistUsings(string snippet)
    {
        var lines = snippet.Replace("\r\n", "\n").Split('\n');
        var hoisted = new List<string>();
        var kept = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            if (UsingDirectiveRegex.IsMatch(line))
                hoisted.Add(line.Trim());
            else
                kept.Add(line);
        }
        return (hoisted, string.Join("\n", kept));
    }

    private static readonly Lazy<IReadOnlyList<MetadataReference>> CachedReferences =
        new(BuildReferencesCore, isThreadSafe: true);

    private static IReadOnlyList<MetadataReference> BuildReferences() => CachedReferences.Value;

    private static IReadOnlyList<MetadataReference> BuildReferencesCore()
    {
        var refs = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrEmpty(tpa))
        {
            foreach (var path in tpa.Split(Path.PathSeparator))
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (!File.Exists(path)) continue;
                if (!seen.Add(path)) continue;
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        Type[] anchors =
        {
            typeof(Lumi.LumiApp),
            typeof(Lumi.Core.Element),
            typeof(Lumi.Core.Components.LumiButton),
            typeof(Lumi.Core.Binding.BindingExpression),
            typeof(Lumi.Core.Animation.AnimationBuilder),
            typeof(Lumi.Layout.YogaLayoutEngine),
            typeof(Lumi.Styling.StyleResolver),
            typeof(Lumi.Input.HitTester),
            typeof(Lumi.Rendering.SkiaRenderer),
        };
        foreach (var t in anchors)
        {
            var loc = t.Assembly.Location;
            if (string.IsNullOrEmpty(loc)) continue;
            if (!seen.Add(loc)) continue;
            refs.Add(MetadataReference.CreateFromFile(loc));
        }

        return refs;
    }

    private static string ResolveDocsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "docs");
            if (Directory.Exists(candidate)
                && Directory.EnumerateFiles(candidate, "*.md", SearchOption.AllDirectories).Any())
                return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate docs/ directory walking up from " + AppContext.BaseDirectory);
    }
}
