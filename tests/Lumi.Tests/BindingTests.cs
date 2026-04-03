using System.Collections.ObjectModel;
using System.ComponentModel;
using Lumi.Core;
using Lumi.Core.Binding;

namespace Lumi.Tests;

#region Test ViewModels

public class TestViewModel : INotifyPropertyChanged
{
    private string _name = "Test";
    public string Name
    {
        get => _name;
        set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); }
    }

    private int _age = 25;
    public int Age
    {
        get => _age;
        set { _age = value; PropertyChanged?.Invoke(this, new(nameof(Age))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class NestedUser
{
    public string FirstName { get; set; } = "John";
}

public class ParentViewModel : INotifyPropertyChanged
{
    private NestedUser _user = new();
    public NestedUser User
    {
        get => _user;
        set { _user = value; PropertyChanged?.Invoke(this, new(nameof(User))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

#endregion

public class BindingExpressionTests
{
    [Fact]
    public void Parse_SimpleBinding()
    {
        var expr = BindingExpression.Parse("{Binding Name}");
        Assert.Equal("Name", expr.Path);
        Assert.Equal(BindingMode.OneWay, expr.Mode);
        Assert.Null(expr.Converter);
        Assert.Null(expr.FallbackValue);
        Assert.Null(expr.Template);
    }

    [Fact]
    public void Parse_TwoWayMode()
    {
        var expr = BindingExpression.Parse("{Binding Name, Mode=TwoWay}");
        Assert.Equal("Name", expr.Path);
        Assert.Equal(BindingMode.TwoWay, expr.Mode);
    }

    [Fact]
    public void Parse_WithTemplate()
    {
        var expr = BindingExpression.Parse("{Binding Items, Template=itemTemplate}");
        Assert.Equal("Items", expr.Path);
        Assert.Equal("itemTemplate", expr.Template);
    }

    [Fact]
    public void Parse_WithConverter()
    {
        var expr = BindingExpression.Parse("{Binding Price, Converter=currency}");
        Assert.Equal("Price", expr.Path);
        Assert.Equal("currency", expr.Converter);
    }

    [Fact]
    public void Parse_WithFallbackValue()
    {
        var expr = BindingExpression.Parse("{Binding Name, FallbackValue=N/A}");
        Assert.Equal("Name", expr.Path);
        Assert.Equal("N/A", expr.FallbackValue);
    }

    [Fact]
    public void Parse_AllParameters()
    {
        var expr = BindingExpression.Parse("{Binding User.Name, Mode=TwoWay, Converter=upper, FallbackValue=unknown}");
        Assert.Equal("User.Name", expr.Path);
        Assert.Equal(BindingMode.TwoWay, expr.Mode);
        Assert.Equal("upper", expr.Converter);
        Assert.Equal("unknown", expr.FallbackValue);
    }

    [Fact]
    public void Parse_InvalidExpression_Throws()
    {
        Assert.Throws<FormatException>(() => BindingExpression.Parse("no braces"));
        Assert.Throws<FormatException>(() => BindingExpression.Parse("{NotBinding Foo}"));
    }

    [Fact]
    public void IsBindingExpression_ReturnsCorrectly()
    {
        Assert.True(BindingExpression.IsBindingExpression("{Binding Name}"));
        Assert.False(BindingExpression.IsBindingExpression("plain text"));
        Assert.False(BindingExpression.IsBindingExpression(null));
        Assert.False(BindingExpression.IsBindingExpression(""));
    }

    [Fact]
    public void Parse_OneTimeMode()
    {
        var expr = BindingExpression.Parse("{Binding Name, Mode=OneTime}");
        Assert.Equal(BindingMode.OneTime, expr.Mode);
    }
}

public class BindingEngineTests
{
    [Fact]
    public void Bind_SourcePropertyChange_UpdatesTarget()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "Alice" };
        var textEl = new TextElement();
        var expr = BindingExpression.Parse("{Binding Name}");

        engine.Bind(textEl, "Text", vm, expr);

        Assert.Equal("Alice", textEl.Text);

        vm.Name = "Bob";
        Assert.Equal("Bob", textEl.Text);
    }

    [Fact]
    public void Bind_DotPath_NavigatesNestedProperties()
    {
        var engine = new BindingEngine();
        var vm = new ParentViewModel();
        vm.User.FirstName = "Jane";

        var textEl = new TextElement();
        var expr = BindingExpression.Parse("{Binding User.FirstName}");

        engine.Bind(textEl, "Text", vm, expr);

        Assert.Equal("Jane", textEl.Text);

        // Replace the User object entirely — triggers PropertyChanged for "User"
        vm.User = new NestedUser { FirstName = "Kate" };
        Assert.Equal("Kate", textEl.Text);
    }

    [Fact]
    public void Bind_OneTime_DoesNotUpdateAfterInitial()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "Initial" };
        var textEl = new TextElement();
        var expr = BindingExpression.Parse("{Binding Name, Mode=OneTime}");

        engine.Bind(textEl, "Text", vm, expr);
        Assert.Equal("Initial", textEl.Text);

        vm.Name = "Changed";
        Assert.Equal("Initial", textEl.Text); // Should NOT update
    }

    [Fact]
    public void Bind_FallbackValue_UsedWhenNull()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "Test" };
        var textEl = new TextElement();
        var expr = new BindingExpression
        {
            Path = "NonExistentProperty",
            FallbackValue = "fallback"
        };

        engine.Bind(textEl, "Text", vm, expr);
        Assert.Equal("fallback", textEl.Text);
    }

    [Fact]
    public void UpdateAll_RefreshesAllBindings()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "Start" };
        var textEl = new TextElement();
        var expr = BindingExpression.Parse("{Binding Name, Mode=OneTime}");

        engine.Bind(textEl, "Text", vm, expr);
        vm.Name = "Updated";

        // OneTime won't auto-update, but UpdateAll should force it
        engine.UpdateAll();
        Assert.Equal("Updated", textEl.Text);
    }

    [Fact]
    public void ClearAll_RemovesBindings()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "Before" };
        var textEl = new TextElement();
        var expr = BindingExpression.Parse("{Binding Name}");

        engine.Bind(textEl, "Text", vm, expr);
        engine.ClearAll();

        vm.Name = "After";
        Assert.Equal("Before", textEl.Text); // Binding cleared, no update
    }

    [Fact]
    public void ResolvePath_WorksOnSimpleAndNestedPaths()
    {
        var vm = new ParentViewModel();
        vm.User.FirstName = "Alice";

        Assert.Equal(vm.User, BindingEngine.ResolvePath(vm, "User"));
        Assert.Equal("Alice", BindingEngine.ResolvePath(vm, "User.FirstName"));
        Assert.Null(BindingEngine.ResolvePath(vm, "NonExistent.Prop"));
    }
}

public class BindingContextTests
{
    [Fact]
    public void GetEffectiveDataContext_ReturnsOwnContext()
    {
        var vm = new TestViewModel();
        var element = new BoxElement();
        element.DataContext = vm;

        Assert.Same(vm, BindingContext.GetEffectiveDataContext(element));
    }

    [Fact]
    public void GetEffectiveDataContext_InheritsFromParent()
    {
        var vm = new TestViewModel();
        var parent = new BoxElement();
        parent.DataContext = vm;

        var child = new TextElement();
        parent.AddChild(child);

        Assert.Same(vm, BindingContext.GetEffectiveDataContext(child));
    }

    [Fact]
    public void GetEffectiveDataContext_WalksUpMultipleLevels()
    {
        var vm = new TestViewModel();
        var grandparent = new BoxElement();
        grandparent.DataContext = vm;

        var parent = new BoxElement();
        grandparent.AddChild(parent);

        var child = new TextElement();
        parent.AddChild(child);

        Assert.Same(vm, BindingContext.GetEffectiveDataContext(child));
    }

    [Fact]
    public void GetEffectiveDataContext_ChildOverridesParent()
    {
        var parentVm = new TestViewModel { Name = "Parent" };
        var childVm = new TestViewModel { Name = "Child" };

        var parent = new BoxElement();
        parent.DataContext = parentVm;

        var child = new TextElement();
        child.DataContext = childVm;
        parent.AddChild(child);

        Assert.Same(childVm, BindingContext.GetEffectiveDataContext(child));
    }

    [Fact]
    public void GetEffectiveDataContext_ReturnsNull_WhenNoneSet()
    {
        var element = new BoxElement();
        Assert.Null(BindingContext.GetEffectiveDataContext(element));
    }
}

public class TwoWayBindingTests
{
    [Fact]
    public void TwoWay_InputValueChange_UpdatesSource()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "Initial" };
        var input = new InputElement();
        var expr = BindingExpression.Parse("{Binding Name, Mode=TwoWay}");

        engine.Bind(input, "Value", vm, expr);
        Assert.Equal("Initial", input.Value);

        // Simulate user typing
        input.Value = "UserInput";
        Assert.Equal("UserInput", vm.Name);
    }

    [Fact]
    public void TwoWay_SourceChange_UpdatesInput()
    {
        var engine = new BindingEngine();
        var vm = new TestViewModel { Name = "First" };
        var input = new InputElement();
        var expr = BindingExpression.Parse("{Binding Name, Mode=TwoWay}");

        engine.Bind(input, "Value", vm, expr);

        vm.Name = "Second";
        Assert.Equal("Second", input.Value);
    }

    [Fact]
    public void ValueChanged_Event_FiresOnValueChange()
    {
        var input = new InputElement();
        string? receivedValue = null;
        input.ValueChanged += v => receivedValue = v;

        input.Value = "hello";
        Assert.Equal("hello", receivedValue);
    }

    [Fact]
    public void ValueChanged_DoesNotFire_WhenSameValue()
    {
        var input = new InputElement { Value = "same" };
        int fireCount = 0;
        input.ValueChanged += _ => fireCount++;

        input.Value = "same";
        Assert.Equal(0, fireCount);
    }
}

public class CollectionBindingTests
{
    [Fact]
    public void BindItemsSource_PopulatesChildren()
    {
        var container = new BoxElement();
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var renderer = new ItemsRenderer();

        renderer.BindItemsSource(container, items, () => new TextElement());

        Assert.Equal(3, container.Children.Count);
        Assert.Equal("A", container.Children[0].DataContext);
        Assert.Equal("B", container.Children[1].DataContext);
        Assert.Equal("C", container.Children[2].DataContext);
    }

    [Fact]
    public void BindItemsSource_AddItem_AddsChild()
    {
        var container = new BoxElement();
        var items = new ObservableCollection<string> { "A" };
        var renderer = new ItemsRenderer();

        renderer.BindItemsSource(container, items, () => new TextElement());
        Assert.Single(container.Children);

        items.Add("B");
        Assert.Equal(2, container.Children.Count);
        Assert.Equal("B", container.Children[1].DataContext);
    }

    [Fact]
    public void BindItemsSource_RemoveItem_RemovesChild()
    {
        var container = new BoxElement();
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var renderer = new ItemsRenderer();

        renderer.BindItemsSource(container, items, () => new TextElement());
        Assert.Equal(3, container.Children.Count);

        items.RemoveAt(1); // Remove "B"
        Assert.Equal(2, container.Children.Count);
        Assert.Equal("A", container.Children[0].DataContext);
        Assert.Equal("C", container.Children[1].DataContext);
    }

    [Fact]
    public void BindItemsSource_ClearCollection_ClearsChildren()
    {
        var container = new BoxElement();
        var items = new ObservableCollection<string> { "A", "B" };
        var renderer = new ItemsRenderer();

        renderer.BindItemsSource(container, items, () => new TextElement());
        items.Clear();

        Assert.Empty(container.Children);
    }

    [Fact]
    public void Unbind_StopsTrackingChanges()
    {
        var container = new BoxElement();
        var items = new ObservableCollection<string> { "A" };
        var renderer = new ItemsRenderer();

        renderer.BindItemsSource(container, items, () => new TextElement());
        renderer.Unbind();

        items.Add("B");
        Assert.Single(container.Children); // Should not have added
    }
}
