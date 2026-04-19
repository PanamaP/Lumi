using System.Text;
using BenchmarkDotNet.Attributes;
using Lumi.Styling;

namespace Lumi.Benchmarks;

/// <summary>
/// Measures CssParser.Parse on a synthetic ~2000-rule stylesheet built once in setup.
/// </summary>
[MemoryDiagnoser]
public class CssParseBenchmarks
{
    private string _css = null!;

    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder(capacity: 200_000);
        for (int i = 0; i < 2000; i++)
        {
            sb.Append(".rule").Append(i).Append(" { ");
            sb.Append("color: #").Append((i * 37 % 0xFFFFFF).ToString("x6")).Append("; ");
            sb.Append("padding: ").Append(i % 32).Append("px; ");
            sb.Append("margin: ").Append(i % 16).Append("px; ");
            sb.Append("background: rgb(").Append(i % 256).Append(", ").Append((i * 3) % 256).Append(", ").Append((i * 7) % 256).Append("); ");
            sb.Append("font-size: ").Append(10 + (i % 20)).Append("px; ");
            sb.Append("display: ").Append(i % 2 == 0 ? "flex" : "block").Append("; ");
            sb.Append("}\n");
        }
        _css = sb.ToString();
    }

    [Benchmark]
    public ParsedStyleSheet Parse_2000Rules()
    {
        return CssParser.Parse(_css);
    }
}
