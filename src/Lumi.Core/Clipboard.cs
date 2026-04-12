namespace Lumi.Core;

/// <summary>
/// Provides clipboard access from Core without a direct platform dependency.
/// Initialized by the host (e.g. LumiApp) at startup; safe to re-initialize
/// across multiple <see cref="LumiApp.Run"/> calls in the same process.
/// </summary>
public static class Clipboard
{
    private static readonly object _lock = new();
    private static Func<string?>? _getText;
    private static Action<string>? _setText;

    /// <summary>
    /// Whether the clipboard has been initialized with platform delegates.
    /// </summary>
    public static bool IsInitialized
    {
        get { lock (_lock) { return _getText != null && _setText != null; } }
    }

    /// <summary>
    /// Retrieves text from the OS clipboard. Returns null when unavailable.
    /// </summary>
    public static string? GetText()
    {
        Func<string?>? getter;
        lock (_lock) { getter = _getText; }
        return getter?.Invoke();
    }

    /// <summary>
    /// Places text on the OS clipboard.
    /// </summary>
    public static void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Action<string>? setter;
        lock (_lock) { setter = _setText; }
        setter?.Invoke(text);
    }

    /// <summary>
    /// Initializes the clipboard with platform-specific implementations.
    /// Safe to call multiple times — subsequent calls update the delegates
    /// so that <c>LumiApp.Run</c> can be invoked more than once
    /// in the same process (e.g. login window → main window).
    /// </summary>
    internal static void Initialize(Func<string?> getText, Action<string> setText)
    {
        lock (_lock)
        {
            _getText = getText ?? throw new ArgumentNullException(nameof(getText));
            _setText = setText ?? throw new ArgumentNullException(nameof(setText));
        }
    }

    /// <summary>
    /// Resets clipboard state so the next <see cref="Initialize"/> starts fresh.
    /// Called by <c>LumiApp</c> during disposal.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _getText = null;
            _setText = null;
        }
    }

    /// <summary>Reset for test isolation.</summary>
    internal static void ResetForTesting() => Reset();
}
