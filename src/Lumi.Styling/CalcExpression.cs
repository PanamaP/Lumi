using System.Globalization;

namespace Lumi.Styling;

/// <summary>
/// Evaluates CSS calc() expressions with mixed units.
/// Supports +, -, *, / operators with px, em, rem, %, vh, vw, vmin, vmax, pt units.
/// </summary>
internal static class CalcExpression
{
    private const float RootFontSize = 16f;
    private const float PtToPx = 96f / 72f;

    /// <summary>
    /// Evaluate a calc() expression body (without the outer "calc(" and ")").
    /// <paramref name="percentBase"/> is the reference dimension for percentage
    /// values. When zero, percentages are resolved relative to viewport width.
    /// </summary>
    private const int MaxDepth = 32;

    public static float Evaluate(string expr, float fontSize, float viewportWidth, float viewportHeight, float fallback, float percentBase = 0)
    {
        try
        {
            float pctRef = percentBase > 0 ? percentBase : viewportWidth;
            int pos = 0;
            float result = ParseAddSub(expr.AsSpan(), ref pos, fontSize, viewportWidth, viewportHeight, pctRef, 0);
            return float.IsNaN(result) ? fallback : result;
        }
        catch
        {
            return fallback;
        }
    }

    // Addition and subtraction (lowest precedence)
    private static float ParseAddSub(ReadOnlySpan<char> expr, ref int pos, float fontSize, float vw, float vh, float pctRef, int depth)
    {
        if (depth > MaxDepth) return 0;
        float left = ParseMulDiv(expr, ref pos, fontSize, vw, vh, pctRef, depth);

        while (pos < expr.Length)
        {
            SkipSpaces(expr, ref pos);
            if (pos >= expr.Length) break;

            char op = expr[pos];
            // CSS calc requires spaces around + and - to disambiguate from signs
            if ((op == '+' || op == '-') && pos > 0 && pos + 1 < expr.Length
                && expr[pos - 1] == ' ' && expr[pos + 1] == ' ')
            {
                pos++;
                float right = ParseMulDiv(expr, ref pos, fontSize, vw, vh, pctRef, depth);
                left = op == '+' ? left + right : left - right;
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Multiplication and division (higher precedence)
    private static float ParseMulDiv(ReadOnlySpan<char> expr, ref int pos, float fontSize, float vw, float vh, float pctRef, int depth)
    {
        if (depth > MaxDepth) return 0;
        float left = ParseUnary(expr, ref pos, fontSize, vw, vh, pctRef, depth);

        while (pos < expr.Length)
        {
            SkipSpaces(expr, ref pos);
            if (pos >= expr.Length) break;

            char op = expr[pos];
            if (op == '*' || op == '/')
            {
                pos++;
                float right = ParseUnary(expr, ref pos, fontSize, vw, vh, pctRef, depth);
                left = op == '*' ? left * right : (right != 0 ? left / right : 0);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private static float ParseUnary(ReadOnlySpan<char> expr, ref int pos, float fontSize, float vw, float vh, float pctRef, int depth)
    {
        if (depth > MaxDepth) return 0;
        SkipSpaces(expr, ref pos);

        // Parenthesized sub-expression
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++; // skip '('
            float val = ParseAddSub(expr, ref pos, fontSize, vw, vh, pctRef, depth + 1);
            SkipSpaces(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')')
                pos++; // skip ')'
            return val;
        }

        return ParseValue(expr, ref pos, fontSize, vw, vh, pctRef);
    }

    private static float ParseValue(ReadOnlySpan<char> expr, ref int pos, float fontSize, float vw, float vh, float pctRef)
    {
        SkipSpaces(expr, ref pos);

        // Parse optional sign
        bool negative = false;
        if (pos < expr.Length && expr[pos] == '-')
        {
            negative = true;
            pos++;
        }
        else if (pos < expr.Length && expr[pos] == '+')
        {
            pos++;
        }

        // Parse number
        int numStart = pos;
        while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.'))
            pos++;

        if (pos == numStart)
            return 0;

        float number = float.Parse(expr[numStart..pos], NumberStyles.Float, CultureInfo.InvariantCulture);
        if (negative) number = -number;

        // Parse unit
        int unitStart = pos;
        while (pos < expr.Length && char.IsLetter(expr[pos]))
            pos++;
        // Also handle % sign
        if (pos < expr.Length && expr[pos] == '%')
            pos++;

        if (unitStart == pos)
            return number; // unitless

        var unit = expr[unitStart..pos];

        if (unit.Equals("px", StringComparison.OrdinalIgnoreCase))
            return number;
        if (unit.Equals("%", StringComparison.Ordinal))
            return number * pctRef / 100f;
        if (unit.Equals("em", StringComparison.OrdinalIgnoreCase))
            return number * fontSize;
        if (unit.Equals("rem", StringComparison.OrdinalIgnoreCase))
            return number * RootFontSize;
        if (unit.Equals("vh", StringComparison.OrdinalIgnoreCase))
            return number * vh / 100f;
        if (unit.Equals("vw", StringComparison.OrdinalIgnoreCase))
            return number * vw / 100f;
        if (unit.Equals("vmin", StringComparison.OrdinalIgnoreCase))
            return number * Math.Min(vw, vh) / 100f;
        if (unit.Equals("vmax", StringComparison.OrdinalIgnoreCase))
            return number * Math.Max(vw, vh) / 100f;
        if (unit.Equals("pt", StringComparison.OrdinalIgnoreCase))
            return number * PtToPx;

        return number; // unknown unit, treat as px
    }

    private static void SkipSpaces(ReadOnlySpan<char> s, ref int pos)
    {
        while (pos < s.Length && s[pos] == ' ')
            pos++;
    }
}
