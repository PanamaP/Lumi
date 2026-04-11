namespace Lumi.Core;

/// <summary>
/// Provides clipboard access from Core without a direct platform dependency.
/// Delegates are wired up by the host (e.g. LumiApp) at startup.
/// </summary>
public static class Clipboard
{
    /// <summary>
    /// Retrieves text from the OS clipboard. Returns null when unavailable.
    /// </summary>
    public static Func<string?>? GetText { get; set; }

    /// <summary>
    /// Places text on the OS clipboard.
    /// </summary>
    public static Action<string>? SetText { get; set; }
}
