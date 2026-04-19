using Lumi.Core.Binding;

namespace Lumi.Tests.Binding;

/// <summary>
/// Targets surviving mutants in BindingExpression: brace requirements, the
/// "Binding " prefix, '=' index handling for parameters, and IsBindingExpression
/// guard for whitespace/braced strings.
/// </summary>
public class BindingExpressionTests
{
    [Theory]
    [InlineData("{Binding Name}")]
    [InlineData("  {Binding Name}  ")]
    public void Parse_SimplePath_PopulatesPathAndDefaultsToOneWay(string expr)
    {
        var be = BindingExpression.Parse(expr);
        Assert.Equal("Name", be.Path);
        Assert.Equal(BindingMode.OneWay, be.Mode);
        Assert.Null(be.Converter);
        Assert.Null(be.FallbackValue);
    }

    [Fact]
    public void Parse_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BindingExpression.Parse(null!));
    }

    [Theory]
    [InlineData("Binding Name")]      // missing braces
    [InlineData("{Binding Name")]      // missing close
    [InlineData("Binding Name}")]      // missing open
    public void Parse_MissingBraces_ThrowsFormatException(string expr)
    {
        Assert.Throws<FormatException>(() => BindingExpression.Parse(expr));
    }

    [Fact]
    public void Parse_MissingBindingPrefix_Throws()
    {
        Assert.Throws<FormatException>(() => BindingExpression.Parse("{Path Name}"));
    }

    [Fact]
    public void Parse_EmptyPath_Throws()
    {
        Assert.Throws<FormatException>(() => BindingExpression.Parse("{Binding }"));
    }

    [Fact]
    public void Parse_AllParameters_PopulatesEachField()
    {
        var be = BindingExpression.Parse(
            "{Binding First.Name, Mode=TwoWay, Converter=upper, FallbackValue=N/A, Template={0:C}}");
        Assert.Equal("First.Name", be.Path);
        Assert.Equal(BindingMode.TwoWay, be.Mode);
        Assert.Equal("upper", be.Converter);
        Assert.Equal("N/A", be.FallbackValue);
        Assert.Equal("{0:C}", be.Template);
    }

    [Fact]
    public void Parse_ParameterWithoutEquals_Throws()
    {
        // Second part has no '=' — should throw with FormatException pointing at the bad parameter.
        Assert.Throws<FormatException>(() => BindingExpression.Parse("{Binding Path, BadParam}"));
    }

    [Fact]
    public void Parse_UnknownParameter_Throws()
    {
        Assert.Throws<FormatException>(() => BindingExpression.Parse("{Binding Path, Unknown=value}"));
    }

    [Fact]
    public void Parse_OneTimeMode_ParsesCaseInsensitively()
    {
        var be = BindingExpression.Parse("{Binding X, Mode=onetime}");
        Assert.Equal(BindingMode.OneTime, be.Mode);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("    ", false)]
    [InlineData("not a binding", false)]
    [InlineData("{Binding Foo}", true)]
    [InlineData("  {Binding Foo}  ", true)]
    [InlineData("{Foo}", false)] // missing 'Binding ' prefix
    [InlineData("{Binding Foo", false)] // missing close brace
    public void IsBindingExpression_RecognisesFormat(string? value, bool expected)
    {
        Assert.Equal(expected, BindingExpression.IsBindingExpression(value));
    }
}
