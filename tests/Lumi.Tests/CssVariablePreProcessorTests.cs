using Lumi.Styling;

namespace Lumi.Tests;

public class CssVariablePreProcessorTests
{
    // --- Basic variable resolution ---

    [Fact]
    public void Process_NoVariables_ReturnsUnchanged()
    {
        var css = ".box { color: red; width: 100px; }";
        var result = CssVariablePreProcessor.Process(css);
        Assert.Equal(css, result);
    }

    [Fact]
    public void Process_SimpleVariable_ResolvesCorrectly()
    {
        var css = ":root { --primary: #3B82F6; } .btn { color: var(--primary); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("#3B82F6", result);
        Assert.DoesNotContain("var(--primary)", result);
    }

    [Fact]
    public void Process_MultipleVariables_ResolvesAll()
    {
        var css = @"
            :root {
                --color: red;
                --size: 16px;
            }
            .text { color: var(--color); font-size: var(--size); }
        ";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("red", result);
        Assert.Contains("16px", result);
        Assert.DoesNotContain("var(--color)", result);
        Assert.DoesNotContain("var(--size)", result);
    }

    // --- Fallback values ---

    [Fact]
    public void Process_NoVariablesDefined_VarRefsUntouched()
    {
        // When no custom properties are defined, the preprocessor returns CSS unchanged
        var css = ".box { color: var(--undefined, blue); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Equal(css, result);
    }

    [Fact]
    public void Process_UndefinedVariable_UsesFallback()
    {
        // var() resolution only happens when at least one custom property is defined
        var css = ":root { --other: 1; } .box { color: var(--undefined, blue); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("blue", result);
        Assert.DoesNotContain("var(", result);
    }

    [Fact]
    public void Process_UndefinedVariable_NoFallback_ReturnsInherit()
    {
        // var() resolution only happens when at least one custom property is defined
        var css = ":root { --other: 1; } .box { color: var(--undefined); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("inherit", result);
        Assert.DoesNotContain("var(", result);
    }

    [Fact]
    public void Process_DefinedVariable_IgnoresFallback()
    {
        var css = ":root { --color: red; } .box { color: var(--color, blue); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("red", result);
        Assert.DoesNotContain("blue", result);
    }

    // --- Nested variable references ---

    [Fact]
    public void Process_NestedVariables_ResolvesChain()
    {
        var css = @"
            :root {
                --base-color: #FF0000;
                --accent: var(--base-color);
            }
            .box { color: var(--accent); }
        ";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("#FF0000", result);
        Assert.DoesNotContain("var(", result);
    }

    [Fact]
    public void Process_DeeplyNestedVariables_ResolvesMultipleLevels()
    {
        var css = @"
            :root {
                --level1: 10px;
                --level2: var(--level1);
                --level3: var(--level2);
                --level4: var(--level3);
            }
            .box { width: var(--level4); }
        ";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("10px", result);
        Assert.DoesNotContain("var(", result);
    }

    // --- Custom property declaration removal ---

    [Fact]
    public void Process_RemovesCustomPropertyDeclarations()
    {
        var css = ":root { --color: red; } .box { color: var(--color); }";
        var result = CssVariablePreProcessor.Process(css);

        // The custom property declaration should be removed
        Assert.DoesNotContain("--color:", result);
    }

    // --- Multiple usage of same variable ---

    [Fact]
    public void Process_SameVariable_UsedMultipleTimes()
    {
        var css = @"
            :root { --spacing: 8px; }
            .a { margin: var(--spacing); }
            .b { padding: var(--spacing); }
        ";
        var result = CssVariablePreProcessor.Process(css);

        // Both usages should be resolved
        Assert.DoesNotContain("var(", result);
        // Count occurrences of "8px" — should appear at least twice
        var count = result.Split("8px").Length - 1;
        Assert.True(count >= 2, $"Expected '8px' at least twice, found {count} times");
    }

    // --- Edge cases ---

    [Fact]
    public void Process_EmptyInput_ReturnsEmpty()
    {
        var result = CssVariablePreProcessor.Process("");
        Assert.Equal("", result);
    }

    [Fact]
    public void Process_VariableWithDashes_InName()
    {
        var css = ":root { --my-long-variable-name: 42px; } .box { width: var(--my-long-variable-name); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("42px", result);
        Assert.DoesNotContain("var(", result);
    }

    [Fact]
    public void Process_VariableWithNumbers_InName()
    {
        var css = ":root { --color-100: #eee; } .box { color: var(--color-100); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("#eee", result);
    }

    [Fact]
    public void Process_VariableWithUnderscore_InName()
    {
        var css = ":root { --my_var: bold; } .text { font-weight: var(--my_var); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("bold", result);
    }

    [Fact]
    public void Process_CaseInsensitive_VariableNames()
    {
        // CSS spec says custom properties are case-sensitive, but this implementation
        // uses case-insensitive comparison — test that behavior
        var css = ":root { --MyColor: red; } .box { color: var(--mycolor); }";
        var result = CssVariablePreProcessor.Process(css);

        Assert.Contains("red", result);
    }

    [Fact]
    public void Process_VariableOverride_LastWins()
    {
        var css = @"
            :root { --color: red; }
            .theme { --color: blue; }
            .box { color: var(--color); }
        ";
        var result = CssVariablePreProcessor.Process(css);

        // Last definition should win
        Assert.Contains("blue", result);
    }

    [Fact]
    public void Process_FallbackWithSpaces_PreservedCorrectly()
    {
        var css = ".box { font-family: var(--font, Arial, sans-serif); }";
        var result = CssVariablePreProcessor.Process(css);

        // Since --font is not defined, fallback should be used
        Assert.Contains("Arial, sans-serif", result);
    }
}
