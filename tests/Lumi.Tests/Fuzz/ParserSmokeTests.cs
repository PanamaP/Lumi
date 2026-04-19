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

    private static string GenerateInput(Random rng, char[] alphabet)
    {
        int len = rng.Next(1, 4097);
        var sb = new StringBuilder(len);
        for (int i = 0; i < len; i++)
        {
            // ~2% chance of injecting an arbitrary BMP unicode character.
            if (rng.Next(50) == 0)
            {
                sb.Append((char)rng.Next(0x80, 0xFFFE));
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

    [Fact]
    [Trait("Category", "Fuzz")]
    public void CssParser_NeverCrashes_OnRandomInput()
    {
        var rng = new Random(Seed);
        for (int i = 0; i < 5000; i++)
        {
            string input = GenerateInput(rng, CssAlphabet);
            try
            {
                CssParser.Parse(input);
            }
            catch (OutOfMemoryException ex)
            {
                Assert.Fail($"CssParser OOM at seed={Seed} iter={i}: {ex.Message}\nInput: {Snippet(input)}");
            }
            catch (StackOverflowException ex)
            {
                Assert.Fail($"CssParser StackOverflow at seed={Seed} iter={i}: {ex.Message}\nInput: {Snippet(input)}");
            }
            catch (AccessViolationException ex)
            {
                Assert.Fail($"CssParser AccessViolation at seed={Seed} iter={i}: {ex.Message}\nInput: {Snippet(input)}");
            }
            catch (Exception)
            {
                // ordinary parse exceptions are acceptable
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
            try
            {
                HtmlTemplateParser.Parse(input);
            }
            catch (OutOfMemoryException ex)
            {
                Assert.Fail($"HtmlTemplateParser OOM at seed={Seed} iter={i}: {ex.Message}\nInput: {Snippet(input)}");
            }
            catch (StackOverflowException ex)
            {
                Assert.Fail($"HtmlTemplateParser StackOverflow at seed={Seed} iter={i}: {ex.Message}\nInput: {Snippet(input)}");
            }
            catch (AccessViolationException ex)
            {
                Assert.Fail($"HtmlTemplateParser AccessViolation at seed={Seed} iter={i}: {ex.Message}\nInput: {Snippet(input)}");
            }
            catch (Exception)
            {
                // ordinary parse exceptions are acceptable
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
            bool finished = task.Wait(2000);
#pragma warning restore xUnit1031
            if (!finished)
            {
                Assert.Fail($"CssParser hang at seed={Seed} iter={i} (>2s)\nInput: {Snippet(input)}");
            }
        }
    }
}
