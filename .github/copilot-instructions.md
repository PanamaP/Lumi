# Copilot Instructions for Lumi

## Project Overview

Lumi is a .NET 10 native C# desktop GUI framework that uses HTML and CSS as the authoring syntax, rendered via SkiaSharp — no browser engine, no Electron. It uses SDL3 for windowing and input.

## Architecture

```
Lumi (umbrella)          — Window, LumiApp, HotReload, Inspector
├── Lumi.Core             — Element tree, events, components, animation, data binding
├── Lumi.Styling          — CSS parsing, selectors, cascade, HTML templates
├── Lumi.Layout           — Yoga-based flexbox + CSS Grid layout engine
├── Lumi.Rendering        — SkiaSharp 2D painting (GPU + CPU fallback)
├── Lumi.Input            — Hit testing and interaction state
├── Lumi.Platform         — SDL3 windowing and input translation
├── Lumi.Text             — HarfBuzz text shaping
└── Lumi.Generators       — Roslyn source generator for [Observable]
```

### Render Pipeline

```
HTML Template → Element Tree → CSS Cascade → Yoga Layout → Skia Paint → SDL3 Present
```

## Build & Test

```bash
dotnet build Lumi.slnx
dotnet test tests/Lumi.Tests/Lumi.Tests.csproj
dotnet run --project samples/HelloWorld
```

## Code Style

- .NET 10 with `ImplicitUsings` and `Nullable` enabled
- XML doc comments (`/// <summary>`) on all public APIs
- File-scoped namespaces (`namespace Lumi;`)
- Use `InvariantCulture` for float-to-string in CSS/style values

## Key Patterns

### Window Lifecycle

Subclass `Window`, load HTML/CSS, override lifecycle methods:

```csharp
public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "MyApp";
        Width = 960;
        Height = 680;
        LoadTemplate(Path.Combine(AppContext.BaseDirectory, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(AppContext.BaseDirectory, "MainWindow.css"));
    }

    protected override void OnReady()    { /* Wire up events — called once after load */ }
    protected override void OnUpdate()   { /* Per-frame logic — called every frame */ }
}
```

Entry point: `LumiApp.Run(new MainWindow());`

### Element Tree

- `Element` is the base class; concrete types: `BoxElement`, `TextElement`, `ImageElement`, `InputElement`
- HTML tags map to element types via `ElementRegistry` (unknown tags become `BoxElement`)
- `element.AddChild(child)` / `element.RemoveChild(child)` to modify the tree
- `FindById("id")` for O(1) lookup; `FindByClass("class")` for class-based lookup
- Always call `element.MarkDirty()` after changing `InlineStyle` or element content

### Event System

Three-phase routed events: Tunnel → Direct → Bubble.

```csharp
element.On("Click", (sender, e) => { /* handler */ });
```

- Event names are case-insensitive: `"Click"`, `"click"`, `"CLICK"` all work
- `RoutedEvent` base; cast to `RoutedMouseEvent` (X, Y, Button) or `RoutedKeyEvent` (Key, Shift, Ctrl, Alt)
- Set `e.Handled = true` to stop propagation
- Available events: Click, MouseDown, MouseUp, MouseMove, MouseEnter, MouseLeave, KeyDown, KeyUp, Focus, Blur, Scroll, DragStart, DragOver, Drop, DragEnter, DragLeave

### Styling

- **InlineStyle** (string on Element): Dynamic CSS set in code — `element.InlineStyle = "color: red; padding: 10px;"`
- **ComputedStyle** (typed properties): Read-only resolved values computed each frame from CSS cascade
- Use `InlineStyle` for all dynamic styling; `ComputedStyle` is rebuilt from CSS every frame
- CSS variables: define with `--name: value;` and use with `var(--name)`

### Components

Components are NOT elements — they wrap elements and expose a `Root` property:

```csharp
var btn = new LumiButton { Text = "Save" };
btn.OnClick = () => SaveData();
container.AddChild(btn.Root);  // Always add .Root to the tree
```

Available: `LumiButton`, `LumiCheckbox`, `LumiSlider`, `LumiDialog`, `LumiDropdown`, `LumiTextBox`, `LumiList`, `LumiRadioGroup`, `LumiToggle`, `LumiProgressBar`, `LumiTabControl`, `LumiTooltip`

All live in `Lumi.Core.Components`. Use `ComponentStyles` for consistent theming.

### Custom Component Pattern

```csharp
public class MyComponent
{
    private readonly BoxElement _root;
    private readonly TextElement _label;

    public Element Root => _root;

    public MyComponent()
    {
        _root = new BoxElement("div");
        _label = new TextElement("Default");
        _root.AddChild(_label);
        ComponentStyles.ApplyContainer(_root, FlexDirection.Column);
    }

    public string Text
    {
        get => _label.Text;
        set { _label.Text = value; _root.MarkDirty(); }
    }
}
```

### Data Binding

Bind `INotifyPropertyChanged` view models to elements:

```csharp
BindingEngine.Bind(viewModel, "PropertyName", targetElement, "Text");
```

Modes: `OneTime`, `OneWay` (source→target), `TwoWay` (bidirectional, for InputElement).

Use the `[Observable]` source generator to eliminate INotifyPropertyChanged boilerplate:

```csharp
[Observable]
public partial class MyViewModel
{
    [Observable] public partial string Name { get; set; }
}
```

### Template Directives

```html
<template for="{Items}" as="item">
  <div>{item.Name}</div>
</template>

<template if="{IsVisible}">
  <div>Conditionally shown</div>
</template>
```

Activate with `TemplateEngine.Process(root, viewModel)`.

### Animations

Fluent builder API on any element:

```csharp
element.Animate()
    .Property("opacity", 0f, 1f)
    .Duration(0.3f)
    .Easing(Easing.EaseOut)
    .Start();
```

Animatable properties: opacity, width, height, border-radius, font-size, margin-*, padding-*.

### Multi-Window

```csharp
Windows?.Open(new MySecondaryWindow());  // SecondaryWindow inherits Window
```

## Testing Conventions

- xUnit with `[Fact]` and `[Theory]` attributes
- Test naming: `MethodName_Condition_ExpectedResult`
- Use `HeadlessPipeline.Render(html, css)` or `HeadlessPipeline.StyleAndLayout(html, css)` for integration tests
- Tests in `tests/Lumi.Tests/`, helpers in `tests/Lumi.Tests/Helpers/`

## NuGet Packaging

- Only the umbrella `Lumi` package is published to NuGet
- Sub-projects have `IsPackable=false` — they are bundled into the umbrella via `PrivateAssets="all"`
- `Lumi.Generators` is included as a source generator analyzer
