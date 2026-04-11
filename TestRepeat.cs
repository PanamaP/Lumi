using System;
using System.Linq;

var result = string.Join(" ", Enumerable.Repeat("1fr", 0));
Console.WriteLine($"Result: '{result}'");
Console.WriteLine($"Length: {result.Length}");
