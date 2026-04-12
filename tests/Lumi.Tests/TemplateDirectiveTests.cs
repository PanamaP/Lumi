using System.Collections.ObjectModel;
using System.ComponentModel;
using Lumi.Core;
using Lumi.Core.Binding;
using Lumi.Styling;

namespace Lumi.Tests;

public class TemplateDirectiveTests : IDisposable
{
    public TemplateDirectiveTests()
    {
        // Ensure the template engine can parse HTML
        TemplateEngine.HtmlParser = HtmlTemplateParser.Parse;
    }

    public void Dispose()
    {
        // Reset to avoid leaking state between tests
        TemplateEngine.HtmlParser = HtmlTemplateParser.Parse;
    }

    #region Parser Tests

    [Fact]
    public void Parser_DetectsTemplateForDirective()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><li>{item.Name}</li></template></div>");

        var div = root.Children[0];
        Assert.Single(div.Children);

        var templateFor = Assert.IsType<TemplateForElement>(div.Children[0]);
        Assert.Equal("Items", templateFor.CollectionPath);
        Assert.Equal("item", templateFor.ItemAlias);
        Assert.Contains("<li>", templateFor.TemplateHtml);
    }

    [Fact]
    public void Parser_DetectsTemplateIfDirective()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template if=\"{IsVisible}\"><span>Visible!</span></template></div>");

        var div = root.Children[0];
        Assert.Single(div.Children);

        var templateIf = Assert.IsType<TemplateIfElement>(div.Children[0]);
        Assert.Equal("IsVisible", templateIf.ConditionPath);
        Assert.Contains("<span>", templateIf.TemplateHtml);
    }

    [Fact]
    public void Parser_TemplateWithoutDirective_CreatesNormalElement()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template><span>Normal</span></template></div>");

        var div = root.Children[0];
        Assert.Single(div.Children);
        Assert.IsNotType<TemplateForElement>(div.Children[0]);
        Assert.IsNotType<TemplateIfElement>(div.Children[0]);
    }

    [Fact]
    public void Parser_TemplateFor_DefaultAlias()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\"><li>text</li></template></div>");

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal("item", templateFor.ItemAlias);
    }

    #endregion

    #region TemplateFor Tests

    [Fact]
    public void TemplateFor_RendersItemsFromList()
    {
        var root = HtmlTemplateParser.Parse(
            "<ul><template for=\"{Names}\" as=\"name\"><li>{name}</li></template></ul>");

        var vm = new SimpleListViewModel { Names = ["Alice", "Bob", "Charlie"] };
        TemplateEngine.Apply(root, vm);

        var ul = root.Children[0];
        var templateFor = Assert.IsType<TemplateForElement>(ul.Children[0]);
        Assert.Equal(3, templateFor.Children.Count);
    }

    [Fact]
    public void TemplateFor_InterpolatesItemProperties()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{People}\" as=\"person\"><span>{person.Name}</span></template></div>");

        var vm = new PeopleViewModel
        {
            People = [new Person { Name = "Alice" }, new Person { Name = "Bob" }]
        };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(2, templateFor.Children.Count);

        // Each child is a template-item container wrapping the <span>
        var firstSpan = FindTextElement(templateFor.Children[0]);
        var secondSpan = FindTextElement(templateFor.Children[1]);

        Assert.NotNull(firstSpan);
        Assert.NotNull(secondSpan);
        Assert.Equal("Alice", firstSpan!.Text);
        Assert.Equal("Bob", secondSpan!.Text);
    }

    [Fact]
    public void TemplateFor_EmptyCollection_NoChildren()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Names}\" as=\"item\"><span>{item}</span></template></div>");

        var vm = new SimpleListViewModel { Names = [] };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Empty(templateFor.Children);
    }

    [Fact]
    public void TemplateFor_ObservableCollection_AddItem()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        var items = new ObservableCollection<string>(["A", "B"]);
        var vm = new ObservableListViewModel { Items = items };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(2, templateFor.Children.Count);

        // Add an item — should auto-update
        items.Add("C");
        Assert.Equal(3, templateFor.Children.Count);
    }

    [Fact]
    public void TemplateFor_ObservableCollection_RemoveItem()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        var items = new ObservableCollection<string>(["A", "B", "C"]);
        var vm = new ObservableListViewModel { Items = items };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(3, templateFor.Children.Count);

        // Remove an item
        items.RemoveAt(1);
        Assert.Equal(2, templateFor.Children.Count);
    }

    [Fact]
    public void TemplateFor_ObservableCollection_Reset()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        var items = new ObservableCollection<string>(["A", "B", "C"]);
        var vm = new ObservableListViewModel { Items = items };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(3, templateFor.Children.Count);

        // Clear collection
        items.Clear();
        Assert.Empty(templateFor.Children);

        // Subscription must survive Reset — adding items should still work
        items.Add("D");
        Assert.Single(templateFor.Children);
    }

    [Fact]
    public void TemplateFor_ObservableCollection_Reset_FullLifecycleAfterClear()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        var items = new ObservableCollection<string>(["A", "B"]);
        var vm = new ObservableListViewModel { Items = items };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(2, templateFor.Children.Count);

        // Reset via Clear
        items.Clear();
        Assert.Empty(templateFor.Children);

        // Full lifecycle after Reset: Add → Remove → Add
        items.Add("X");
        items.Add("Y");
        items.Add("Z");
        Assert.Equal(3, templateFor.Children.Count);

        items.RemoveAt(1); // Remove "Y"
        Assert.Equal(2, templateFor.Children.Count);

        items.Add("W");
        Assert.Equal(3, templateFor.Children.Count);
    }

    [Fact]
    public void TemplateFor_ObservableCollection_ReplaceItem()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        var items = new ObservableCollection<string>(["A", "B", "C"]);
        var vm = new ObservableListViewModel { Items = items };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(3, templateFor.Children.Count);

        // Replace via index assignment
        items[1] = "Z";
        Assert.Equal(3, templateFor.Children.Count);

        var replacedSpan = FindTextElement(templateFor.Children[1]);
        Assert.NotNull(replacedSpan);
        Assert.Equal("Z", replacedSpan!.Text);
    }

    [Fact]
    public void TemplateFor_NullCollection_ClearsStaleChildren()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Names}\" as=\"item\"><span>{item}</span></template></div>");

        var vm = new SimpleListViewModel { Names = ["A", "B"] };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(2, templateFor.Children.Count);

        // Re-apply with null collection — stale children must be cleared
        vm.Names = null!;
        TemplateEngine.Apply(root, vm);
        Assert.Empty(templateFor.Children);
    }

    #endregion

    #region TemplateIf Tests

    [Fact]
    public void TemplateIf_TrueCondition_RendersContent()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template if=\"{IsVisible}\"><span>Hello</span></template></div>");

        var vm = new ConditionalViewModel { IsVisible = true };
        TemplateEngine.Apply(root, vm);

        var templateIf = Assert.IsType<TemplateIfElement>(root.Children[0].Children[0]);
        Assert.NotEmpty(templateIf.Children);
    }

    [Fact]
    public void TemplateIf_FalseCondition_NoChildren()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template if=\"{IsVisible}\"><span>Hello</span></template></div>");

        var vm = new ConditionalViewModel { IsVisible = false };
        TemplateEngine.Apply(root, vm);

        var templateIf = Assert.IsType<TemplateIfElement>(root.Children[0].Children[0]);
        Assert.Empty(templateIf.Children);
    }

    [Fact]
    public void TemplateIf_PropertyChange_TogglesVisibility()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template if=\"{IsVisible}\"><span>Hello</span></template></div>");

        var vm = new ConditionalViewModel { IsVisible = false };
        TemplateEngine.Apply(root, vm);

        var templateIf = Assert.IsType<TemplateIfElement>(root.Children[0].Children[0]);
        Assert.Empty(templateIf.Children);

        // Toggle to true
        vm.IsVisible = true;
        Assert.NotEmpty(templateIf.Children);

        // Toggle back to false
        vm.IsVisible = false;
        Assert.Empty(templateIf.Children);
    }

    [Fact]
    public void TemplateIf_InterpolationNotSupported_RendersLiterally()
    {
        // Known limitation: {PropertyPath} inside template-if is not interpolated
        var root = HtmlTemplateParser.Parse(
            "<div><template if=\"{IsVisible}\"><span>{IsVisible}</span></template></div>");

        var vm = new ConditionalViewModel { IsVisible = true };
        TemplateEngine.Apply(root, vm);

        var templateIf = Assert.IsType<TemplateIfElement>(root.Children[0].Children[0]);
        Assert.NotEmpty(templateIf.Children);

        // Text should remain as the literal pattern since alias is null
        var span = FindTextElement(templateIf.Children[0]);
        Assert.NotNull(span);
        Assert.Equal("{IsVisible}", span!.Text);
    }

    #endregion

    #region Nesting Tests

    [Fact]
    public void TemplateFor_NestedInIf()
    {
        // Simpler HTML for nested test (no extra whitespace)
        var html = "<div><template if=\"{HasItems}\"><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></template></div>";

        var root = HtmlTemplateParser.Parse(html);
        var items = new ObservableCollection<string>(["X", "Y"]);
        var vm = new NestedViewModel { HasItems = true, Items = items };
        TemplateEngine.Apply(root, vm);

        var templateIf = Assert.IsType<TemplateIfElement>(root.Children[0].Children[0]);
        Assert.NotEmpty(templateIf.Children);

        // Find the template-for inside the rendered if content
        var templateFor = FindElement<TemplateForElement>(templateIf);
        Assert.NotNull(templateFor);
        Assert.Equal(2, templateFor!.Children.Count);
    }

    #endregion

    #region Text Interpolation Tests (via TemplateBinding)

    [Fact]
    public void TemplateBinding_ReplacesAliasProperties()
    {
        var person = new Person { Name = "Alice" };
        var textEl = new TextElement("Hello, {p.Name}!");
        var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "p", person);

        Assert.NotNull(binding);
        Assert.Equal("Hello, Alice!", textEl.Text);
        binding!.Dispose();
    }

    [Fact]
    public void TemplateBinding_BareAlias_UsesToString()
    {
        var textEl = new TextElement("Value: {item}");
        var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "item", "hello");

        Assert.NotNull(binding);
        Assert.Equal("Value: hello", textEl.Text);
        binding!.Dispose();
    }

    [Fact]
    public void TemplateBinding_NoMatch_ReturnsNull()
    {
        var textEl = new TextElement("No bindings here");
        var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "item", "test");

        Assert.Null(binding);
        Assert.Equal("No bindings here", textEl.Text);
    }

    [Fact]
    public void TemplateBinding_UpdatesOnPropertyChange()
    {
        var person = new NotifyPerson { Name = "Alice" };
        var textEl = new TextElement("{p.Name}");
        var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "p", person);

        Assert.NotNull(binding);
        Assert.Equal("Alice", textEl.Text);

        // Change the property — text should update reactively
        person.Name = "Bob";
        Assert.Equal("Bob", textEl.Text);

        binding!.Dispose();
    }

    [Fact]
    public void TemplateBinding_MixedContent_UpdatesOnPropertyChange()
    {
        var person = new NotifyPerson { Name = "Alice" };
        var textEl = new TextElement("Hello {p.Name}, welcome!");
        var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "p", person);

        Assert.NotNull(binding);
        Assert.Equal("Hello Alice, welcome!", textEl.Text);

        person.Name = "Bob";
        Assert.Equal("Hello Bob, welcome!", textEl.Text);

        binding!.Dispose();
    }

    [Fact]
    public void TemplateBinding_Dispose_UnsubscribesEvents()
    {
        var person = new NotifyPerson { Name = "Alice" };
        var textEl = new TextElement("{p.Name}");
        var binding = TemplateBinding.TryCreate(textEl, "Text", false, textEl.Text, "p", person);

        Assert.NotNull(binding);
        Assert.Equal("Alice", textEl.Text);

        binding!.Dispose();

        // After dispose, changes should NOT update the text
        person.Name = "Bob";
        Assert.Equal("Alice", textEl.Text);
    }

    [Fact]
    public void TemplateBinding_AttributeBinding()
    {
        var person = new Person { Name = "Alice" };
        var el = new BoxElement("div");
        el.Attributes["data-name"] = "{p.Name}";
        var binding = TemplateBinding.TryCreate(el, "data-name", true, "{p.Name}", "p", person);

        Assert.NotNull(binding);
        Assert.Equal("Alice", el.Attributes["data-name"]);
        binding!.Dispose();
    }

    [Fact]
    public void TemplateBinding_LiteralBraces_DoNotThrow()
    {
        var person = new Person { Name = "Alice" };
        var textEl = new TextElement("CSS .x { color: red } and {p.Name}");
        var binding = TemplateBinding.TryCreate(
            textEl, "Text", false, textEl.Text, "p", person);

        Assert.NotNull(binding);
        Assert.Equal("CSS .x { color: red } and Alice", textEl.Text);
        binding!.Dispose();
    }

    #endregion

    #region DeepClone Tests

    [Fact]
    public void DeepClone_ClonesAllProperties()
    {
        var original = new BoxElement("div");
        original.Id = "test-id";
        original.Classes.Add("cls-a");
        original.Classes.Add("cls-b");
        original.InlineStyle = "color: red";
        original.Attributes["data-x"] = "42";
        original.IsFocusable = true;
        original.TabIndex = 3;

        var child = new TextElement("hello");
        original.AddChild(child);

        var clone = original.DeepClone();

        Assert.IsType<BoxElement>(clone);
        Assert.Equal("div", clone.TagName);
        Assert.Equal("test-id", clone.Id);
        Assert.Equal(2, clone.Classes.Count);
        Assert.Contains("cls-a", clone.Classes);
        Assert.Contains("cls-b", clone.Classes);
        Assert.Equal("color: red", clone.InlineStyle);
        Assert.Equal("42", clone.Attributes["data-x"]);
        Assert.True(clone.IsFocusable);
        Assert.Equal(3, clone.TabIndex);
        Assert.Single(clone.Children);
        Assert.IsType<TextElement>(clone.Children[0]);
        Assert.Equal("hello", ((TextElement)clone.Children[0]).Text);
    }

    [Fact]
    public void DeepClone_DoesNotShareReferences()
    {
        var original = new BoxElement("div");
        original.Id = "original";
        original.Classes.Add("a");
        original.Attributes["key"] = "val";
        var child = new TextElement("text");
        original.AddChild(child);

        var clone = original.DeepClone();

        // Mutating clone should NOT affect original
        clone.Id = "cloned";
        clone.Classes.Add("b");
        clone.Attributes["key"] = "changed";
        ((TextElement)clone.Children[0]).Text = "modified";

        Assert.Equal("original", original.Id);
        Assert.Single(original.Classes);
        Assert.Equal("val", original.Attributes["key"]);
        Assert.Equal("text", ((TextElement)original.Children[0]).Text);

        // Clone should have no parent or DataContext
        Assert.Null(clone.Parent);
        Assert.Null(clone.DataContext);
    }

    #endregion

    #region Prototype Cache Tests

    [Fact]
    public void TemplateFor_UsesPrototypeCache()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        int parseCount = 0;
        var originalParser = TemplateEngine.HtmlParser;
        TemplateEngine.HtmlParser = html =>
        {
            parseCount++;
            return originalParser!(html);
        };

        var items = new ObservableCollection<string>(["A", "B"]);
        var vm = new ObservableListViewModel { Items = items };
        TemplateEngine.Apply(root, vm);

        // Should parse the template HTML exactly once (prototype cache)
        Assert.Equal(1, parseCount);

        // Adding more items should NOT trigger additional parses (uses clone)
        items.Add("C");
        items.Add("D");
        Assert.Equal(1, parseCount);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        Assert.Equal(4, templateFor.Children.Count);
    }

    #endregion

    #region Item Property Change Tests

    [Fact]
    public void TemplateFor_ItemPropertyChange_UpdatesText()
    {
        var root = HtmlTemplateParser.Parse(
            "<div><template for=\"{People}\" as=\"person\"><span>{person.Name}</span></template></div>");

        var alice = new NotifyPerson { Name = "Alice" };
        var bob = new NotifyPerson { Name = "Bob" };
        var vm = new NotifyPeopleViewModel
        {
            People = new ObservableCollection<NotifyPerson>([alice, bob])
        };
        TemplateEngine.Apply(root, vm);

        var templateFor = Assert.IsType<TemplateForElement>(root.Children[0].Children[0]);
        var firstSpan = FindTextElement(templateFor.Children[0]);
        Assert.Equal("Alice", firstSpan!.Text);

        // Change the item's property — text should update reactively
        alice.Name = "Alicia";
        Assert.Equal("Alicia", firstSpan.Text);
    }

    #endregion

    #region Helpers

    private static TextElement? FindTextElement(Element root)
    {
        if (root is TextElement te)
            return te;
        foreach (var child in root.Children)
        {
            var found = FindTextElement(child);
            if (found != null) return found;
        }
        return null;
    }

    private static T? FindElement<T>(Element root) where T : Element
    {
        if (root is T t)
            return t;
        foreach (var child in root.Children)
        {
            var found = FindElement<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    #endregion

    #region Test ViewModels

    private class SimpleListViewModel
    {
        public List<string> Names { get; set; } = [];
    }

    private class ObservableListViewModel
    {
        public ObservableCollection<string> Items { get; set; } = [];
    }

    private class Person
    {
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    private class PeopleViewModel
    {
        public List<Person> People { get; set; } = [];
    }

    private class ConditionalViewModel : INotifyPropertyChanged
    {
        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private class NotifyPerson : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public override string ToString() => Name;
    }

    private class NotifyPeopleViewModel
    {
        public ObservableCollection<NotifyPerson> People { get; set; } = [];
    }

    private class NestedViewModel : INotifyPropertyChanged
    {
        private bool _hasItems;
        public bool HasItems
        {
            get => _hasItems;
            set
            {
                _hasItems = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
            }
        }

        public ObservableCollection<string> Items { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    #endregion
}
