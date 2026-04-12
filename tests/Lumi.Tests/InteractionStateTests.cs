using Lumi.Core;
using Lumi.Input;

namespace Lumi.Tests;

public class InteractionStateTests
{
    // --- Hover state ---

    [Fact]
    public void IsHovered_ReturnsFalse_WhenNothingHovered()
    {
        var state = new InteractionState();
        var element = new BoxElement("div");

        Assert.False(state.IsHovered(element));
        Assert.Null(state.HoveredElement);
    }

    [Fact]
    public void SetHovered_TracksElement()
    {
        var state = new InteractionState();
        var element = new BoxElement("button");

        state.SetHovered(element);

        Assert.Same(element, state.HoveredElement);
        Assert.True(state.IsHovered(element));
    }

    [Fact]
    public void SetHovered_TracksParentPath()
    {
        var state = new InteractionState();
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        var grandchild = new BoxElement("button");
        root.AddChild(child);
        child.AddChild(grandchild);

        state.SetHovered(grandchild);

        Assert.True(state.IsHovered(grandchild));
        Assert.True(state.IsHovered(child));
        Assert.True(state.IsHovered(root));
    }

    [Fact]
    public void SetHovered_ClearsPreviousPath()
    {
        var state = new InteractionState();
        var parent1 = new BoxElement("div");
        var child1 = new BoxElement("button");
        parent1.AddChild(child1);

        var parent2 = new BoxElement("div");
        var child2 = new BoxElement("button");
        parent2.AddChild(child2);

        state.SetHovered(child1);
        Assert.True(state.IsHovered(parent1));

        state.SetHovered(child2);
        Assert.False(state.IsHovered(parent1));
        Assert.False(state.IsHovered(child1));
        Assert.True(state.IsHovered(parent2));
        Assert.True(state.IsHovered(child2));
    }

    [Fact]
    public void SetHovered_Null_ClearsAll()
    {
        var state = new InteractionState();
        var element = new BoxElement("div");
        state.SetHovered(element);
        Assert.True(state.IsHovered(element));

        state.SetHovered(null);

        Assert.Null(state.HoveredElement);
        Assert.False(state.IsHovered(element));
    }

    [Fact]
    public void SetHovered_SameElement_NoOp()
    {
        var state = new InteractionState();
        var element = new BoxElement("div");

        state.SetHovered(element);
        state.SetHovered(element); // should not change anything

        Assert.Same(element, state.HoveredElement);
        Assert.True(state.IsHovered(element));
    }

    // --- Active state ---

    [Fact]
    public void IsActive_ReturnsFalse_WhenNothingActive()
    {
        var state = new InteractionState();
        var element = new BoxElement("button");

        Assert.False(state.IsActive(element));
        Assert.Null(state.ActiveElement);
    }

    [Fact]
    public void SetActive_TracksElement()
    {
        var state = new InteractionState();
        var element = new BoxElement("button");

        state.SetActive(element);

        Assert.Same(element, state.ActiveElement);
        Assert.True(state.IsActive(element));
    }

    [Fact]
    public void SetActive_DoesNotTrackParentPath()
    {
        var state = new InteractionState();
        var parent = new BoxElement("div");
        var child = new BoxElement("button");
        parent.AddChild(child);

        state.SetActive(child);

        Assert.True(state.IsActive(child));
        Assert.False(state.IsActive(parent)); // Active is direct match only
    }

    [Fact]
    public void SetActive_Null_ClearsActive()
    {
        var state = new InteractionState();
        var element = new BoxElement("button");
        state.SetActive(element);

        state.SetActive(null);

        Assert.Null(state.ActiveElement);
        Assert.False(state.IsActive(element));
    }

    // --- Focus state ---

    [Fact]
    public void IsFocused_ReturnsFalse_WhenNothingFocused()
    {
        var state = new InteractionState();
        var element = new BoxElement("input");

        Assert.False(state.IsFocused(element));
        Assert.Null(state.FocusedElement);
    }

    [Fact]
    public void SetFocused_TracksElement()
    {
        var state = new InteractionState();
        var element = new BoxElement("input");

        state.SetFocused(element);

        Assert.Same(element, state.FocusedElement);
        Assert.True(state.IsFocused(element));
    }

    [Fact]
    public void SetFocused_TracksParentPath()
    {
        var state = new InteractionState();
        var root = new BoxElement("div");
        var child = new BoxElement("div");
        var input = new BoxElement("input");
        root.AddChild(child);
        child.AddChild(input);

        state.SetFocused(input);

        Assert.True(state.IsFocused(input));
        Assert.True(state.IsFocused(child));
        Assert.True(state.IsFocused(root));
    }

    [Fact]
    public void SetFocused_ClearsPreviousPath()
    {
        var state = new InteractionState();
        var parent1 = new BoxElement("div");
        var input1 = new BoxElement("input");
        parent1.AddChild(input1);

        var parent2 = new BoxElement("div");
        var input2 = new BoxElement("input");
        parent2.AddChild(input2);

        state.SetFocused(input1);
        Assert.True(state.IsFocused(parent1));

        state.SetFocused(input2);
        Assert.False(state.IsFocused(parent1));
        Assert.False(state.IsFocused(input1));
        Assert.True(state.IsFocused(parent2));
        Assert.True(state.IsFocused(input2));
    }

    [Fact]
    public void SetFocused_Null_ClearsAll()
    {
        var state = new InteractionState();
        var element = new BoxElement("input");
        state.SetFocused(element);

        state.SetFocused(null);

        Assert.Null(state.FocusedElement);
        Assert.False(state.IsFocused(element));
    }

    [Fact]
    public void SetFocused_SameElement_NoOp()
    {
        var state = new InteractionState();
        var element = new BoxElement("input");

        state.SetFocused(element);
        state.SetFocused(element); // should not change anything

        Assert.Same(element, state.FocusedElement);
    }

    // --- Independent states ---

    [Fact]
    public void States_AreIndependent()
    {
        var state = new InteractionState();
        var hoveredEl = new BoxElement("div");
        var activeEl = new BoxElement("button");
        var focusedEl = new BoxElement("input");

        state.SetHovered(hoveredEl);
        state.SetActive(activeEl);
        state.SetFocused(focusedEl);

        Assert.True(state.IsHovered(hoveredEl));
        Assert.False(state.IsActive(hoveredEl));
        Assert.False(state.IsFocused(hoveredEl));

        Assert.True(state.IsActive(activeEl));
        Assert.False(state.IsHovered(activeEl));
        Assert.False(state.IsFocused(activeEl));

        Assert.True(state.IsFocused(focusedEl));
        Assert.False(state.IsHovered(focusedEl));
        Assert.False(state.IsActive(focusedEl));
    }

    // --- Clear ---

    [Fact]
    public void Clear_ResetsAllState()
    {
        var state = new InteractionState();
        var parent = new BoxElement("div");
        var child = new BoxElement("button");
        parent.AddChild(child);

        state.SetHovered(child);
        state.SetActive(child);
        state.SetFocused(child);

        state.Clear();

        Assert.Null(state.HoveredElement);
        Assert.Null(state.ActiveElement);
        Assert.Null(state.FocusedElement);
        Assert.False(state.IsHovered(child));
        Assert.False(state.IsHovered(parent));
        Assert.False(state.IsActive(child));
        Assert.False(state.IsFocused(child));
        Assert.False(state.IsFocused(parent));
    }
}
