# Lumi

**A native C# GUI framework that uses HTML and CSS as the authoring syntax, rendered via SkiaSharp.**

Write desktop UI with HTML for structure and CSS for styling, while rendering everything natively through Skia. The result is a lightweight, fast, single-process desktop application with no web runtime overhead.

## Quick Start

```bash
dotnet new install Lumi.Templates
dotnet new lumi -n MyApp
cd MyApp
dotnet run
```

## Features

- **HTML Templates** — Define UI structure using standard HTML markup
- **CSS Styling** — Full cascade, specificity, inheritance, 86 properties, CSS variables
- **Native Rendering** — SkiaSharp paints every pixel; no browser, no WebView
- **Flexbox Layout** — Facebook Yoga powers the layout engine
- **SDL3 Windowing** — Cross-platform window management and input
- **Data Binding** — One-way, two-way, and one-time binding to `INotifyPropertyChanged` view models
- **Components** — Button, Checkbox, Slider, Dialog, Dropdown, TextBox, List
- **Animations** — Fluent tween API with easing functions + CSS transitions
- **Hot Reload** — Live HTML/CSS editing with instant refresh
- **Inspector** — Press F12 for a DevTools-style box model overlay

## Example

```csharp
using Lumi;

LumiApp.Run(new MainWindow());

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

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Learn More

- [Getting Started Guide](https://github.com/PanamaP/Lumi/blob/main/docs/getting-started.md)
- [GitHub Repository](https://github.com/PanamaP/Lumi)
