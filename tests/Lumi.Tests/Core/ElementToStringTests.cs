using Lumi.Core;

namespace Lumi.Tests.Core;

/// <summary>
/// Targets surviving mutants in Element.ToString and event-handler paths.
/// </summary>
public class ElementToStringTests
{
    [Fact]
    public void ToString_Default_OnlyTagName()
    {
        var e = new BoxElement("div");
        Assert.Equal("<div>", e.ToString());
    }

    [Fact]
    public void ToString_WithId_IncludesIdAttribute()
    {
        var e = new BoxElement("section") { Id = "main" };
        Assert.Equal("<section id=\"main\">", e.ToString());
    }

    [Fact]
    public void ToString_WithClasses_IncludesSpaceJoinedClassAttribute()
    {
        var e = new BoxElement("div");
        e.Classes.Add("a");
        e.Classes.Add("b");
        Assert.Equal("<div class=\"a b\">", e.ToString());
    }

    [Fact]
    public void ToString_NoClasses_OmitsClassAttribute()
    {
        var e = new BoxElement("div");
        Assert.DoesNotContain("class=", e.ToString());
    }

    [Fact]
    public void ToString_NullId_OmitsIdAttribute()
    {
        var e = new BoxElement("div");
        Assert.DoesNotContain("id=", e.ToString());
    }

    [Fact]
    public void ToString_IdAndClasses_BothIncluded_OrderIdFirstThenClass()
    {
        var e = new BoxElement("button") { Id = "submit" };
        e.Classes.Add("primary");
        Assert.Equal("<button id=\"submit\" class=\"primary\">", e.ToString());
    }

    [Fact]
    public void RemoveEventHandler_UnknownEvent_DoesNotThrow()
    {
        var e = new BoxElement("div");
        var ex = Record.Exception(() => e.Off("click", (_, _) => { }));
        Assert.Null(ex);
    }

    [Fact]
    public void On_Then_Off_RemovesHandler()
    {
        var e = new BoxElement("div");
        int calls = 0;
        RoutedEventHandler handler = (_, _) => calls++;
        e.On("click", handler);
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, e);
        Assert.Equal(1, calls);

        e.Off("click", handler);
        EventDispatcher.Dispatch(new RoutedMouseEvent("click") { Button = MouseButton.Left }, e);
        Assert.Equal(1, calls);
    }
}
