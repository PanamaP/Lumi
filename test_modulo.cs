using System;

public class Test {
    public static void Main() {
        // Test case: a = -1, b = 3, pos = 1
        // Expected: (-1) * k + 3 = 1 => k = 2 (valid)
        // diff = 1 - 3 = -2
        // diff % a = -2 % -1 = 0 (in C#)
        // diff / a = -2 / -1 = 2 >= 0 ✓
        
        int a = -1, b = 3, pos = 1;
        int diff = pos - b;
        Console.WriteLine($"Testing a={a}, b={b}, pos={pos}");
        Console.WriteLine($"diff = {diff}");
        Console.WriteLine($"diff % a = {diff % a}");
        Console.WriteLine($"diff / a = {diff / a}");
        Console.WriteLine($"Result: {diff % a == 0 && diff / a >= 0}");
        Console.WriteLine();
        
        // Test case: a = -1, b = 3, pos = 4
        // Expected: (-1) * k + 3 = 4 => -k = 1 => k = -1 (invalid, k must be >= 0)
        // diff = 4 - 3 = 1
        // diff % a = 1 % -1 = 0
        // diff / a = 1 / -1 = -1 < 0 ✗
        pos = 4;
        diff = pos - b;
        Console.WriteLine($"Testing a={a}, b={b}, pos={pos}");
        Console.WriteLine($"diff = {diff}");
        Console.WriteLine($"diff % a = {diff % a}");
        Console.WriteLine($"diff / a = {diff / a}");
        Console.WriteLine($"Result: {diff % a == 0 && diff / a >= 0}");
    }
}
