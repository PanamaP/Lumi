using FsCheck;

namespace Lumi.Tests.Properties;

/// <summary>
/// Custom FsCheck arbitraries used by the property-based tests.
/// </summary>
public static class Arbitraries
{
    /// <summary>
    /// Printable-ASCII char (space..~) suitable for InputElement.Type operations
    /// that should never produce control characters or surrogate halves.
    /// </summary>
    public static Arbitrary<PrintableAsciiChar> PrintableAsciiChar() =>
        Gen.Choose(32, 126).Select(i => new PrintableAsciiChar((char)i)).ToArbitrary();
}

public readonly record struct PrintableAsciiChar(char Value);
