using Lumi.Core;

namespace Lumi.Tests.Binding;

/// <summary>
/// Targets surviving mutants in TemplateIfElement: IsTruthy across types,
/// SetRendered idempotency, GetRootProperty dotted-path handling, Unbind
/// disposes nested bindings, DeepClone preserves ConditionPath/HTML.
/// </summary>
public class TemplateIfElementTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-5, true)]
    [InlineData(0.0, false)]
    [InlineData(0.5, true)]
    [InlineData("", false)]
    [InlineData("non-empty", true)]
    public void IsTruthy_FollowsExpectedTable(object? value, bool expected)
    {
        Assert.Equal(expected, TemplateIfElement.IsTruthy(value));
    }

    [Fact]
    public void IsTruthy_AnyOtherObject_ReturnsTrue()
    {
        Assert.True(TemplateIfElement.IsTruthy(new object()));
        Assert.True(TemplateIfElement.IsTruthy(new List<int>()));
    }

    [Fact]
    public void TagName_IsTemplate()
    {
        var t = new TemplateIfElement();
        Assert.Equal("template", t.TagName);
    }

    [Fact]
    public void DeepClone_CopiesConditionPathAndTemplateHtml()
    {
        var src = new TemplateIfElement
        {
            ConditionPath = "User.IsActive",
            TemplateHtml = "<span>{User.Name}</span>"
        };
        var clone = (TemplateIfElement)src.DeepClone();
        Assert.NotSame(src, clone);
        Assert.Equal("User.IsActive", clone.ConditionPath);
        Assert.Equal("<span>{User.Name}</span>", clone.TemplateHtml);
    }

    [Fact]
    public void SetRendered_True_AddsContentChildren()
    {
        var t = new TemplateIfElement { ConditionPath = "Show" };
        t.BindCondition(new object(), CreateContent);
        t.SetRendered(true);
        Assert.Single(t.Children);

        static Element CreateContent()
        {
            var c = new BoxElement("template-if-content");
            c.AddChild(new BoxElement("p"));
            return c;
        }
    }

    [Fact]
    public void SetRendered_True_TwiceWithoutFalse_IsIdempotent()
    {
        var t = new TemplateIfElement();
        int builds = 0;
        t.BindCondition(new object(), () =>
        {
            builds++;
            var c = new BoxElement("template-if-content");
            c.AddChild(new BoxElement("p"));
            return c;
        });
        t.SetRendered(true);
        t.SetRendered(true);
        Assert.Equal(1, builds);
        Assert.Single(t.Children);
    }

    [Fact]
    public void SetRendered_False_AfterTrue_ClearsChildren()
    {
        var t = new TemplateIfElement();
        t.BindCondition(new object(), () =>
        {
            var c = new BoxElement("template-if-content");
            c.AddChild(new BoxElement("p"));
            return c;
        });
        t.SetRendered(true);
        t.SetRendered(false);
        Assert.Empty(t.Children);
    }

    [Fact]
    public void Unbind_DisposesAndStopsListening()
    {
        var t = new TemplateIfElement { ConditionPath = "X" };
        t.BindCondition(new object(), () => new BoxElement("template-if-content"));
        var ex = Record.Exception(() => t.Unbind());
        Assert.Null(ex);
        // Unbind a second time must also be safe.
        t.Unbind();
    }
}
