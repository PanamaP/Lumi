using System.Collections.ObjectModel;
using System.ComponentModel;
using Lumi.Core;
using Lumi.Core.Binding;
using Lumi.Tests.Helpers;

namespace Lumi.Tests.Integration;

/// <summary>
/// Regression tests that exercise data binding through the full render pipeline
/// (HTML parse → style → layout → render → pixel verification).
/// </summary>
[Collection("Integration")]
public class BindingRegressionTests
{
    #region Test ViewModels

    private class TestVM : INotifyPropertyChanged
    {
        private string _name = "Hello";
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private class UserModel
    {
        public string FirstName { get; set; } = "Alice";
    }

    private class ParentVM : INotifyPropertyChanged
    {
        private UserModel _user = new();
        public UserModel User
        {
            get => _user;
            set { _user = value; PropertyChanged?.Invoke(this, new(nameof(User))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    #endregion

    private const string BaseCss =
        "#host { width:400px; height:200px; display:flex; flex-direction:column; } " +
        "#label { color:black; font-size:16px; } " +
        "#field { width:200px; height:30px; }";

    /// <summary>
    /// 1. One-way binding renders — bind ViewModel.Name to TextElement.Text,
    ///    render → text element has correct text and non-zero layout width.
    /// </summary>
    [Fact]
    public void OneWayBinding_RendersCorrectText()
    {
        var vm = new TestVM { Name = "World" };
        var engine = new BindingEngine();

        using var p = HeadlessPipeline.Render(
            "<div id='host'><span id='label'></span></div>", BaseCss, 400, 200);

        var label = (TextElement)p.FindById("label")!;
        engine.Bind(label, "Text", vm, BindingExpression.Parse("{Binding Name}"));
        p.Rerender();

        Assert.Equal("World", label.Text);
        var layout = p.GetLayoutOf("label");
        Assert.True(layout.Width > 0, "Bound text element should have non-zero layout width");
    }

    /// <summary>
    /// 2. Source update re-renders — change ViewModel.Name, call engine.UpdateAll(),
    ///    rerender → new text visible.
    /// </summary>
    [Fact]
    public void SourceUpdate_Rerender_ShowsNewText()
    {
        var vm = new TestVM { Name = "First" };
        var engine = new BindingEngine();

        using var p = HeadlessPipeline.Render(
            "<div id='host'><span id='label'></span></div>", BaseCss, 400, 200);

        var label = (TextElement)p.FindById("label")!;
        engine.Bind(label, "Text", vm, BindingExpression.Parse("{Binding Name}"));
        p.Rerender();
        Assert.Equal("First", label.Text);

        vm.Name = "Second";
        engine.UpdateAll();
        p.Rerender();

        Assert.Equal("Second", label.Text);
        Assert.True(p.HasContentInRegion(0, 0, 400, 200),
            "Updated text should produce rendered pixels");
    }

    /// <summary>
    /// 3. DataContext inheritance — set DataContext on parent, bind child element
    ///    → child gets parent's context and renders correctly.
    /// </summary>
    [Fact]
    public void DataContextInheritance_ChildGetsParentContext()
    {
        var vm = new TestVM { Name = "Inherited" };
        var engine = new BindingEngine();

        using var p = HeadlessPipeline.Render(
            "<div id='parent'><span id='child'></span></div>", BaseCss, 400, 200);

        var parent = p.FindById("parent")!;
        parent.DataContext = vm;

        var child = p.FindById("child")!;
        var effective = BindingContext.GetEffectiveDataContext(child);
        Assert.Same(vm, effective);

        engine.Bind((TextElement)child, "Text", vm, BindingExpression.Parse("{Binding Name}"));
        p.Rerender();

        Assert.Equal("Inherited", ((TextElement)child).Text);
        Assert.True(p.GetLayoutOf("child").Width > 0,
            "Child bound via inherited DataContext should render");
    }

    /// <summary>
    /// 4. Dot-path binding — ViewModel with nested User.FirstName → resolves
    ///    through the object graph and renders.
    /// </summary>
    [Fact]
    public void DotPathBinding_ResolvesNestedProperty()
    {
        var vm = new ParentVM();
        vm.User.FirstName = "Jane";
        var engine = new BindingEngine();

        using var p = HeadlessPipeline.Render(
            "<div id='host'><span id='name'></span></div>", BaseCss, 400, 200);

        var nameEl = (TextElement)p.FindById("name")!;
        engine.Bind(nameEl, "Text", vm, BindingExpression.Parse("{Binding User.FirstName}"));
        p.Rerender();

        Assert.Equal("Jane", nameEl.Text);
        Assert.True(p.GetLayoutOf("name").Width > 0,
            "Dot-path bound text should have non-zero layout width");
    }

    /// <summary>
    /// 5. Collection binding adds children — bind ObservableCollection to container,
    ///    add items → children count increases after rerender.
    /// </summary>
    [Fact]
    public void CollectionBinding_AddItem_IncreasesChildrenAfterRerender()
    {
        var items = new ObservableCollection<string> { "A", "B" };
        var renderer = new ItemsRenderer();

        const string listCss =
            "#list { width:400px; height:300px; display:flex; flex-direction:column; }";
        using var p = HeadlessPipeline.Render(
            "<div id='list'></div>", listCss, 400, 300);

        var list = p.FindById("list")!;
        renderer.BindItemsSource(list, items, () => new TextElement());
        Assert.Equal(2, list.Children.Count);

        items.Add("C");
        Assert.Equal(3, list.Children.Count);

        p.Rerender();
        Assert.Equal(3, list.Children.Count);
        Assert.Equal("C", list.Children[2].DataContext);
    }

    /// <summary>
    /// 6. Collection binding removes children — remove item from collection
    ///    → children count decreases after rerender.
    /// </summary>
    [Fact]
    public void CollectionBinding_RemoveItem_DecreasesChildrenAfterRerender()
    {
        var items = new ObservableCollection<string> { "X", "Y", "Z" };
        var renderer = new ItemsRenderer();

        const string listCss =
            "#list { width:400px; height:300px; display:flex; flex-direction:column; }";
        using var p = HeadlessPipeline.Render(
            "<div id='list'></div>", listCss, 400, 300);

        var list = p.FindById("list")!;
        renderer.BindItemsSource(list, items, () => new TextElement());
        Assert.Equal(3, list.Children.Count);

        items.RemoveAt(1); // remove "Y"
        Assert.Equal(2, list.Children.Count);

        p.Rerender();
        Assert.Equal(2, list.Children.Count);
        Assert.Equal("X", list.Children[0].DataContext);
        Assert.Equal("Z", list.Children[1].DataContext);
    }

    /// <summary>
    /// 7. Two-way binding — bind InputElement with Mode=TwoWay,
    ///    change InputElement.Value → source property updates.
    /// </summary>
    [Fact]
    public void TwoWayBinding_InputValueChange_UpdatesSource()
    {
        var vm = new TestVM { Name = "Initial" };
        var engine = new BindingEngine();

        using var p = HeadlessPipeline.Render(
            "<div id='host'><input id='field' /></div>", BaseCss, 400, 200);

        var field = (InputElement)p.FindById("field")!;
        var expr = BindingExpression.Parse("{Binding Name, Mode=TwoWay}");
        engine.Bind(field, "Value", vm, expr);
        Assert.Equal("Initial", field.Value);

        field.Value = "UserTyped";
        Assert.Equal("UserTyped", vm.Name);

        p.Rerender();
        Assert.Equal("UserTyped", vm.Name);
        Assert.Equal("UserTyped", field.Value);
    }

    /// <summary>
    /// 8. OneTime binding — bind with Mode=OneTime, change source
    ///    → target does NOT update, even after rerender.
    /// </summary>
    [Fact]
    public void OneTimeBinding_SourceChange_DoesNotUpdateTarget()
    {
        var vm = new TestVM { Name = "Once" };
        var engine = new BindingEngine();

        using var p = HeadlessPipeline.Render(
            "<div id='host'><span id='label'></span></div>", BaseCss, 400, 200);

        var label = (TextElement)p.FindById("label")!;
        engine.Bind(label, "Text", vm, BindingExpression.Parse("{Binding Name, Mode=OneTime}"));
        Assert.Equal("Once", label.Text);

        vm.Name = "Modified";
        Assert.Equal("Once", label.Text);

        p.Rerender();
        Assert.Equal("Once", label.Text);
    }
}
