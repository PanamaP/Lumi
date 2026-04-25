using Lumi.Core;

namespace Lumi.Tests.Core;

/// <summary>
/// Targets surviving mutants in Clipboard: Initialize null guards, GetText/SetText
/// delegate routing, IsInitialized state, Reset clears delegates.
/// </summary>
/// <remarks>
/// Placed in the non-parallel "Lifecycle" collection because <see cref="Clipboard"/>
/// is process-wide static state; running concurrently with other clipboard-using
/// tests (e.g. ApplicationLifecycleTests) causes race conditions.
/// </remarks>
[Collection("Lifecycle")]
public class ClipboardTests : IDisposable
{
    public ClipboardTests() => Clipboard.ResetForTesting();
    public void Dispose() => Clipboard.ResetForTesting();

    [Fact]
    public void Default_NotInitialized_GetReturnsNull_SetIsNoOp()
    {
        Assert.False(Clipboard.IsInitialized);
        Assert.Null(Clipboard.GetText());
        var ex = Record.Exception(() => Clipboard.SetText("hello"));
        Assert.Null(ex);
    }

    [Fact]
    public void SetText_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Clipboard.SetText(null!));
    }

    [Fact]
    public void Initialize_GetText_ReturnsValueFromDelegate()
    {
        Clipboard.Initialize(() => "stored", _ => { });
        Assert.True(Clipboard.IsInitialized);
        Assert.Equal("stored", Clipboard.GetText());
    }

    [Fact]
    public void Initialize_SetText_RoutesToSetterDelegate()
    {
        string? captured = null;
        Clipboard.Initialize(() => null, s => captured = s);
        Clipboard.SetText("payload");
        Assert.Equal("payload", captured);
    }

    [Fact]
    public void Reset_ClearsDelegates_AndInvalidatesIsInitialized()
    {
        Clipboard.Initialize(() => "x", _ => { });
        Assert.True(Clipboard.IsInitialized);
        Clipboard.ResetForTesting();
        Assert.False(Clipboard.IsInitialized);
        Assert.Null(Clipboard.GetText());
    }

    [Fact]
    public void Initialize_OverwritesDelegates_OnReinitialize()
    {
        Clipboard.Initialize(() => "first", _ => { });
        Clipboard.Initialize(() => "second", _ => { });
        Assert.Equal("second", Clipboard.GetText());
    }
}
