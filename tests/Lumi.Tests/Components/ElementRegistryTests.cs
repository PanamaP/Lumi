using Lumi.Core;

namespace Lumi.Tests.Components;

/// <summary>
/// Targets ElementRegistry surviving mutants: every built-in tag must produce the right
/// element subtype with the right TagName, custom registration overrides previous entries,
/// and lookups are case-insensitive.
/// </summary>
public class ElementRegistryTests
{
    [Theory]
    [InlineData("div")]
    [InlineData("section")]
    [InlineData("nav")]
    [InlineData("header")]
    [InlineData("footer")]
    [InlineData("main")]
    [InlineData("aside")]
    [InlineData("article")]
    [InlineData("ul")]
    [InlineData("ol")]
    [InlineData("li")]
    [InlineData("h1")]
    [InlineData("h2")]
    [InlineData("h3")]
    [InlineData("h4")]
    [InlineData("h5")]
    [InlineData("h6")]
    [InlineData("p")]
    [InlineData("a")]
    [InlineData("button")]
    public void Create_BuiltInBoxTag_ReturnsBoxElementWithMatchingTagName(string tag)
    {
        var el = ElementRegistry.Create(tag);
        Assert.IsType<BoxElement>(el);
        Assert.Equal(tag, el.TagName);
    }

    [Fact]
    public void Create_Span_ReturnsTextElement()
    {
        var el = ElementRegistry.Create("span");
        Assert.IsType<TextElement>(el);
    }

    [Fact]
    public void Create_Img_ReturnsImageElement()
    {
        var el = ElementRegistry.Create("img");
        Assert.IsType<ImageElement>(el);
    }

    [Fact]
    public void Create_Input_ReturnsInputElement()
    {
        var el = ElementRegistry.Create("input");
        Assert.IsType<InputElement>(el);
    }

    [Fact]
    public void Create_UnregisteredTag_FallsBackToBoxElementWithThatTagName()
    {
        var el = ElementRegistry.Create("custom-thing-xyz");
        Assert.IsType<BoxElement>(el);
        Assert.Equal("custom-thing-xyz", el.TagName);
    }

    [Fact]
    public void IsRegistered_BuiltInTags_ReturnsTrue()
    {
        Assert.True(ElementRegistry.IsRegistered("div"));
        Assert.True(ElementRegistry.IsRegistered("span"));
        Assert.True(ElementRegistry.IsRegistered("img"));
        Assert.True(ElementRegistry.IsRegistered("input"));
        Assert.True(ElementRegistry.IsRegistered("button"));
    }

    [Fact]
    public void IsRegistered_UnknownTag_ReturnsFalse()
    {
        Assert.False(ElementRegistry.IsRegistered("definitely-not-a-tag-zzz"));
    }

    [Fact]
    public void IsRegistered_IsCaseInsensitive()
    {
        Assert.True(ElementRegistry.IsRegistered("DIV"));
        Assert.True(ElementRegistry.IsRegistered("Span"));
    }

    [Fact]
    public void Create_IsCaseInsensitiveLookup()
    {
        var el = ElementRegistry.Create("DIV");
        Assert.IsType<BoxElement>(el);
    }

    [Fact]
    public void Register_FactoryOverridesExistingTag()
    {
        // Use a per-test GUID so we never permanently mutate the global registry
        // in a way that can leak across tests / parallel runs.
        var sentinelTag = "registry-test-tag-" + Guid.NewGuid().ToString("N");
        int callCount = 0;
        ElementRegistry.Register(sentinelTag, () =>
        {
            callCount++;
            return new TextElement("hello");
        });

        Assert.True(ElementRegistry.IsRegistered(sentinelTag));
        var first = ElementRegistry.Create(sentinelTag);
        Assert.IsType<TextElement>(first);
        Assert.Equal("hello", ((TextElement)first).Text);
        Assert.Equal(1, callCount);

        // Re-registering replaces the factory.
        ElementRegistry.Register(sentinelTag, () => new BoxElement("override"));
        var second = ElementRegistry.Create(sentinelTag);
        Assert.IsType<BoxElement>(second);
        Assert.Equal("override", second.TagName);
        // Old factory should not have been called again.
        Assert.Equal(1, callCount);
    }

    private sealed class CustomElement : Element
    {
        public override string TagName => "custom";
        protected override Element CreateCloneInstance() => new CustomElement();
    }

    [Fact]
    public void RegisterGeneric_CreatesNewInstance_OnEachCall()
    {
        var tag = "registry-test-generic-1";
        ElementRegistry.Register<CustomElement>(tag);
        var a = ElementRegistry.Create(tag);
        var b = ElementRegistry.Create(tag);
        Assert.IsType<CustomElement>(a);
        Assert.IsType<CustomElement>(b);
        Assert.NotSame(a, b);
    }

    [Fact]
    public void Create_ReturnsFreshInstance_EachCall()
    {
        var a = ElementRegistry.Create("div");
        var b = ElementRegistry.Create("div");
        Assert.NotSame(a, b);
    }
}
