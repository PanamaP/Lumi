namespace Lumi.Core;

/// <summary>
/// Selects which color scheme the application uses.
/// </summary>
public enum ThemeMode
{
    /// <summary>Use the light color scheme.</summary>
    Light,

    /// <summary>Use the dark color scheme.</summary>
    Dark,

    /// <summary>Follow the operating system preference.</summary>
    System
}

/// <summary>
/// Manages light/dark theme switching and exposes CSS custom properties
/// that cascade through the element tree via the style resolver.
/// </summary>
public class ThemeManager
{
    private readonly object _lock = new();
    private ThemeMode _mode = ThemeMode.System;
    private bool _systemDarkMode;
    private bool _isDarkMode;
    private Element? _appliedRoot;
    private bool _isApplying;

    // ── Light palette ──────────────────────────────────────────────
    public static readonly Dictionary<string, string> LightVariables = new()
    {
        ["--bg-primary"]     = "#ffffff",
        ["--bg-secondary"]   = "#f1f5f9",
        ["--bg-tertiary"]    = "#e2e8f0",
        ["--text-primary"]   = "#0f172a",
        ["--text-secondary"] = "#475569",
        ["--text-muted"]     = "#94a3b8",
        ["--border-color"]   = "#cbd5e1",
        ["--accent"]         = "#3b82f6",
        ["--accent-hover"]   = "#2563eb",
        ["--error"]          = "#ef4444",
        ["--success"]        = "#22c55e",
        ["--warning"]        = "#f59e0b",
    };

    // ── Dark palette ───────────────────────────────────────────────
    public static readonly Dictionary<string, string> DarkVariables = new()
    {
        ["--bg-primary"]     = "#0f172a",
        ["--bg-secondary"]   = "#1e293b",
        ["--bg-tertiary"]    = "#334155",
        ["--text-primary"]   = "#f8fafc",
        ["--text-secondary"] = "#94a3b8",
        ["--text-muted"]     = "#64748b",
        ["--border-color"]   = "#475569",
        ["--accent"]         = "#3b82f6",
        ["--accent-hover"]   = "#60a5fa",
        ["--error"]          = "#f87171",
        ["--success"]        = "#4ade80",
        ["--warning"]        = "#fbbf24",
    };

    private static readonly HashSet<string> _allThemeKeys = new(
        LightVariables.Keys.Concat(DarkVariables.Keys),
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>Current theme mode setting(Light, Dark, or System).</summary>
    public ThemeMode Mode
    {
        get => _mode;
        set
        {
            Action<bool>? handler;
            bool isDark;
            Element? root;

            lock (_lock)
            {
                if (_mode == value) return;
                _mode = value;
                (handler, isDark, root) = RecalculateCore();
            }

            NotifyOutsideLock(handler, isDark, root);
        }
    }

    /// <summary>Resolved dark-mode flag (accounts for <see cref="ThemeMode.System"/>).</summary>
    public bool IsDarkMode => _isDarkMode;

    /// <summary>Raised whenever <see cref="IsDarkMode"/> changes. Passes the new value.</summary>
    public event Action<bool>? ThemeChanged;

    /// <summary>
    /// Returns the active variable set for the current theme.
    /// </summary>
    public IReadOnlyDictionary<string, string> CurrentVariables =>
        _isDarkMode ? DarkVariables : LightVariables;

    /// <summary>
    /// Sets the theme mode and recalculates the resolved dark-mode state.
    /// </summary>
    public void SetTheme(ThemeMode mode) => Mode = mode;

    /// <summary>
    /// Toggles between <see cref="ThemeMode.Light"/> and <see cref="ThemeMode.Dark"/>.
    /// If the current mode is <see cref="ThemeMode.System"/>, switches to the opposite of the resolved state.
    /// </summary>
    public void Toggle()
    {
        Action<bool>? handler;
        bool isDark;
        Element? root;

        lock (_lock)
        {
            _mode = _isDarkMode ? ThemeMode.Light : ThemeMode.Dark;
            (handler, isDark, root) = RecalculateCore();
        }

        NotifyOutsideLock(handler, isDark, root);
    }

    /// <summary>
    /// Updates the cached OS dark-mode preference. Call when the platform preference changes.
    /// </summary>
    public void SetSystemPreference(bool systemIsDark)
    {
        Action<bool>? handler = null;
        bool isDark = false;
        Element? root = null;

        lock (_lock)
        {
            if (_systemDarkMode == systemIsDark) return;
            _systemDarkMode = systemIsDark;
            if (_mode == ThemeMode.System)
                (handler, isDark, root) = RecalculateCore();
        }

        NotifyOutsideLock(handler, isDark, root);
    }

    /// <summary>
    /// Applies the current theme's CSS custom properties to <paramref name="root"/>
    /// via the <see cref="Element.ThemeVariables"/> dictionary so they resolve at
    /// stylesheet specificity. User stylesheets and inline styles can override them.
    /// </summary>
    public void ApplyTo(Element root)
    {
        _isApplying = true;
        try
        {
            _appliedRoot = root;
            var variables = _isDarkMode ? DarkVariables : LightVariables;

            // Store theme variables at stylesheet specificity instead of inline
            root.ThemeVariables = new Dictionary<string, string>(variables);

            // Strip any previously-injected theme variables from inline style
            var existing = StripThemeVariables(root.InlineStyle);
            root.InlineStyle = string.IsNullOrEmpty(existing) ? null : existing;
        }
        finally
        {
            _isApplying = false;
        }
    }

    // ── Private helpers ────────────────────────────────────────────

    /// <summary>
    /// Recalculates resolved dark-mode state. Must be called under <see cref="_lock"/>.
    /// Returns the event handler to invoke, the new value, and the root element to
    /// re-apply — all of which must be acted on <b>outside</b> the lock.
    /// </summary>
    private (Action<bool>? Handler, bool IsDark, Element? Root) RecalculateCore()
    {
        var newDark = _mode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark  => true,
            _               => _systemDarkMode
        };

        if (newDark == _isDarkMode)
            return (null, false, null);

        _isDarkMode = newDark;

        var handler = ThemeChanged;
        var root = (_appliedRoot != null && !_isApplying) ? _appliedRoot : null;

        return (handler, _isDarkMode, root);
    }

    /// <summary>
    /// Raises <see cref="ThemeChanged"/> and re-applies the theme to the root element
    /// outside the lock so subscribers cannot cause a deadlock.
    /// </summary>
    private void NotifyOutsideLock(Action<bool>? handler, bool isDark, Element? root)
    {
        handler?.Invoke(isDark);

        if (root != null)
        {
            ApplyTo(root);
            root.MarkDirty();
        }
    }

    /// <summary>
    /// Removes all <c>--theme-variable</c> declarations we previously injected,
    /// keeping any user-supplied inline styles intact.
    /// </summary>
    private static string StripThemeVariables(string? inlineStyle)
    {
        if (string.IsNullOrWhiteSpace(inlineStyle))
            return string.Empty;

        var kept = new List<string>();
        foreach (var decl in inlineStyle.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = decl.Trim();
            if (trimmed.Length == 0) continue;

            // Theme variables start with '--' and match one of our known keys
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx > 0)
            {
                var prop = trimmed[..colonIdx].Trim();
                if (_allThemeKeys.Contains(prop))
                    continue; // strip theme var
            }

            kept.Add(trimmed);
        }

        return string.Join("; ", kept);
    }
}
