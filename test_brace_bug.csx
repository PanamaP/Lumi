using Lumi.Core;
using Lumi.Core.Binding;

var person = new TestPerson { Name = "Alice" };
var textEl = new TextElement("Value is {p.Name} and range is [0..10}");
try {
    var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "p", person);
    Console.WriteLine($"Result: '{textEl.Text}'");
    Console.WriteLine("SUCCESS");
} catch (Exception ex) {
    Console.WriteLine($"ERROR: {ex.Message}");
}

public class TestPerson { public string Name { get; set; } = ""; }
