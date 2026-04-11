using System.Runtime.CompilerServices;
using Lumi;
using Lumi.Core;
using Lumi.Core.Animation;
using Lumi.Core.Components;

namespace Dashboard;

public class MainWindow : Window
{
    private bool _isDark = true;
    private string _activeTab = "overview";
    private bool _settingsBuilt;

    public MainWindow()
    {
        Title = "Lumi — Dashboard";
        Width = 960;
        Height = 720;

        var outputDir = AppContext.BaseDirectory;
        var sourceDir = GetSourceDirectory();

        LoadTemplate(Path.Combine(outputDir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(outputDir, "MainWindow.css"));

        HtmlPath = Path.Combine(sourceDir, "MainWindow.html");
        CssPath = Path.Combine(sourceDir, "MainWindow.css");
        EnableHotReload = true;
    }

    public override void OnReady()
    {
        // Theme toggle
        FindById("btn-theme")?.On("Click", (_, _) => ToggleTheme());

        // Tab navigation
        FindById("nav-overview")?.On("Click", (_, _) => SwitchTab("overview"));
        FindById("nav-analytics")?.On("Click", (_, _) => SwitchTab("analytics"));
        FindById("nav-settings")?.On("Click", (_, _) => SwitchTab("settings"));

        // Build dynamic content
        BuildProgressBars();
        BuildBarChart();
        BuildSettingsPanel();

        // Animate stat cards on entry
        AnimateStatCards();
    }

    private void SwitchTab(string tab)
    {
        _activeTab = tab;

        // Toggle tab visibility
        var tabs = new[] { "overview", "analytics", "settings" };
        foreach (var t in tabs)
        {
            var panel = FindById($"tab-{t}");
            if (panel == null) continue;
            if (t == tab)
                panel.Classes.Remove("hidden");
            else
                panel.Classes.Add("hidden");
            panel.MarkDirty();
        }

        // Update nav link active state
        var navIds = new[] { "nav-overview", "nav-analytics", "nav-settings" };
        var navNames = new[] { "overview", "analytics", "settings" };
        for (int i = 0; i < navIds.Length; i++)
        {
            var link = FindById(navIds[i]);
            if (link == null) continue;
            if (navNames[i] == tab)
                link.Classes.Add("active");
            else
                link.Classes.Remove("active");
            link.MarkDirty();
        }
    }

    private void ToggleTheme()
    {
        _isDark = !_isDark;

        // Toggle theme via CSS class — the stylesheet handles all color
        // changes through CSS variables defined in .app / .app.light.
        var app = FindById("app-root");
        if (app == null) return;

        if (_isDark)
            app.Classes.Remove("light");
        else
            app.Classes.Add("light");

        var themeBtn = FindById("btn-theme");
        if (themeBtn != null)
        {
            var txt = themeBtn.Children.OfType<TextElement>().FirstOrDefault();
            if (txt != null) txt.Text = _isDark ? "🌙 Dark" : "☀️ Light";
        }

        app.MarkDirty();
    }

    private void BuildProgressBars()
    {
        var container = FindById("progress-list");
        if (container == null) return;

        var projects = new (string Name, float Progress, string Color)[]
        {
            ("Website Redesign", 0.85f, "fill-blue"),
            ("Mobile App v2", 0.62f, "fill-green"),
            ("API Integration", 0.41f, "fill-yellow"),
            ("Data Migration", 0.93f, "fill-purple"),
        };

        foreach (var (name, progress, color) in projects)
        {
            var item = new BoxElement("div");
            item.Classes.Add("progress-item");

            // Header row with name and percentage
            var header = new BoxElement("div");
            header.Classes.Add("progress-header");

            var nameEl = new TextElement(name);
            nameEl.Classes.Add("progress-name");
            header.AddChild(nameEl);

            var percentEl = new TextElement($"{(int)(progress * 100)}%");
            percentEl.Classes.Add("progress-percent");
            header.AddChild(percentEl);

            item.AddChild(header);

            // Progress bar using LumiProgressBar
            var progressBar = new LumiProgressBar { Value = progress };
            item.AddChild(progressBar.Root);

            container.AddChild(item);
        }
    }

    private void BuildBarChart()
    {
        var barData = new (string Id, float Height)[]
        {
            ("bar-mon", 120), ("bar-tue", 85), ("bar-wed", 150),
            ("bar-thu", 95), ("bar-fri", 135), ("bar-sat", 60), ("bar-sun", 45),
        };

        foreach (var (id, height) in barData)
        {
            var bar = FindById(id);
            if (bar == null) continue;
            bar.InlineStyle = $"height: {height}px";
            bar.MarkDirty();
        }
    }

    private void BuildSettingsPanel()
    {
        if (_settingsBuilt) return;
        _settingsBuilt = true;

        var container = FindById("settings-list");
        if (container == null) return;

        // Toggle: notifications
        var notifToggle = new LumiToggle { Label = "Push Notifications", IsOn = true };
        container.AddChild(notifToggle.Root);

        // Toggle: auto-update
        var updateToggle = new LumiToggle { Label = "Auto-update Dashboard", IsOn = false };
        container.AddChild(updateToggle.Root);

        // Slider: refresh interval
        var slider = new LumiSlider { Min = 5, Max = 60, Value = 15 };
        container.AddChild(slider.Root);

        // Dropdown: timezone
        var dropdown = new LumiDropdown
        {
            Items = new List<string> { "UTC-8 Pacific", "UTC-5 Eastern", "UTC+0 London", "UTC+1 Berlin", "UTC+9 Tokyo" },
            SelectedIndex = 0
        };
        container.AddChild(dropdown.Root);
    }

    private void AnimateStatCards()
    {
        var cards = FindByClass("stat-card");
        float delay = 0;
        foreach (var card in cards)
        {
            card.ComputedStyle.Opacity = 0;
            card.Animate()
                .Property("opacity", 0, 1)
                .Duration(0.4f)
                .Delay(delay)
                .Easing(Easing.EaseOutCubic)
                .Start();
            delay += 0.1f;
        }
    }

    private static string GetSourceDirectory([CallerFilePath] string callerPath = "")
        => Path.GetDirectoryName(callerPath)!;
}
