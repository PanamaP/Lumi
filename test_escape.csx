using System;
using System.Collections.Generic;
using System.Text;

static string EscapeLiteralBraces(string template, int placeholderCount)
{
    var validPlaceholders = new HashSet<string>();
    for (int i = 0; i < placeholderCount; i++)
        validPlaceholders.Add(string.Format("{{{0}}}", i));

    var sb = new StringBuilder(template.Length);
    for (int i = 0; i < template.Length; i++)
    {
        if (template[i] == '{')
        {
            var remaining = template.AsSpan(i);
            bool isPlaceholder = false;
            foreach (var ph in validPlaceholders)
            {
                if (remaining.StartsWith(ph.AsSpan()))
                {
                    sb.Append(ph);
                    i += ph.Length - 1;
                    isPlaceholder = true;
                    break;
                }
            }
            if (!isPlaceholder)
                sb.Append("{{");
        }
        else if (template[i] == '}')
        {
            sb.Append("}}");
        }
        else
        {
            sb.Append(template[i]);
        }
    }
    return sb.ToString();
}

// Test cases
var tests = new[] {
    ("{0}", 1),
    ("hello {0} world", 1),
    ("{0}{1}", 2),
    ("a{0}b{1}c", 2),
    ("{literal} {0}", 1),
    ("price: {0} (was )", 1),
};

foreach (var (tmpl, count) in tests)
{
    var result = EscapeLiteralBraces(tmpl, count);
    Console.WriteLine($"Input: '{tmpl}' ({count}) => '{result}'");
    try { string.Format(result, new object[count]); Console.WriteLine("  Format: OK"); }
    catch (Exception ex) { Console.WriteLine($"  Format: FAILED - {ex.GetType().Name}: {ex.Message}"); }
}
