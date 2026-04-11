var text = "hello 🚀 world";
Console.WriteLine($"String: '{text}'");
Console.WriteLine($"Length: {text.Length}");
for (int i = 0; i < text.Length; i++) {
    Console.WriteLine($"Index {i}: U+{(int)text[i]:X4} ('{text[i]}')");
}

Console.WriteLine();
Console.WriteLine("Testing backspace at position 7:");
var result1 = text[..(7 - 1)] + text[7..];
Console.WriteLine($"Result: '{result1}'");
Console.WriteLine($"Length: {result1.Length}");

Console.WriteLine();
Console.WriteLine("Testing delete at position 6:");
var result2 = text[..6] + text[(6 + 1)..];
Console.WriteLine($"Result: '{result2}'");
Console.WriteLine($"Length: {result2.Length}");
