using System.ComponentModel;
using Lumi.Core;
using Lumi.Core.Binding;

namespace Lumi.Tests.Binding;

/// <summary>
/// Targets surviving mutants in BindingEngine: ResolvePath segments, SetSourceValue
/// nested writes, ConvertValue type coercion, OneWay/TwoWay/OneTime modes,
/// PropertyChanged subscribe/unsubscribe, FallbackValue, ClearAll.
/// </summary>
public class BindingEngineTests
{
    private sealed class Person : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }

        private int _age;
        public int Age
        {
            get => _age;
            set { _age = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Age))); }
        }

        private Person? _spouse;
        public Person? Spouse
        {
            get => _spouse;
            set { _spouse = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Spouse))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class Plain
    {
        public string Name { get; set; } = "Plain";
        public Plain? Inner { get; set; }
        public int Number { get; set; }
        public string ReadOnly { get; } = "ro";
    }

    // ---------------- ResolvePath ----------------

    [Fact]
    public void ResolvePath_NullSource_ReturnsNull()
    {
        Assert.Null(BindingEngine.ResolvePath(null, "Anything"));
    }

    [Fact]
    public void ResolvePath_EmptyPath_ReturnsSourceItself()
    {
        var p = new Plain();
        Assert.Same(p, BindingEngine.ResolvePath(p, ""));
    }

    [Fact]
    public void ResolvePath_SingleSegment_ReadsProperty()
    {
        var p = new Plain { Name = "X" };
        Assert.Equal("X", BindingEngine.ResolvePath(p, "Name"));
    }

    [Fact]
    public void ResolvePath_NestedSegments_NavigateAcrossDots()
    {
        var p = new Plain { Inner = new Plain { Name = "deep" } };
        Assert.Equal("deep", BindingEngine.ResolvePath(p, "Inner.Name"));
    }

    [Fact]
    public void ResolvePath_NullMidway_ReturnsNull()
    {
        var p = new Plain { Inner = null };
        Assert.Null(BindingEngine.ResolvePath(p, "Inner.Name"));
    }

    [Fact]
    public void ResolvePath_UnknownProperty_ReturnsNull()
    {
        var p = new Plain();
        Assert.Null(BindingEngine.ResolvePath(p, "DoesNotExist"));
    }

    [Fact]
    public void ResolvePath_MultiLevelMissing_ReturnsNull()
    {
        var p = new Plain { Inner = new Plain() };
        Assert.Null(BindingEngine.ResolvePath(p, "Inner.Nope"));
    }

    // ---------------- Bind: initial push & OneWay ----------------

    [Fact]
    public void Bind_OneWay_InitiallyPushesSourceValueIntoTextElement()
    {
        var person = new Person { Name = "Alice" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", person, new BindingExpression { Path = "Name", Mode = BindingMode.OneWay });
        Assert.Equal("Alice", text.Text);
    }

    [Fact]
    public void Bind_OneWay_PropagatesPropertyChanged()
    {
        var person = new Person { Name = "A" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", person, new BindingExpression { Path = "Name" });
        person.Name = "B";
        Assert.Equal("B", text.Text);
    }

    [Fact]
    public void Bind_OneWay_IgnoresUnrelatedPropertyChange()
    {
        var person = new Person { Name = "A", Age = 1 };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", person, new BindingExpression { Path = "Name" });
        person.Age = 99; // unrelated property
        Assert.Equal("A", text.Text);
    }

    [Fact]
    public void Bind_OneWay_NestedPath_RootChangeRefreshes()
    {
        var p = new Person { Name = "outer", Spouse = new Person { Name = "inner" } };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Spouse.Name" });
        Assert.Equal("inner", text.Text);
        // Replacing the whole spouse triggers PropertyChanged("Spouse") whose root is "Spouse" -> refreshes
        p.Spouse = new Person { Name = "new" };
        Assert.Equal("new", text.Text);
    }

    [Fact]
    public void Bind_OneTime_DoesNotSubscribe()
    {
        var p = new Person { Name = "A" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name", Mode = BindingMode.OneTime });
        Assert.Equal("A", text.Text);
        p.Name = "B";
        Assert.Equal("A", text.Text); // no update
    }

    [Fact]
    public void Bind_NonNotifyingSource_PushesInitialButDoesNotSubscribe()
    {
        var p = new Plain { Name = "init" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        Assert.Equal("init", text.Text);
        p.Name = "changed";
        Assert.Equal("init", text.Text);
    }

    // ---------------- Fallback ----------------

    [Fact]
    public void Bind_NullValue_UsesFallback()
    {
        var p = new Plain { Name = null! };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name", FallbackValue = "FB" });
        Assert.Equal("FB", text.Text);
    }

    [Fact]
    public void Bind_NullValue_NoFallback_BecomesEmptyString()
    {
        var p = new Plain { Name = null! };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        Assert.Equal("", text.Text);
    }

    // ---------------- TwoWay binding from input back to source ----------------

    [Fact]
    public void Bind_TwoWay_InputChange_WritesBackToSource()
    {
        var p = new Person { Name = "A" };
        var input = new InputElement { Value = "" };
        var engine = new BindingEngine();
        engine.Bind(input, "Value", p, new BindingExpression { Path = "Name", Mode = BindingMode.TwoWay });
        // Initial pushed
        Assert.Equal("A", input.Value);
        // Push from input
        input.Value = "Z";
        Assert.Equal("Z", p.Name);
    }

    [Fact]
    public void Bind_OneWay_InputChange_DoesNotWriteBack()
    {
        var p = new Person { Name = "A" };
        var input = new InputElement { Value = "" };
        var engine = new BindingEngine();
        engine.Bind(input, "Value", p, new BindingExpression { Path = "Name", Mode = BindingMode.OneWay });
        input.Value = "Z";
        Assert.Equal("A", p.Name);
    }

    [Fact]
    public void Bind_TwoWay_NestedPath_UpdatesNestedSource()
    {
        var outer = new Person { Spouse = new Person { Name = "spouse" } };
        var input = new InputElement { Value = "" };
        var engine = new BindingEngine();
        engine.Bind(input, "Value", outer, new BindingExpression { Path = "Spouse.Name", Mode = BindingMode.TwoWay });
        input.Value = "edited";
        Assert.Equal("edited", outer.Spouse!.Name);
    }

    // ---------------- ClearAll ----------------

    [Fact]
    public void ClearAll_StopsForwardPropagation()
    {
        var p = new Person { Name = "A" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        engine.ClearAll();
        p.Name = "B";
        Assert.Equal("A", text.Text);
    }

    [Fact]
    public void ClearAll_StopsTwoWayReverse()
    {
        var p = new Person { Name = "A" };
        var input = new InputElement { Value = "" };
        var engine = new BindingEngine();
        engine.Bind(input, "Value", p, new BindingExpression { Path = "Name", Mode = BindingMode.TwoWay });
        engine.ClearAll();
        input.Value = "Z";
        Assert.Equal("A", p.Name);
    }

    [Fact]
    public void ClearAll_AllowsReBind()
    {
        var p = new Person { Name = "A" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        engine.ClearAll();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        p.Name = "C";
        Assert.Equal("C", text.Text);
    }

    // ---------------- UpdateAll ----------------

    [Fact]
    public void UpdateAll_RePushesAllBindings()
    {
        var p = new Plain { Name = "A" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        // Plain doesn't notify -- silently change the source
        p.Name = "B";
        Assert.Equal("A", text.Text);
        engine.UpdateAll();
        Assert.Equal("B", text.Text);
    }

    [Fact]
    public void UpdateAll_SkipsInactiveBindings()
    {
        var p = new Plain { Name = "A" };
        var text = new TextElement("");
        var engine = new BindingEngine();
        engine.Bind(text, "Text", p, new BindingExpression { Path = "Name" });
        engine.ClearAll();
        // ClearAll wipes the list; UpdateAll shouldn't throw or update
        text.Text = "untouched";
        engine.UpdateAll();
        Assert.Equal("untouched", text.Text);
    }

    // ---------------- Bind: argument validation ----------------

    [Fact]
    public void Bind_NullTarget_Throws()
    {
        var engine = new BindingEngine();
        Assert.Throws<ArgumentNullException>(() =>
            engine.Bind(null!, "Text", new Plain(), new BindingExpression { Path = "Name" }));
    }

    [Fact]
    public void Bind_NullSource_Throws()
    {
        var engine = new BindingEngine();
        Assert.Throws<ArgumentNullException>(() =>
            engine.Bind(new TextElement(""), "Text", null!, new BindingExpression { Path = "Name" }));
    }

    [Fact]
    public void Bind_NullExpression_Throws()
    {
        var engine = new BindingEngine();
        Assert.Throws<ArgumentNullException>(() =>
            engine.Bind(new TextElement(""), "Text", new Plain(), null!));
    }

    // ---------------- SetTargetValue: various target shapes ----------------

    [Fact]
    public void Bind_InlineStyle_AssignsString()
    {
        var p = new Plain { Name = "color: red;" };
        var div = new BoxElement("div");
        var engine = new BindingEngine();
        engine.Bind(div, "InlineStyle", p, new BindingExpression { Path = "Name" });
        Assert.Equal("color: red;", div.InlineStyle);
    }

    [Fact]
    public void Bind_ImageSource_AssignsString()
    {
        var p = new Plain { Name = "logo.png" };
        var img = new ImageElement();
        var engine = new BindingEngine();
        engine.Bind(img, "Source", p, new BindingExpression { Path = "Name" });
        Assert.Equal("logo.png", img.Source);
    }

    [Fact]
    public void Bind_GenericReflectionProperty_Assigns()
    {
        var p = new Plain { Number = 42 };
        var box = new BoxElement("div");
        var engine = new BindingEngine();
        // TabIndex is an int property exposed by Element; tests reflection + ConvertValue.
        engine.Bind(box, "TabIndex", p, new BindingExpression { Path = "Number" });
        Assert.Equal(42, box.TabIndex);
    }

    [Fact]
    public void Bind_GenericReflection_UnknownProperty_DoesNotThrow()
    {
        var p = new Plain { Name = "x" };
        var box = new BoxElement("div");
        var engine = new BindingEngine();
        var ex = Record.Exception(() =>
            engine.Bind(box, "ThisDoesNotExist", p, new BindingExpression { Path = "Name" }));
        Assert.Null(ex);
    }
}
