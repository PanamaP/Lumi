namespace Lumi.Core;

/// <summary>
/// Provides clipboard access from Core without a direct platform dependency.
/// Initialized once by the host (e.g. LumiApp) at startup.
/// </summary>
public static class Clipboard
{
    private static Func<string?>? _getText;
    private static Action<string>? _setText;
    private static bool _initialized;

    /// <summary>
    /// Retrieves text from the OS clipboard. Returns null when unavailable.
    /// </summary>
    public static string? GetText() => _getText?.Invoke();

    /// <summary>
    /// Places text on the OS clipboard.
    /// </summary>
    public static void SetText(string text) => _setText?.Invoke(text);

    /// <summary>
    /// Initializes the clipboard with platform-specific implementations. Can only be called once.
    /// </summary>
    internal static void Initialize(Func<string?> getText, Action<string> setText)
    {
        if (_initialized)
            throw new InvalidOperationException("Clipboard has already been initialized.");
        _getText = getText ?? throw new ArgumentNullException(nameof(getText));
        _setText = setText ?? throw new ArgumentNullException(nameof(setText));
        _initialized = true;
    }

    /// <summary>Reset for test isolation.</summary>
    internal static void ResetForTesting()
    {
        _getText = null;
        _setText = null;
        _initialized = false;
    }
}
