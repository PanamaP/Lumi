# Lumi

**A native C# GUI framework that uses HTML and CSS as the authoring syntax, rendered via SkiaSharp — no browser engine, no Electron.**

Lumi lets you write desktop UI with the languages you already know — HTML for structure, CSS for styling — while rendering everything natively through Skia. The result is a lightweight, fast, single-process desktop application with no web runtime overhead.

---

## ✨ Features

- **HTML Templates** — Define your UI structure using standard HTML markup
- **CSS Styling** — Full cascade, specificity, inheritance, 86 CSS properties, CSS variables (`var()`)
- **Native Rendering** — SkiaSharp paints every pixel; no browser, no WebView
- **Flexbox Layout** — Facebook Yoga powers the layout engine (flex-direction, wrapping, gap, alignment)
- **SDL3 Windowing** — Cross-platform window management and input handling
- **Routed Events** — Three-phase event system (tunnel → direct → bubble)
- **Hit Testing** — Reverse paint-order element targeting, scroll-offset-aware
- **Component Library** — Button, Checkbox, Slider, Dialog, Dropdown, TextBox, List
- **Data Binding** — One-way, two-way, and one-time binding to `INotifyPropertyChanged` view models
- **Animations** — Fluent tween API with easing functions + CSS transitions
- **Hot Reload** — Live HTML/CSS editing with instant refresh
- **Inspector** — Press F12 for a DevTools-style box model overlay
- **Screenshots** — Press F5 to capture the current frame, or use the headless screenshot tool

## 🚀 Quick Start

```bash
dotnet new install Lumi.Templates
dotnet new lumi -n MyApp
cd MyApp
dotnet run
```

That's it — you get a running desktop app with a window, HTML template, and CSS styling.

### What's inside

```csharp
// Program.cs — one line to start the app
using Lumi;

LumiApp.Run(new MainWindow());
```

```csharp
// MainWindow.cs — your window class
using Lumi;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "MyApp";
        Width = 960;
        Height = 680;

        var dir = AppContext.BaseDirectory;
        LoadTemplate(Path.Combine(dir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(dir, "MainWindow.css"));
    }
}
```

```html
<!-- MainWindow.html -->
<div class="app">
  <h1 class="title">Hello, Lumi!</h1>
  <button class="btn" id="my-button">Click Me</button>
</div>
```

```css
/* MainWindow.css */
.app {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background-color: #1E293B;
}

.title {
  color: #F8FAFC;
  font-size: 32px;
}

.btn {
  padding: 12px 24px;
  background-color: #3B82F6;
  color: white;
  border-radius: 8px;
  cursor: pointer;
}
```

> 📖 See [docs/getting-started.md](docs/getting-started.md) for a full beginner's guide with examples of every feature.

## 🏗️ Architecture

Lumi is split into focused libraries, each handling one responsibility:

```
Lumi (umbrella)
├── Lumi.Core        — Element tree, events, components, animation, data binding
├── Lumi.Styling     — CSS parsing, selectors, cascade, HTML templates
├── Lumi.Layout      — Yoga-based flexbox layout engine
├── Lumi.Rendering   — SkiaSharp 2D painting
├── Lumi.Input       — Hit testing and interaction state
├── Lumi.Platform    — SDL3 windowing and input translation
└── Lumi.Text        — HarfBuzz text shaping (implemented, integration in progress)
```

### Render Pipeline

```
HTML Template → Element Tree → CSS Cascade → Yoga Layout → Skia Paint → SDL3 Present
```

Each frame:
1. **Input** — SDL3 events are translated to Lumi input events
2. **Events** — Routed through the element tree (tunnel → direct → bubble)
3. **Update** — User `OnUpdate()` code runs, animations tick
4. **Style** — CSS rules + inline styles are matched and applied via cascade/specificity
5. **Layout** — Yoga calculates flex positions and sizes
6. **Paint** — SkiaSharp renders backgrounds, borders, text, and decorations
7. **Present** — Pixels are blitted to the SDL3 window

## 📁 Project Structure

```
Lumi.slnx                      Solution file
src/
  Lumi/                         Umbrella library (Window, LumiApp, HotReload, Inspector)
  Lumi.Core/                    Element, ComputedStyle, Application, Events
    Components/                 LumiButton, LumiCheckbox, LumiSlider, LumiDialog, ...
    Animation/                  TweenEngine, AnimationBuilder, Easing, TransitionManager
    Binding/                    BindingEngine, BindingExpression
  Lumi.Styling/                 CssParser, SelectorMatcher, StyleResolver, HtmlTemplateParser
  Lumi.Layout/                  YogaLayoutEngine
  Lumi.Rendering/               SkiaRenderer
  Lumi.Input/                   HitTester, InteractionState
  Lumi.Platform/                Sdl3Window, Sdl3RenderTarget, IPlatformWindow
  Lumi.Text/                    HarfBuzz text shaping (implemented, integration in progress)
samples/
  HelloWorld/                   Feature showcase sample app
  StressTest/                   Performance benchmark suite
tests/
  Lumi.Tests/                   315 unit + integration tests
tools/
  ScreenshotTool/               Headless screenshot capture utility
```

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| [SkiaSharp](https://github.com/mono/SkiaSharp) | 3.* | 2D rendering engine |
| [ppy.SDL3-CS](https://github.com/ppy/SDL3-CS) | 2026.320.0 | Cross-platform windowing & input |
| [AngleSharp](https://anglesharp.github.io/) | 1.4.0 | HTML5 template parsing |
| [Yoga-CS](https://www.nuget.org/packages/Yoga-CS) | 1.0.0 | Flexbox layout (Facebook Yoga) |
| [HarfBuzzSharp](https://github.com/nicklackner/HarfBuzzSharp) | 8.3.1.3 | Text shaping |

## 🧪 Running Tests

```bash
dotnet test Lumi.slnx
```

315 tests covering CSS parsing, layout, hit testing, components, binding, rendering, and text shaping.

## 🔧 Building & Running

```bash
# Build everything
dotnet build Lumi.slnx

# Run the sample
dotnet run --project samples/HelloWorld
```

### Keyboard Shortcuts (at runtime)

| Key | Action |
|-----|--------|
| **F5** | Save screenshot to `~/Desktop/LumiScreenshots/` |
| **F12** | Toggle the inspector overlay |

## 🗺️ Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1. Foundation | ✅ Done | SDL3 windowing, Skia rendering, app loop |
| 2. Element Tree & Events | ✅ Done | Element model, HTML parser, routed events |
| 3. CSS Engine | ✅ Done | CSS parsing, selectors, cascade, specificity, variables |
| 4. Layout Engine | ✅ Done | Yoga flexbox, absolute/fixed positioning, scrolling |
| 5. Data Binding | ✅ Done | `INotifyPropertyChanged` binding with expression parser |
| 6. Animations | ✅ Done | Tween engine, easing functions, CSS transitions |
| 7. Components | ✅ Done | Button, Checkbox, Slider, Dialog, Dropdown, TextBox, List |
| 8. Developer Tools | ✅ Done | Hot reload, inspector overlay, screenshot capture |
| 9. Text & Images | 🔶 In Progress | HarfBuzz text shaping (done), image loading (planned) |
| 10. Accessibility | 🔲 Planned | UIA on Windows, AT-SPI on Linux |
| 11. Performance | 🔶 In Progress | VSync frame pacing, live resize, event-driven idle |

## 📄 License

MIT

---

> *Lumi — Light up your desktop with web-native syntax.*
