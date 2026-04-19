using Lumi.Core;

namespace Lumi.Tests.Core;

/// <summary>
/// Targets surviving mutants in Clipboard: Initialize null guards, GetText/SetText
/// delegate routing, IsInitialized state, Reset clears delegates.
/// </summary>
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
        InvokeInitialize(() => "stored", _ => { });
        Assert.True(Clipboard.IsInitialized);
        Assert.Equal("stored", Clipboard.GetText());
    }

    [Fact]
    public void Initialize_SetText_RoutesToSetterDelegate()
    {
        string? captured = null;
        InvokeInitialize(() => null, s => captured = s);
        Clipboard.SetText("payload");
        Assert.Equal("payload", captured);
    }

    [Fact]
    public void Reset_ClearsDelegates_AndInvalidatesIsInitialized()
    {
        InvokeInitialize(() => "x", _ => { });
        Assert.True(Clipboard.IsInitialized);
        Clipboard.ResetForTesting();
        Assert.False(Clipboard.IsInitialized);
        Assert.Null(Clipboard.GetText());
    }

    [Fact]
    public void Initialize_OverwritesDelegates_OnReinitialize()
    {
        InvokeInitialize(() => "first", _ => { });
        InvokeInitialize(() => "second", _ => { });
        Assert.Equal("second", Clipboard.GetText());
    }

    private static void InvokeInitialize(Func<string?> getter, Action<string> setter)
    {
        var method = typeof(Clipboard).GetMethod(
            "Initialize",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        method!.Invoke(null, [getter, setter]);
    }
}
