using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lumi;
using Lumi.Core;
using Lumi.Core.Animation;
using Lumi.Core.Binding;
using Lumi.Core.Components;

namespace HelloWorld;

/// <summary>
/// The main application window. Loads its UI from MainWindow.html and styles from MainWindow.css.
/// Demonstrates interactive features: counter with data binding, animated progress bar, theme toggle,
/// component showcase (slider, checkbox, list, dialog), entrance animations, and live FPS.
/// </summary>
public class MainWindow : Window
{
    private double _progressTime;
    private bool _isDarkTheme = true;
    private bool _animationsEnabled = false;
    private int _fpsUpdateCounter;

    // Data binding
    private readonly AppViewModel _viewModel = new();
    private readonly BindingEngine _bindingEngine = new();

    // Cached element references for per-frame updates
    private Element? _progressCard;
    private Element? _progressFill;
    private TextElement? _progressLabel;
    private TextElement? _themeLabel;
    private TextElement? _fpsText;
    private string _lastFpsText = "";

    // Dialog reference
    private LumiDialog? _dialog;

    public MainWindow()
    {
        Title = "Lumi — Hello World";
        Width = 960;
        Height = 720;

        var outputDir = AppContext.BaseDirectory;
        var sourceDir = GetSourceDirectory();

        // Load from output (works in production)
        LoadTemplate(Path.Combine(outputDir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(outputDir, "MainWindow.css"));

        // Hot reload watches the SOURCE files you edit, not the bin/ copies
        HtmlPath = Path.Combine(sourceDir, "MainWindow.html");
        CssPath = Path.Combine(sourceDir, "MainWindow.css");
        EnableHotReload = true;
    }

    public override void OnReady()
    {
        // Cache element references
        _progressCard = FindById("progress-card");
        _progressFill = FindById("progress-fill");
        _progressLabel = FindById("progress-label") as TextElement;
        _themeLabel = FindById("theme-label") as TextElement;
        _fpsText = FindById("fps-text") as TextElement;
        
        var checkbox = new LumiCheckbox { Label = "Enable animations", IsChecked = _animationsEnabled };
        checkbox.OnChanged = (isChecked) =>
        {
            _animationsEnabled = isChecked;
            if (!isChecked && _progressFill != null)
            {
                // Reset progress bar immediately when disabling animations
                _progressFill.InlineStyle = "width: 0px";
                _progressLabel!.Text = "0%";
                _progressFill.MarkDirty();
            }
        };
        _progressCard?.AddChild(checkbox.Root);

        // Bind counter display to view model
        var counterValue = FindById("counter-value");
        if (counterValue != null)
        {
            var expr = BindingExpression.Parse("{Binding Count}");
            _bindingEngine.Bind(counterValue, "Text", _viewModel, expr);
        }

        // Wire up card hover effects
        SetupHoverEffects("card", "#475569", "#334155");
        SetupHoverEffects("demo-card", "#264A78", "#1E3A5F");

        // Wire up nav link hover
        var navLinks = FindByClass("nav-link");
        foreach (var link in navLinks)
        {
            link.On("MouseEnter", (sender, _) =>
            {
                var hoverColor = _isDarkTheme ? "#F1F5F9" : "#1E293B";
                ((Element)sender).InlineStyle = $"color: {hoverColor}";
                ((Element)sender).MarkDirty();
            });

            link.On("MouseLeave", (sender, _) =>
            {
                var el = (Element)sender;
                string color;
                if (el.Classes.Contains("active"))
                    color = _isDarkTheme ? "#F1F5F9" : "#1E293B";
                else
                    color = _isDarkTheme ? "#94A3B8" : "#64748B";
                el.InlineStyle = $"color: {color}";
                el.MarkDirty();
            });
        }

        // Wire up button hover effects
        SetupButtonHover("btn-increment", "#3B82F6", "#2563EB");
        SetupButtonHover("btn-decrement", "#64748B", "#475569");
        SetupButtonHover("btn-theme", "#8B5CF6", "#7C3AED");
        SetupButtonHover("btn-dialog", "#3B82F6", "#2563EB");

        // Counter buttons (use data binding via view model)
        var btnIncrement = FindById("btn-increment");
        btnIncrement?.On("Click", (_, _) => _viewModel.Count++);

        var btnDecrement = FindById("btn-decrement");
        btnDecrement?.On("Click", (_, _) => _viewModel.Count--);

        // Theme toggle
        var btnTheme = FindById("btn-theme");
        btnTheme?.On("Click", (_, _) => ToggleTheme());

        // Component showcase
        SetupComponents();
        SetupScrollList();
        SetupDialog();

        // Animate cards on startup
        AnimateCardsEntrance();
    }

    public override void OnUpdate()
    {
        if (_animationsEnabled)
        {
            // Animate progress bar (cycles every 3 seconds)
            _progressTime += 0.016; // ~60fps frame time approximation
            double progress = (_progressTime % 3.0) / 3.0;
            int percent = (int)(progress * 100);

            if (_progressFill != null)
            {
                // Clamp fill to track width (140px track)
                float fillWidth = (float)(140.0 * progress);
                string newStyle = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"width: {fillWidth:F1}px");
                if (_progressFill.InlineStyle != newStyle)
                {
                    _progressFill.InlineStyle = newStyle;
                    _progressFill.MarkDirty();
                }
            }

            if (_progressLabel != null)
            {
                string newText = $"{percent}%";
                if (_progressLabel.Text != newText)
                {
                    _progressLabel.Text = newText;
                    _progressLabel.MarkDirty();
                }
            }
        }

        // Update FPS display every 30 frames (~500ms) to avoid constant dirtying
        _fpsUpdateCounter++;
        if (_fpsText != null && _fpsUpdateCounter >= 30)
        {
            _fpsUpdateCounter = 0;
            double fps = FrameMetrics.CurrentFps;
            double avgFps = FrameMetrics.AverageFps;
            string newFps = $"FPS: {fps:F0} (avg {avgFps:F0}) | F12 Inspector | Built with Lumi";
            if (newFps != _lastFpsText)
            {
                _fpsText.Text = newFps;
                _lastFpsText = newFps;
                _fpsText.MarkDirty();
            }
        }
    }

    private void SetupComponents()
    {
        var host = FindById("component-host");
        if (host == null) return;

        var slider = new LumiSlider { Min = 0, Max = 100, Value = 50 };
        host.AddChild(slider.Root);
    }

    private void SetupScrollList()
    {
        var scrollHost = FindById("scroll-list");
        if (scrollHost == null) return;

        var items = new List<string>();
        for (int i = 1; i <= 20; i++)
            items.Add($"List item {i}");

        var list = new LumiList { Items = items };
        scrollHost.AddChild(list.Root);
    }

    private void SetupDialog()
    {
        _dialog = new LumiDialog { Title = "Hello from Lumi!" };

        var btnDialog = FindById("btn-dialog");
        btnDialog?.On("Click", (_, _) =>
        {
            if (_dialog != null)
                _dialog.IsOpen = !_dialog.IsOpen;
        });

        Root.AddChild(_dialog.Root);
    }

    private void AnimateCardsEntrance()
    {
        var cards = FindByClass("card");
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
            delay += 0.15f;
        }
    }

    private void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;

        var app = FindByClass("app").FirstOrDefault();
        var main = FindByClass("main").FirstOrDefault();
        var header = FindByClass("header").FirstOrDefault();
        var footer = FindByClass("footer").FirstOrDefault();

        if (_isDarkTheme)
        {
            if (app != null) { app.InlineStyle = "background-color: #1E293B"; app.MarkDirty(); }
            if (main != null) { main.InlineStyle = "background-color: #1E293B"; main.MarkDirty(); }
            if (header != null) { header.InlineStyle = "background-color: #0F172A"; header.MarkDirty(); }
            if (footer != null) { footer.InlineStyle = "background-color: #0F172A"; footer.MarkDirty(); }
            if (_themeLabel != null) { _themeLabel.Text = "Dark Mode"; _themeLabel.InlineStyle = "color: #F8FAFC; font-size: 18px; font-weight: 600; margin: 0px 0px 16px 0px; text-align: center"; _themeLabel.MarkDirty(); }

            foreach (var card in FindByClass("card"))
            {
                card.InlineStyle = "background-color: #334155";
                card.MarkDirty();
            }
            foreach (var demoCard in FindByClass("demo-card"))
            {
                demoCard.InlineStyle = "background-color: #1E3A5F";
                demoCard.MarkDirty();
            }
        }
        else
        {
            if (app != null) { app.InlineStyle = "background-color: #F1F5F9"; app.MarkDirty(); }
            if (main != null) { main.InlineStyle = "background-color: #F1F5F9"; main.MarkDirty(); }
            if (header != null) { header.InlineStyle = "background-color: #FFFFFF"; header.MarkDirty(); }
            if (footer != null) { footer.InlineStyle = "background-color: #FFFFFF"; footer.MarkDirty(); }
            if (_themeLabel != null) { _themeLabel.Text = "Light Mode"; _themeLabel.InlineStyle = "color: #1E293B; font-size: 18px; font-weight: 600; margin: 0px 0px 16px 0px; text-align: center"; _themeLabel.MarkDirty(); }

            foreach (var card in FindByClass("card"))
            {
                card.InlineStyle = "background-color: #FFFFFF";
                card.MarkDirty();
            }
            foreach (var demoCard in FindByClass("demo-card"))
            {
                demoCard.InlineStyle = "background-color: #E2E8F0";
                demoCard.MarkDirty();
            }
        }
    }

    private void SetupHoverEffects(string className, string darkHoverColor, string darkNormalColor)
    {
        // Light theme equivalents
        string lightNormalColor = className == "card" ? "#FFFFFF" : "#E2E8F0";
        string lightHoverColor = className == "card" ? "#E2E8F0" : "#BFDBFE";

        var elements = FindByClass(className);
        foreach (var el in elements)
        {
            el.On("MouseEnter", (sender, _) =>
            {
                var hc = _isDarkTheme ? darkHoverColor : lightHoverColor;
                ((Element)sender).InlineStyle = $"background-color: {hc}";
                ((Element)sender).MarkDirty();
            });

            el.On("MouseLeave", (sender, _) =>
            {
                var nc = _isDarkTheme ? darkNormalColor : lightNormalColor;
                ((Element)sender).InlineStyle = $"background-color: {nc}";
                ((Element)sender).MarkDirty();
            });
        }
    }

    private void SetupButtonHover(string id, string hoverColor, string normalColor)
    {
        var btn = FindById(id);
        if (btn == null) return;

        btn.On("MouseEnter", (sender, _) =>
        {
            ((Element)sender).InlineStyle = $"background-color: {hoverColor}";
            ((Element)sender).MarkDirty();
        });

        btn.On("MouseLeave", (sender, _) =>
        {
            ((Element)sender).InlineStyle = $"background-color: {normalColor}";
            ((Element)sender).MarkDirty();
        });
    }

    /// <summary>
    /// Gets the source project directory at compile time so hot reload
    /// watches the files you actually edit, not the bin/output copies.
    /// </summary>
    private static string GetSourceDirectory([CallerFilePath] string callerPath = "")
        => Path.GetDirectoryName(callerPath)!;

    /// <summary>Simple view model for data binding demo.</summary>
    private class AppViewModel : INotifyPropertyChanged
    {
        private int _count;
        public int Count
        {
            get => _count;
            set { _count = value; PropertyChanged?.Invoke(this, new(nameof(Count))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
