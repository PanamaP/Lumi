using System;
using System.Text;
using System.Threading.Tasks;
using Lumi.Styling;
using Xunit;

namespace Lumi.Tests.Fuzz;

// Cheap "parsers don't crash or hang on adversarial input" coverage.
// NOT coverage-guided fuzzing — bounded random with a fixed seed for
// deterministic repro. Seed is 0; failures report iteration index plus
// the offending input so they can be lifted into a regression test.
public class ParserSmokeTests
{
    private const int Seed = 0;
    private const int ParserTimeoutMs = 2000;

    private static readonly char[] CssAlphabet = BuildAlphabet(
        "{}[]():;,.#% \n\t\"'");

    private static readonly char[] HtmlAlphabet = BuildAlphabet(
        "<>/=\" \n\t!?-");

    private static char[] BuildAlphabet(string extra)
    {
        var sb = new StringBuilder();
        for (char c = 'a'; c <= 'z'; c++) sb.Append(c);
        for (char c = 'A'; c <= 'Z'; c++) sb.Append(c);
        for (char c = '0'; c <= '9'; c++) sb.Append(c);
        sb.Append(extra);
        return sb.ToString().ToCharArray();
    }

    private static char NextBmpScalarChar(Random rng)
    {
        // Pick from the BMP while skipping the UTF-16 surrogate range
        // (0xD800-0xDFFF), which does not represent valid scalar values.
        int value = rng.Next(0x80, 0xF800);
        if (value >= 0xD800)
        {
            value += 0x800;
        }

        return (char)value;
    }

    private static string GenerateInput(Random rng, char[] alphabet)
    {
        int len = rng.Next(1, 4097);
        var sb = new StringBuilder(len);
        for (int i = 0; i < len; i++)
        {
            // ~2% chance of injecting an arbitrary BMP Unicode scalar character.
            if (rng.Next(50) == 0)
            {
                sb.Append(NextBmpScalarChar(rng));
            }
            else
            {
                sb.Append(alphabet[rng.Next(alphabet.Length)]);
            }
        }
        return sb.ToString();
    }

    private static string Snippet(string s)
    {
        const int max = 200;
        if (s.Length <= max) return s;
        return s.Substring(0, max) + "...(truncated, total=" + s.Length + ")";
    }

    // Note: StackOverflowException and AccessViolationException are not reliably
    // catchable in user code — they terminate the process. If a fuzz input triggers
    // either, the test runner crash itself is the signal. We only assert on
    // OutOfMemoryException (which is catchable) and on hangs (per-iteration timeout).

    [Fact]
    [Trait("Category", "Fuzz")]
    public void CssParser_NeverCrashes_OnRandomInput()
    {
        var rng = new Random(Seed);
        for (int i = 0; i < 5000; i++)
        {
            string input = GenerateInput(rng, CssAlphabet);
            var task = Task.Run(() =>
            {
                try { CssParser.Parse(input); }
                catch (OutOfMemoryException) { throw; }
                catch { /* ordinary parse exceptions are acceptable */ }
            });

#pragma warning disable xUnit1031 // intentional blocking wait for hang detection
            bool finished = task.Wait(ParserTimeoutMs);
#pragma warning restore xUnit1031
            if (!finished)
            {
                Assert.Fail($"CssParser hang at seed={Seed} iter={i} (>{ParserTimeoutMs}ms)\nInput: {Snippet(input)}");
            }

            if (task.Exception?.GetBaseException() is OutOfMemoryException oom)
            {
                Assert.Fail($"CssParser OOM at seed={Seed} iter={i}: {oom.Message}\nInput: {Snippet(input)}");
            }
        }
    }

    [Fact]
    [Trait("Category", "Fuzz")]
    public void HtmlTemplateParser_NeverCrashes_OnRandomInput()
    {
        var rng = new Random(Seed);
        for (int i = 0; i < 5000; i++)
        {
            string input = GenerateInput(rng, HtmlAlphabet);
            var task = Task.Run(() =>
            {
                try { HtmlTemplateParser.Parse(input); }
                catch (OutOfMemoryException) { throw; }
                catch { /* ordinary parse exceptions are acceptable */ }
            });

#pragma warning disable xUnit1031 // intentional blocking wait for hang detection
            bool finished = task.Wait(ParserTimeoutMs);
#pragma warning restore xUnit1031
            if (!finished)
            {
                Assert.Fail($"HtmlTemplateParser hang at seed={Seed} iter={i} (>{ParserTimeoutMs}ms)\nInput: {Snippet(input)}");
            }

            if (task.Exception?.GetBaseException() is OutOfMemoryException oom)
            {
                Assert.Fail($"HtmlTemplateParser OOM at seed={Seed} iter={i}: {oom.Message}\nInput: {Snippet(input)}");
            }
        }
    }

    [Fact]
    [Trait("Category", "Fuzz")]
    public void CssParser_NeverHangs_2sBudget()
    {
        var rng = new Random(Seed);
        for (int i = 0; i < 100; i++)
        {
            string input = GenerateInput(rng, CssAlphabet);
            var task = Task.Run(() =>
            {
                try { CssParser.Parse(input); }
                catch { /* ordinary exceptions are fine; we only care about hangs */ }
            });
#pragma warning disable xUnit1031 // intentional blocking wait for hang detection
            bool finished = task.Wait(ParserTimeoutMs);
#pragma warning restore xUnit1031
            if (!finished)
            {
                Assert.Fail($"CssParser hang at seed={Seed} iter={i} (>{ParserTimeoutMs}ms)\nInput: {Snippet(input)}");
            }
        }
    }
}
