using Lumi.Core;
using Lumi.Styling;
using System.Collections.Concurrent;

namespace Lumi;

/// <summary>
/// Watches HTML and CSS files for changes and queues reload actions
/// to be applied on the main thread during the frame loop.
/// Uses timestamp polling (reliable across all editors) with
/// FileSystemWatcher as an accelerator for instant response.
/// </summary>
public sealed class HotReload : IDisposable
{
    private readonly Window _window;
    private readonly string? _htmlPath;
    private readonly string? _cssPath;
    private FileSystemWatcher? _htmlWatcher;
    private FileSystemWatcher? _cssWatcher;
    private readonly ConcurrentQueue<Action> _pendingActions = new();

    /// <summary>
    /// Set to true when HTML is reloaded (element tree replaced).
    /// Cleared after the app reads it.
    /// </summary>
    public bool HtmlWasReloaded { get; set; }
    private Timer? _htmlDebounce;
    private Timer? _cssDebounce;
    private Timer? _pollTimer;
    private readonly object _debounceLock = new();
    private bool _disposed;

    // Content-based change detection (immune to timestamp tunneling)
    private int _lastHtmlHash;
    private int _lastCssHash;

    private const int DebounceMs = 200;
    private const int PollIntervalMs = 500;
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 50;

    public HotReload(Window window, string? htmlPath, string? cssPath)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _htmlPath = htmlPath != null ? Path.GetFullPath(htmlPath) : null;
        _cssPath = cssPath != null ? Path.GetFullPath(cssPath) : null;

        // Snapshot initial content hashes
        _lastHtmlHash = GetContentHash(_htmlPath);
        _lastCssHash = GetContentHash(_cssPath);
    }

    /// <summary>
    /// Begin watching files for changes.
    /// </summary>
    public void Start()
    {
        if (_htmlPath != null && File.Exists(_htmlPath))
            _htmlWatcher = CreateWatcher(_htmlPath, OnHtmlChanged);

        if (_cssPath != null && File.Exists(_cssPath))
            _cssWatcher = CreateWatcher(_cssPath, OnCssChanged);

        // Polling fallback — checks file timestamps every 500ms
        _pollTimer = new Timer(_ => PollForChanges(), null, PollIntervalMs, PollIntervalMs);
    }

    /// <summary>
    /// Stop watching files for changes.
    /// </summary>
    public void Stop()
    {
        DisposeWatcher(ref _htmlWatcher);
        DisposeWatcher(ref _cssWatcher);
        DisposeDebounce(ref _htmlDebounce);
        DisposeDebounce(ref _cssDebounce);
        DisposeDebounce(ref _pollTimer);
    }

    /// <summary>
    /// Returns true if there are pending changes to apply.
    /// </summary>
    public bool HasPendingChanges => !_pendingActions.IsEmpty;

    /// <summary>
    /// Apply all pending reload actions. Call this on the main/UI thread
    /// during the frame loop.
    /// </summary>
    public void ApplyPendingChanges()
    {
        while (_pendingActions.TryDequeue(out var action))
        {
            action();
        }
    }

    /// <summary>
    /// Polls file content hashes to detect changes that FileSystemWatcher might miss.
    /// This is the primary mechanism — immune to editor-specific save strategies
    /// (atomic rename, safe write, timestamp tunneling).
    /// </summary>
    private void PollForChanges()
    {
        if (_disposed) return;

        if (_htmlPath != null)
        {
            var hash = GetContentHash(_htmlPath);
            if (hash != _lastHtmlHash)
            {
                _lastHtmlHash = hash;
                QueueHtmlReload();
            }
        }

        if (_cssPath != null)
        {
            var hash = GetContentHash(_cssPath);
            if (hash != _lastCssHash)
            {
                _lastCssHash = hash;
                QueueCssReload();
            }
        }
    }

    private static int GetContentHash(string? path)
    {
        if (path == null) return 0;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().GetHashCode();
        }
        catch { return 0; }
    }

    private static FileSystemWatcher CreateWatcher(string filePath, FileSystemEventHandler handler)
    {
        var dir = Path.GetDirectoryName(filePath)!;
        var name = Path.GetFileName(filePath);
        var watcher = new FileSystemWatcher(dir, name)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                         | NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        watcher.Changed += handler;
        watcher.Created += handler;
        watcher.Renamed += (s, e) => handler(s, new FileSystemEventArgs(WatcherChangeTypes.Changed, dir, name));
        return watcher;
    }

    private void OnHtmlChanged(object sender, FileSystemEventArgs e)
    {
        lock (_debounceLock)
        {
            _htmlDebounce?.Dispose();
            _htmlDebounce = new Timer(_ =>
            {
                _lastHtmlHash = GetContentHash(_htmlPath);
                QueueHtmlReload();
            }, null, DebounceMs, Timeout.Infinite);
        }
    }

    private void OnCssChanged(object sender, FileSystemEventArgs e)
    {
        lock (_debounceLock)
        {
            _cssDebounce?.Dispose();
            _cssDebounce = new Timer(_ =>
            {
                _lastCssHash = GetContentHash(_cssPath);
                QueueCssReload();
            }, null, DebounceMs, Timeout.Infinite);
        }
    }

    private void QueueHtmlReload()
    {
        if (_htmlPath == null) return;

        var content = ReadFileWithRetry(_htmlPath);
        if (content == null) return;

        _pendingActions.Enqueue(() =>
        {
            var newRoot = HtmlTemplateParser.Parse(content);
            _window.Root = newRoot;
            _window.Root.MarkDirty();
            HtmlWasReloaded = true;
        });
    }

    internal void QueueCssReload()
    {
        if (_cssPath == null) return;

        var content = ReadFileWithRetry(_cssPath);
        if (content == null) return;

        _pendingActions.Enqueue(() =>
        {
            var newSheet = CssParser.Parse(content);
            _window.StyleResolver.ClearStyleSheets();
            _window.StyleResolver.AddStyleSheet(newSheet);
            _window.Root.MarkDirty();
        });
    }

    /// <summary>
    /// Reads a file with retry logic to handle file lock contention
    /// from editors that write atomically.
    /// </summary>
    private static string? ReadFileWithRetry(string path)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (IOException)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }
        return null;
    }

    private static void DisposeWatcher(ref FileSystemWatcher? watcher)
    {
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watcher = null;
        }
    }

    private static void DisposeDebounce(ref Timer? timer)
    {
        timer?.Dispose();
        timer = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
