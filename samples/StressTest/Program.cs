using StressTest;

var scenario = args.Length > 0 ? args[0] : "all";
Console.WriteLine($"Lumi Stress Test — Scenario: {scenario}");
Console.WriteLine(new string('=', 50));

var window = new StressWindow(scenario);
Lumi.LumiApp.Run(window);
