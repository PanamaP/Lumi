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
            "<div><template for=\"{Items}\" as=\"item\"><span>{item}</span></template></div>");

        var vm = new SimpleListViewModel { Names = [] };
        // Use "Names" path doesn't match "Items" — let's fix this
        var root2 = HtmlTemplateParser.Parse(
            "<div><template for=\"{Names}\" as=\"item\"><span>{item}</span></template></div>");

        var vm2 = new SimpleListViewModel { Names = [] };
        TemplateEngine.Apply(root2, vm2);

        var templateFor = Assert.IsType<TemplateForElement>(root2.Children[0].Children[0]);
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

    #region Text Interpolation Tests

    [Fact]
    public void InterpolateText_ReplacesAliasProperties()
    {
        var person = new Person { Name = "Alice" };
        var result = TemplateEngine.InterpolateText("Hello, {p.Name}!", "p", person);
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void InterpolateText_BareAlias_UsesToString()
    {
        var result = TemplateEngine.InterpolateText("Value: {item}", "item", "hello");
        Assert.Equal("Value: hello", result);
    }

    [Fact]
    public void InterpolateText_NoMatch_ReturnsOriginal()
    {
        var result = TemplateEngine.InterpolateText("No bindings here", "item", "test");
        Assert.Equal("No bindings here", result);
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
