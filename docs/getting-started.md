# Lumi — Getting Started Guide

A complete guide to building desktop applications with Lumi. From your first window to advanced features like data binding, animations, and custom components.

---

## Table of Contents

1. [Installation & Setup](#installation--setup)
2. [Your First Application](#your-first-application)
3. [HTML Templates](#html-templates)
4. [CSS Styling](#css-styling)
5. [Events & Interaction](#events--interaction)
6. [Finding Elements](#finding-elements)
7. [Dynamic Styles (InlineStyle)](#dynamic-styles-inlinestyle)
8. [Components](#components)
9. [Data Binding](#data-binding)
10. [Template Directives](#template-directives)
11. [Animations & Tweens](#animations--tweens)
12. [Scrolling](#scrolling)
13. [Multi-Window Support](#multi-window-support)
14. [Source Generator](#source-generator)
15. [Hot Reload](#hot-reload)
16. [Inspector & Screenshots](#inspector--screenshots)
17. [CSS Reference](#css-reference)
18. [API Reference](#api-reference)

---

## Installation & Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A code editor (VS Code, Visual Studio, Rider)

### Create a New Project

The fastest way to start is with the Lumi project template:

```bash
# Install the template (one-time)
dotnet new install Lumi.Templates

# Create a new app
dotnet new lumi -n MyApp
cd MyApp
dotnet run
```

This scaffolds a ready-to-run project with everything wired up:

```
MyApp/
├── MyApp.csproj          # Project file with Lumi NuGet reference
├── Program.cs            # App entry point (1 line)
├── MainWindow.cs         # Your window class
├── MainWindow.html       # UI template
└── MainWindow.css        # Styles
```

> **Developing Lumi itself?** You can reference the source directly instead:
> ```bash
> dotnet new console -n MyApp
> cd MyApp
> dotnet add reference ../path/to/src/Lumi/Lumi.csproj
> ```

---

## Your First Application

The template gives you a working app out of the box. Here's what each file does:

### Program.cs

```csharp
using Lumi;

LumiApp.Run(new MainWindow());
```

That's it — one line starts the application loop: SDL3 window creation, Skia rendering, input handling, all managed automatically.

### MainWindow.cs

```csharp
using Lumi;
using Lumi.Core;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "My First Lumi App";
        Width = 800;
        Height = 600;

        // Load the UI template and styles
        var dir = AppContext.BaseDirectory;
        LoadTemplate(Path.Combine(dir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(dir, "MainWindow.css"));
    }

    /// <summary>
    /// Called once after the element tree is built. Wire up event handlers here.
    /// </summary>
    public override void OnReady()
    {
        var greeting = FindById("greeting");
        var button = FindById("hello-btn");

        button?.On("Click", (sender, e) =>
        {
            if (greeting is TextElement text)
                text.Text = "You clicked the button!";
        });
    }
}
```

### MainWindow.html

```html
<div class="app">
  <h1 class="title">Welcome to Lumi</h1>
  <p class="subtitle" id="greeting">Click the button below.</p>
  <button class="btn" id="hello-btn">Say Hello</button>
</div>
```

### MainWindow.css

```css
:root {
  --bg: #0F172A;
  --text: #F8FAFC;
  --accent: #3B82F6;
}

.app {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background-color: var(--bg);
  padding: 40px;
}

.title {
  color: var(--text);
  font-size: 36px;
  font-weight: 700;
  margin: 0px 0px 8px 0px;
}

.subtitle {
  color: #94A3B8;
  font-size: 16px;
  margin: 0px 0px 24px 0px;
}

.btn {
  padding: 12px 32px;
  background-color: var(--accent);
  color: white;
  font-size: 16px;
  border-radius: 8px;
  cursor: pointer;
}
```

### Run It

```bash
dotnet run
```

---

## HTML Templates

Lumi uses standard HTML parsed by [AngleSharp](https://anglesharp.github.io/). The parser maps HTML tags to Lumi element types:

| HTML Tag | Lumi Element | Notes |
|----------|-------------|-------|
| `<div>`, `<section>`, `<header>`, `<footer>`, `<nav>`, `<main>`, `<article>`, `<aside>` | `BoxElement` | Container elements |
| `<h1>` – `<h6>`, `<p>`, `<span>` | `BoxElement` containing `TextElement` | Text is wrapped in a `TextElement` child |
| `<button>`, `<a>` | `BoxElement` (focusable) | Automatically `IsFocusable = true` |
| `<img>` | `ImageElement` | `src` attribute maps to `Source` property |
| `<input>` | `InputElement` | Supports `type`, `value`, `placeholder` attributes |
| Any text content | `TextElement` | Direct text nodes become `TextElement` |

### Supported Attributes

```html
<div id="my-id"
     class="cls-a cls-b"
     style="color: red; font-size: 20px"
     role="button"
     aria-label="Accessible name"
     data-custom="value">
  Content here
</div>
```

- `id` → `Element.Id`
- `class` → `Element.Classes` (space-separated)
- `style` → `Element.InlineStyle` (highest cascade priority)
- `role`, `aria-*` → `Element.Accessibility`
- `data-*` and others → `Element.Attributes` dictionary

---

## CSS Styling

Lumi supports a comprehensive subset of CSS, parsed by a lightweight hand-written tokenizer. Styles cascade with full specificity rules: inline styles win over ID selectors, which win over class selectors, which win over tag selectors.

### CSS Variables

Define custom properties in `:root` or any selector:

```css
:root {
  --primary: #3B82F6;
  --bg: #1E293B;
  --radius: 8px;
}

.card {
  background-color: var(--bg);
  border-radius: var(--radius);
  border-color: var(--primary);
}

/* Fallback values */
.item {
  color: var(--missing-color, #FFFFFF);
}
```

### Selectors Supported

```css
div { }                  /* Tag selector */
.card { }                /* Class selector */
#my-id { }               /* ID selector */
div.card { }             /* Tag + class */
.parent .child { }       /* Descendant combinator */
.parent > .child { }     /* Direct child combinator */
.card, .panel { }        /* Group selector */
h1, h2, h3 { }           /* Multiple tags */
```

### Loading Multiple Stylesheets

```csharp
// In your Window constructor
LoadStyleSheet(Path.Combine(dir, "reset.css"));
LoadStyleSheet(Path.Combine(dir, "theme.css"));
LoadStyleSheet(Path.Combine(dir, "main.css"));

// Or from strings
LoadStyleSheetString(".dynamic { color: red; }");
```

---

## Events & Interaction

Lumi uses a three-phase event system inspired by the DOM: **Tunnel** (root → target), **Direct** (target only), **Bubble** (target → root).

### Registering Event Handlers

```csharp
// Event names are case-insensitive: "click", "Click", "CLICK" all work
element.On("Click", (sender, e) =>
{
    Console.WriteLine($"Clicked {sender.Id}!");
});

// Mouse events
element.On("MouseDown", (sender, e) =>
{
    if (e is RoutedMouseEvent me)
        Console.WriteLine($"Mouse down at ({me.X}, {me.Y})");
});

element.On("MouseUp", (sender, e) => { /* ... */ });
element.On("MouseMove", (sender, e) => { /* ... */ });
element.On("MouseEnter", (sender, e) => { /* hover in */ });
element.On("MouseLeave", (sender, e) => { /* hover out */ });

// Keyboard events
element.On("KeyDown", (sender, e) =>
{
    if (e is RoutedKeyEvent ke)
        Console.WriteLine($"Key: {ke.Key}, Ctrl: {ke.Ctrl}");
});

// Remove a handler
element.Off("Click", myHandler);
```

### Available Events

| Event | Dispatched When | Event Type |
|-------|----------------|------------|
| `Click` | Mouse button pressed and released on element | `RoutedMouseEvent` |
| `MouseDown` | Mouse button pressed | `RoutedMouseEvent` |
| `MouseUp` | Mouse button released | `RoutedMouseEvent` |
| `MouseMove` | Mouse moved over element | `RoutedMouseEvent` |
| `MouseEnter` | Mouse enters element bounds | `RoutedMouseEvent` |
| `MouseLeave` | Mouse leaves element bounds | `RoutedMouseEvent` |
| `KeyDown` | Key pressed (on focused element) | `RoutedKeyEvent` |
| `KeyUp` | Key released | `RoutedKeyEvent` |
| `Focus` | Element gains focus | `RoutedEvent` |
| `Blur` | Element loses focus | `RoutedEvent` |
| `Scroll` | Mouse wheel scrolled | `RoutedEvent` |

### Stopping Event Propagation

```csharp
element.On("Click", (sender, e) =>
{
    e.Handled = true; // Stops bubble to parent
});
```

---

## Finding Elements

The `Window` class provides methods to query the element tree:

```csharp
public override void OnReady()
{
    // Find by ID (returns null if not found)
    Element? header = FindById("main-header");

    // Find all elements with a class
    List<Element> cards = FindByClass("card");

    // Access children
    Element firstChild = header.Children[0];

    // Navigate up
    Element? parent = firstChild.Parent;

    // Check element type
    if (header is TextElement textEl)
        Console.WriteLine(textEl.Text);
}
```

### Element Tree Manipulation

```csharp
// Create elements programmatically
var box = new BoxElement("div");
box.Id = "dynamic-box";
box.Classes = ["card", "highlighted"];

var label = new TextElement("Hello!");

// Build tree
box.AddChild(label);
parentElement.AddChild(box);

// Remove / clear
parentElement.RemoveChild(box);
parentElement.ClearChildren();

// Insert at position
parentElement.InsertChild(0, box); // Insert at beginning
```

---

## Dynamic Styles (InlineStyle)

**Important:** Never set `ComputedStyle` properties directly — the CSS StyleResolver overwrites them every frame. Use `InlineStyle` instead, which has the highest cascade priority.

```csharp
// ✅ Correct — survives the CSS cascade
element.InlineStyle = "background-color: #FF0000; padding: 16px";
element.MarkDirty(); // Tell the framework to re-render

// ❌ Wrong — overwritten by StyleResolver next frame
element.ComputedStyle.BackgroundColor = Color.FromHex("#FF0000");
```

### Common Patterns

```csharp
// Toggle visibility
element.InlineStyle = isVisible ? "display: flex" : "display: none";
element.MarkDirty();

// Hover effects
element.On("MouseEnter", (sender, _) =>
{
    ((Element)sender).InlineStyle = "background-color: #3B82F6";
    ((Element)sender).MarkDirty();
});
element.On("MouseLeave", (sender, _) =>
{
    ((Element)sender).InlineStyle = "background-color: #1E293B";
    ((Element)sender).MarkDirty();
});

// Animate a property each frame (use InvariantCulture for float formatting!)
public override void OnUpdate()
{
    float width = CalculateWidth();
    _progressBar.InlineStyle = string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"width: {width:F1}px");
    _progressBar.MarkDirty();
}
```

> ⚠️ **Culture Warning:** If your system uses comma as the decimal separator (e.g., `3,14` instead of `3.14`), always use `CultureInfo.InvariantCulture` when formatting floats in InlineStyle strings.

---

## Components

Lumi includes a library of pre-built components. Each component exposes a `Root` element that you add to your element tree.

### LumiButton

```csharp
using Lumi.Core.Components;

var button = new LumiButton
{
    Text = "Submit",
    Variant = ButtonVariant.Primary  // Primary, Secondary, or Danger
};

button.OnClick = () => Console.WriteLine("Submitted!");

// Disable it
button.IsDisabled = true;

// Add to tree
hostElement.AddChild(button.Root);
```

### LumiCheckbox

```csharp
var checkbox = new LumiCheckbox
{
    Label = "Enable notifications",
    IsChecked = true
};

checkbox.OnChanged = (isChecked) =>
{
    Console.WriteLine($"Checked: {isChecked}");
};

hostElement.AddChild(checkbox.Root);
```

### LumiSlider

```csharp
var slider = new LumiSlider
{
    Min = 0,
    Max = 100,
    Value = 50
};

slider.OnValueChanged = (value) =>
{
    Console.WriteLine($"Slider: {value:F1}");
};

hostElement.AddChild(slider.Root);
```

### LumiDialog

```csharp
var dialog = new LumiDialog
{
    Title = "Confirm Action"
};

// Add custom content
var message = new TextElement("Are you sure?");
message.InlineStyle = "color: #F8FAFC; font-size: 14px";
dialog.Content = message;

// Show/hide
dialog.IsOpen = true;

// Handle close (user clicks ✕ button)
dialog.OnClose = () => Console.WriteLine("Dialog closed");

// Add to ROOT element (dialogs overlay the entire window)
Root.AddChild(dialog.Root);
```

### LumiDropdown

```csharp
var dropdown = new LumiDropdown
{
    Items = ["Option A", "Option B", "Option C"],
    SelectedIndex = 0
};

dropdown.OnSelectionChanged = (index) =>
{
    Console.WriteLine($"Selected: {dropdown.Items[index]}");
};

hostElement.AddChild(dropdown.Root);
```

### LumiTextBox

```csharp
var textbox = new LumiTextBox
{
    Label = "Email",
    Placeholder = "you@example.com",
    Value = ""
};

textbox.OnValueChanged = (text) =>
{
    Console.WriteLine($"Input: {text}");
};

// Read-only mode
textbox.IsReadOnly = true;

hostElement.AddChild(textbox.Root);
```

### LumiList

```csharp
var list = new LumiList
{
    Items = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"]
};

list.OnItemClick = (index) =>
{
    Console.WriteLine($"Clicked item {index}");
};

// Place inside a scroll container for scrollable lists
scrollContainer.AddChild(list.Root);
```

### Component Color Palette

The `ComponentStyles` class provides the default dark-theme palette:

```csharp
ComponentStyles.Background   // #1E293B — app background
ComponentStyles.Accent        // #38BDF8 — primary accent
ComponentStyles.TextColor     // #F8FAFC — primary text
ComponentStyles.Subtle        // #94A3B8 — secondary text
ComponentStyles.Danger        // #EF4444 — danger/error
ComponentStyles.Surface       // #334155 — card/surface background
ComponentStyles.Border        // #475569 — borders
ComponentStyles.Disabled      // #64748B — disabled state

// Convert to CSS-compatible rgba() string
string css = ComponentStyles.ToRgba(ComponentStyles.Accent);
// Returns: "rgba(56, 189, 248, 255)"
```

---

## Data Binding

Lumi supports data binding to `INotifyPropertyChanged` view models.

### Basic Binding

```csharp
using System.ComponentModel;
using Lumi.Core.Binding;

// 1. Create a view model
public class CounterViewModel : INotifyPropertyChanged
{
    private int _count;
    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            PropertyChanged?.Invoke(this, new(nameof(Count)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

// 2. In your Window class
private readonly CounterViewModel _vm = new();
private readonly BindingEngine _bindingEngine = new();

public override void OnReady()
{
    // Bind an element's text to the Count property
    var counterDisplay = FindById("counter-value");
    if (counterDisplay != null)
    {
        var expr = BindingExpression.Parse("{Binding Count}");
        _bindingEngine.Bind(counterDisplay, "Text", _vm, expr);
    }

    // Change the view model — UI updates automatically
    FindById("btn-inc")?.On("Click", (_, _) => _vm.Count++);
}
```

### Binding Expression Syntax

```csharp
// Simple property
BindingExpression.Parse("{Binding Name}");

// Nested path
BindingExpression.Parse("{Binding User.Email}");

// With binding mode
BindingExpression.Parse("{Binding Name, Mode=TwoWay}");
BindingExpression.Parse("{Binding Name, Mode=OneTime}");

// With fallback value
BindingExpression.Parse("{Binding Score, FallbackValue=0}");
```

### Binding Modes

| Mode | Description |
|------|-------------|
| `OneWay` (default) | Source → UI. Changes in the view model update the element. |
| `TwoWay` | Source ↔ UI. Changes flow both directions (for input elements). |
| `OneTime` | Reads the value once and never updates again. |

---

## Template Directives

Template directives let you write loops and conditionals directly in HTML, instead of building UI elements manually in C# code-behind.

### `<template for="">` — Repeating Elements

Repeat a block of HTML for each item in a collection:

```html
<!-- In your HTML template -->
<ul class="user-list">
  <template for="{Users}" as="user">
    <li class="user-row">{user.Name} — {user.Email}</li>
  </template>
</ul>
```

- `for="{PropertyName}"` — path to a collection property on the data context
- `as="alias"` — name for the loop variable (default: `"item"`)
- Text like `{user.Name}` is resolved from each item's properties and updates reactively when the item implements `INotifyPropertyChanged`

### `<template if="">` — Conditional Rendering

Conditionally show or hide a block of HTML:

```html
<template if="{IsLoggedIn}">
  <div class="welcome">Welcome back!</div>
</template>

<template if="{HasError}">
  <div class="error-banner">Something went wrong.</div>
</template>
```

- `if="{PropertyName}"` — path to a property on the data context
- Truthy values: `true`, non-zero numbers, non-empty strings, non-null objects
- Falsy values: `false`, `0`, `null`, empty strings
- **Note:** `<template if>` currently supports conditional visibility only. Text interpolation (e.g. `{PropertyName}`) inside template-if content is not yet supported.

### Activating Directives with `TemplateEngine`

Template directives are activated by calling `TemplateEngine.Apply()` with your root element and a view model:

```csharp
using Lumi.Core.Binding;

public class MainWindow : Window
{
    private readonly AppViewModel _vm = new();

    public override void OnReady()
    {
        // Activate all template directives in the tree
        TemplateEngine.Apply(Root, _vm);
    }
}
```

### ObservableCollection Support

When the collection implements `INotifyCollectionChanged` (e.g., `ObservableCollection<T>`), the UI updates automatically when items are added, removed, or cleared — no manual refresh needed:

```csharp
using System.Collections.ObjectModel;

public class AppViewModel
{
    public ObservableCollection<string> Items { get; } = new(["Alpha", "Beta"]);
}

// Later — the UI updates automatically:
_vm.Items.Add("Gamma");      // New element appears
_vm.Items.RemoveAt(0);       // First element disappears
_vm.Items.Clear();           // All elements removed
```

### INotifyPropertyChanged for `<template if="">`

When the view model implements `INotifyPropertyChanged`, conditional blocks toggle automatically when the bound property changes:

```csharp
using System.ComponentModel;

public class AppViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; PropertyChanged?.Invoke(this, new(nameof(IsLoading))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

// Changing the property toggles the template block:
_vm.IsLoading = true;   // <template if="{IsLoading}"> content appears
_vm.IsLoading = false;  // Content disappears
```

### Text Interpolation

Inside `<template for="">` blocks, you can use `{alias.Property}` to insert values:

```html
<template for="{Products}" as="p">
  <div class="product">
    <span class="name">{p.Name}</span>
    <span class="price">${p.Price}</span>
  </div>
</template>
```

- `{p.Name}` — resolves the `Name` property on each item (reactive if item implements `INotifyPropertyChanged`)
- `{p}` — calls `ToString()` on the item itself (useful for `List<string>`)
- Mixed text works: `"Item: {p.Name} (${p.Price})"` — all expressions update reactively

### Nesting Directives

Directives can be nested — for example, a for-loop inside a conditional:

```html
<template if="{HasResults}">
  <h2>Results</h2>
  <template for="{Results}" as="r">
    <div class="result">{r.Title}</div>
  </template>
</template>
```

---

## Animations & Tweens

Lumi provides a fluent animation API for smooth property transitions.

### Fluent Animation Builder

```csharp
using Lumi.Core.Animation;

// Fade an element in
element.Animate()
    .Property("opacity", 0f, 1f)
    .Duration(0.4f)
    .Easing(Easing.EaseOutCubic)
    .Start();

// Slide an element down with a delay
element.Animate()
    .Property("opacity", 0f, 1f)
    .Duration(0.3f)
    .Delay(0.2f)
    .Easing(Easing.EaseOutCubic)
    .OnComplete(() => Console.WriteLine("Animation done!"))
    .Start();
```

### Animatable Properties

The animation system works by calling `PropertyApplier.Apply()` each frame with interpolated values. Any numeric CSS property can be animated:

- `opacity` (0.0 – 1.0)
- `width`, `height`, `min-width`, `max-width`, etc.
- `margin-top`, `margin-left`, etc.
- `padding-top`, `padding-left`, etc.
- `font-size`
- `border-radius`
- `top`, `left`, `right`, `bottom`

### Easing Functions

```csharp
Easing.Linear        // Constant speed
Easing.EaseInCubic   // Slow start
Easing.EaseOutCubic  // Slow end
Easing.EaseInOutCubic // Slow start and end
Easing.EaseInQuad    // Quadratic ease in
Easing.EaseOutQuad   // Quadratic ease out

// From CSS name string
Easing.FromName("ease-in-out") // Returns EaseInOutCubic
```

### Staggered Entrance Animations

```csharp
var cards = FindByClass("card");
float delay = 0;
foreach (var card in cards)
{
    card.ComputedStyle.Opacity = 0; // Start invisible
    card.Animate()
        .Property("opacity", 0, 1)
        .Duration(0.4f)
        .Delay(delay)
        .Easing(Easing.EaseOutCubic)
        .Start();
    delay += 0.15f; // Each card appears 150ms after the previous
}
```

### Low-Level Tween API

```csharp
// Create and manage tweens directly
var tween = new Tween(from: 0f, to: 100f, duration: 1.0f, Easing.EaseOutCubic);
tween.OnUpdate = (value) =>
{
    element.InlineStyle = string.Create(CultureInfo.InvariantCulture,
        $"width: {value:F1}px");
    element.MarkDirty();
};
tween.OnComplete = () => Console.WriteLine("Tween finished!");

AnimationExtensions.GlobalTweenEngine.Add(tween);
```

---

## Scrolling

Elements with `overflow: scroll` in CSS become scrollable containers.

### CSS Setup

```css
.scroll-container {
  max-height: 200px;
  overflow: scroll;
}
```

### Scroll Behavior

- The framework handles mouse wheel events automatically
- Children are clipped to the container's bounds
- Children use `flex-shrink: 0` by default inside scroll containers (prevents unwanted squishing)
- Hit testing is scroll-offset-aware — buttons remain clickable at their visual position after scrolling

### Programmatic Scrolling

```csharp
// Scroll to an absolute position (clamped to valid range)
element.ScrollTo(0, 100); // Scroll to Y=100

// Scroll by a relative delta
element.ScrollBy(0, 50); // Scroll down 50px

// Read scroll state
float currentScroll = element.ScrollTop;
float totalHeight = element.ScrollHeight;
float visibleHeight = element.LayoutBox.Height;
```

---

## Multi-Window Support

Lumi supports opening secondary windows from your main application window. Each secondary
window has its own element tree, style resolver, layout engine, and renderer.

### Creating a Secondary Window

Define a class that inherits from `SecondaryWindow`:

```csharp
using Lumi;

public class SettingsWindow : SecondaryWindow
{
    public SettingsWindow()
    {
        Title = "Settings";
        Width = 480;
        Height = 360;

        var dir = AppContext.BaseDirectory;
        LoadTemplate(Path.Combine(dir, "Settings.html"));
        LoadStyleSheet(Path.Combine(dir, "Settings.css"));
    }

    public override void OnReady()
    {
        var closeBtn = FindById("close-btn");
        closeBtn?.On("Click", (_, _) => Close());
    }
}
```

### Opening a Window

Use the `Windows` property (a `WindowManager`) available on any `Window`:

```csharp
public override void OnReady()
{
    var settingsBtn = FindById("settings-btn");
    settingsBtn?.On("Click", (_, _) =>
    {
        Windows?.Open(new SettingsWindow());
    });
}
```

### Closing a Window

Call `Close()` from within the secondary window, or the user can close it via the
platform close button. The `WindowManager` handles disposal of all platform resources
automatically.

```csharp
// From inside the secondary window:
Close();
```

### Closing All Windows

```csharp
Windows?.CloseAll();
```

> **Note:** Secondary windows run within the main application loop. They are updated,
> styled, laid out, and painted each frame alongside the primary window.

---

## Source Generator

Lumi includes a Roslyn source generator that eliminates `INotifyPropertyChanged`
boilerplate. Mark a partial class and its properties with `[Observable]` and the
generator creates the backing fields, setters, and change notifications for you.

### Usage

```csharp
using Lumi.Generators;

[Observable]
public partial class SettingsViewModel
{
    [Observable]
    public partial string UserName { get; set; }

    [Observable]
    public partial bool DarkMode { get; set; }

    [Observable]
    public partial int FontSize { get; set; }
}
```

The generator produces a partial class that implements `INotifyPropertyChanged` with
backing fields and property setters that call `OnPropertyChanged()`.

### Generated Output (conceptual)

```csharp
partial class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private string _generated_userName;
    public string UserName
    {
        get => _generated_userName;
        set { if (!Equals(_generated_userName, value)) { _generated_userName = value; OnPropertyChanged("UserName"); } }
    }

    // ... same for DarkMode, FontSize
}
```

### Using with Data Binding

The generated view model works directly with Lumi's binding engine:

```csharp
var vm = new SettingsViewModel { UserName = "Jane" };
BindingEngine.Bind(vm, "UserName", nameLabel, "Text");
```

> The `[Observable]` attribute is emitted by the generator itself — no additional
> runtime package is required.

---

## Hot Reload

Lumi watches your source HTML and CSS files and reloads them instantly when you save.

### Enable Hot Reload

```csharp
public MainWindow()
{
    // ... LoadTemplate / LoadStyleSheet ...

    // Point to the SOURCE files (not the bin/ copies)
    HtmlPath = Path.Combine(GetSourceDir(), "MainWindow.html");
    CssPath = Path.Combine(GetSourceDir(), "MainWindow.css");
    EnableHotReload = true;
}

private static string GetSourceDir(
    [System.Runtime.CompilerServices.CallerFilePath] string path = "")
    => Path.GetDirectoryName(path)!;
```

### How It Works

1. Content-hash polling every 500ms detects changes (immune to editor save strategies)
2. `FileSystemWatcher` provides instant acceleration for supported editors
3. 200ms debounce handles atomic writes (rename + write)
4. Changes are applied on the main thread during the next frame

### OnHtmlReloaded Callback

When HTML is reloaded, the element tree is replaced. Override `OnHtmlReloaded()` to re-register event handlers:

```csharp
public override void OnHtmlReloaded()
{
    // Re-wire handlers after HTML hot reload replaces the element tree
    var button = FindById("my-button");
    button?.On("Click", (_, _) => HandleClick());
}
```

---

## Inspector & Screenshots

### Inspector (F12)

Press **F12** at runtime to toggle the inspector overlay:

- **Blue outlines** around every element showing layout bounds
- **Box model visualization** on the hovered element:
  - Orange = margin
  - Green = padding
  - Blue = content area
- **Tooltip** showing tag, id, size, font, color, margin/padding values

### Screenshots (F5)

Press **F5** to capture a PNG screenshot to `~/Desktop/LumiScreenshots/`.

### Headless Screenshots (CLI Tool)

```bash
dotnet run --project tools/ScreenshotTool -- \
  MainWindow.html MainWindow.css output.png 1200 800
```

---

## CSS Reference

### Supported Properties (89+)

#### Box Model

| Property | Values | Example |
|----------|--------|---------|
| `width`, `height` | `px`, `%`, `em`, `rem`, `pt` | `width: 200px` |
| `min-width`, `max-width` | `px`, `%`, `em`, `rem`, `pt` | `max-width: 100%` |
| `min-height`, `max-height` | `px`, `%`, `em`, `rem`, `pt` | `min-height: 50px` |
| `margin` | 1–4 values | `margin: 8px 16px` |
| `padding` | 1–4 values | `padding: 12px 24px` |
| `border-width` | 1–4 values | `border-width: 1px` |
| `border-color` | color | `border-color: #38BDF8` |
| `border-style` | `none`, `solid`, `dashed`, `dotted`, `double` | `border-style: dashed` |
| `border-radius` | 1–4 corner values | `border-radius: 8px` or `32px 4px` |
| `box-sizing` | `content-box`, `border-box` | `box-sizing: border-box` |

#### Flexbox Layout

| Property | Values | Example |
|----------|--------|---------|
| `display` | `block`, `flex`, `none` | `display: flex` |
| `flex-direction` | `row`, `row-reverse`, `column`, `column-reverse` | `flex-direction: column` |
| `flex-wrap` | `nowrap`, `wrap`, `wrap-reverse` | `flex-wrap: wrap` |
| `justify-content` | `flex-start`, `flex-end`, `center`, `space-between`, `space-around`, `space-evenly` | `justify-content: center` |
| `align-items` | `flex-start`, `flex-end`, `center`, `stretch`, `baseline` | `align-items: center` |
| `align-self` | Same as `align-items` | `align-self: flex-end` |
| `flex-grow` | number | `flex-grow: 1` |
| `flex-shrink` | number | `flex-shrink: 0` |
| `flex-basis` | `px`, `%`, `auto` | `flex-basis: 200px` |
| `gap` | `px` | `gap: 16px` |
| `row-gap`, `column-gap` | `px` | `row-gap: 8px` |

#### Positioning

| Property | Values | Example |
|----------|--------|---------|
| `position` | `relative`, `absolute`, `fixed` | `position: absolute` |
| `top`, `right`, `bottom`, `left` | `px`, `%`, `auto` | `top: 0px` |
| `z-index` | integer | `z-index: 100` |
| `overflow` | `visible`, `hidden`, `scroll` | `overflow: scroll` |

#### Typography

| Property | Values | Example |
|----------|--------|---------|
| `color` | hex, `rgb()`, `rgba()`, named | `color: #F8FAFC` |
| `font-family` | string | `font-family: "Segoe UI"` |
| `font-size` | `px`, `em`, `rem`, `pt` | `font-size: 16px` |
| `font-weight` | `100`–`900`, `normal`, `bold` | `font-weight: 600` |
| `font-style` | `normal`, `italic` | `font-style: italic` |
| `line-height` | unitless multiplier or `px`/`em` | `line-height: 1.5` |
| `text-align` | `left`, `center`, `right` | `text-align: center` |
| `letter-spacing` | `px`, `em` | `letter-spacing: 1px` |
| `text-decoration` | `none`, `underline`, `line-through` | `text-decoration: underline` |
| `text-transform` | `none`, `uppercase`, `lowercase`, `capitalize` | `text-transform: uppercase` |
| `white-space` | `normal`, `nowrap`, `pre` | `white-space: nowrap` |
| `text-overflow` | `clip`, `ellipsis` | `text-overflow: ellipsis` |
| `word-break` | `normal`, `break-all` | `word-break: break-all` |

#### Visual

| Property | Values | Example |
|----------|--------|---------|
| `background-color` | color | `background-color: #334155` |
| `opacity` | `0.0`–`1.0` | `opacity: 0.8` |
| `visibility` | `visible`, `hidden` | `visibility: hidden` |
| `cursor` | `default`, `pointer` | `cursor: pointer` |
| `box-shadow` | `offsetX offsetY blur spread color [inset]` | `box-shadow: 0px 4px 12px rgba(0,0,0,0.3)` |

#### Transitions & Animations

| Property | Values | Example |
|----------|--------|---------|
| `transition-property` | CSS property name | `transition-property: opacity` |
| `transition-duration` | seconds or ms | `transition-duration: 0.3s` |
| `transition-timing-function` | `ease`, `linear`, `ease-in`, `ease-out`, `ease-in-out` | `transition-timing-function: ease` |

### Color Formats

```css
color: #F00;                  /* 3-digit hex */
color: #FF0000;               /* 6-digit hex */
color: #FF0000FF;             /* 8-digit hex with alpha */
color: rgb(255, 0, 0);        /* RGB */
color: rgba(255, 0, 0, 0.5);  /* RGBA with alpha 0–1 */
color: red;                   /* Named color */
color: transparent;           /* Fully transparent */
```

**Named colors:** `red`, `blue`, `green`, `yellow`, `orange`, `purple`, `cyan`, `aqua`, `magenta`, `fuchsia`, `lime`, `maroon`, `navy`, `olive`, `teal`, `silver`, `gray`/`grey`, `black`, `white`, `transparent`

### Units

| Unit | Description | Example |
|------|-------------|---------|
| `px` | Pixels (default) | `width: 200px` |
| `%` | Percentage of parent | `width: 50%` |
| `em` | Relative to element's font-size | `padding: 1.5em` |
| `rem` | Relative to root font-size (16px) | `font-size: 1.25rem` |
| `pt` | Points (1pt = 1.333px) | `font-size: 12pt` |

---

## API Reference

### Window

The base class for your application window.

```csharp
public class Window
{
    // Properties
    string Title { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    Element Root { get; }                    // Root of the element tree
    FrameMetrics FrameMetrics { get; }       // FPS and timing data
    string? HtmlPath { get; set; }           // Source HTML path for hot reload
    string? CssPath { get; set; }            // Source CSS path for hot reload
    bool EnableHotReload { get; set; }       // Enable/disable hot reload

    // Template loading
    void LoadTemplate(string path);          // Load HTML from file
    void LoadTemplateString(string html);    // Load HTML from string
    void LoadStyleSheet(string path);        // Load CSS from file
    void LoadStyleSheetString(string css);   // Load CSS from string

    // Element queries
    Element? FindById(string id);            // Find element by ID
    List<Element> FindByClass(string cls);   // Find elements by class name

    // Lifecycle (override these)
    virtual void OnReady();                  // Called once after tree is built
    virtual void OnUpdate();                 // Called every frame
    virtual void OnHtmlReloaded();           // Called after hot reload replaces HTML
    
    // Utilities
    bool SaveScreenshot(string filePath);    // Save current frame as PNG
}
```

### Element

The base class for all UI elements in the tree.

```csharp
public abstract class Element
{
    // Identity
    string? Id { get; set; }
    List<string> Classes { get; set; }
    abstract string TagName { get; }
    Dictionary<string, string> Attributes { get; set; }

    // Tree
    Element? Parent { get; }
    IReadOnlyList<Element> Children { get; }
    void AddChild(Element child);
    void RemoveChild(Element child);
    void InsertChild(int index, Element child);
    void ClearChildren();

    // Styling
    string? InlineStyle { get; set; }        // CSS inline style (highest priority)
    ComputedStyle ComputedStyle { get; }     // Read-only computed style (after cascade)

    // Layout
    LayoutBox LayoutBox { get; set; }        // Position and size after layout

    // Scrolling
    float ScrollTop { get; set; }
    float ScrollLeft { get; set; }
    float ScrollHeight { get; }              // Total content height
    float ScrollWidth { get; }               // Total content width
    void ScrollTo(float x, float y);         // Scroll to absolute position
    void ScrollBy(float dx, float dy);       // Scroll by relative delta

    // Events
    void On(string eventName, RoutedEventHandler handler);
    void Off(string eventName, RoutedEventHandler handler);

    // Focus
    bool IsFocusable { get; set; }
    bool IsFocused { get; set; }
    int TabIndex { get; set; }

    // Dirty tracking
    void MarkDirty();                        // Mark element for re-render

    // Binding
    object? DataContext { get; set; }
}
```

### TemplateEngine

Activates template directives in the element tree.

```csharp
public static class TemplateEngine
{
    // Parser function (set automatically by LumiApp, or set manually for tests)
    static Func<string, Element>? HtmlParser { get; set; }

    // Walk the tree and activate all <template for=""> and <template if=""> directives
    static void Apply(Element root, object dataContext);
}
```

### TemplateForElement

A repeater element created from `<template for="">` in HTML.

```csharp
public class TemplateForElement : Element
{
    string CollectionPath { get; set; }   // Property path to the collection (e.g., "Items")
    string ItemAlias { get; set; }        // Loop variable name (e.g., "item")
    string TemplateHtml { get; set; }     // Inner HTML to repeat per item
    void Unbind();                        // Disconnect from collection change notifications
}
```

### TemplateIfElement

A conditional element created from `<template if="">` in HTML.

```csharp
public class TemplateIfElement : Element
{
    string ConditionPath { get; set; }    // Property path to a boolean (e.g., "IsVisible")
    string TemplateHtml { get; set; }     // Inner HTML to conditionally render
    void Unbind();                        // Disconnect from property change notifications
}
```

### FrameMetrics

Access real-time performance data:

```csharp
public sealed class FrameMetrics
{
    double CurrentFps { get; }       // Instantaneous FPS
    double AverageFps { get; }       // Rolling average (120 frames)
    double TotalFrameTimeMs { get; } // Total frame time in ms
    double PaintTimeMs { get; }      // Rendering time
    double LayoutTimeMs { get; }     // Layout calculation time
    double StyleTimeMs { get; }      // Style resolution time
    double UpdateTimeMs { get; }     // Update logic time
    string GetSummary();             // Formatted summary string
}
```

### Usage in OnUpdate

```csharp
public override void OnUpdate()
{
    if (_fpsLabel != null)
    {
        _fpsLabel.Text = $"FPS: {FrameMetrics.CurrentFps:F0}";
        _fpsLabel.MarkDirty();
    }
}
```

---

## Full Example: Todo App

A minimal but complete example combining templates, template directives, events, and binding:

```csharp
// TodoWindow.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using Lumi;
using Lumi.Core;
using Lumi.Core.Binding;
using Lumi.Core.Components;

public class TodoWindow : Window
{
    private readonly TodoViewModel _vm = new();

    public TodoWindow()
    {
        Title = "Lumi Todo";
        Width = 500;
        Height = 400;

        var dir = AppContext.BaseDirectory;
        LoadTemplate(Path.Combine(dir, "Todo.html"));
        LoadStyleSheet(Path.Combine(dir, "Todo.css"));
    }

    public override void OnReady()
    {
        // Activate template directives — the <template for=""> in HTML
        // binds to the Todos collection and renders items automatically
        TemplateEngine.Apply(Root, _vm);

        // Wire up the add button
        var textbox = new LumiTextBox { Placeholder = "Add a todo..." };
        FindById("input-host")?.AddChild(textbox.Root);

        var addBtn = new LumiButton { Text = "Add", Variant = ButtonVariant.Primary };
        addBtn.OnClick = () =>
        {
            if (!string.IsNullOrWhiteSpace(textbox.Value))
            {
                _vm.Todos.Add(textbox.Value);  // UI updates automatically!
                textbox.Value = "";
            }
        };
        FindById("btn-host")?.AddChild(addBtn.Root);
    }
}

public class TodoViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Todos { get; } = new(["Buy groceries", "Walk the dog"]);
    public event PropertyChangedEventHandler? PropertyChanged;
}
```

```html
<!-- Todo.html -->
<div class="app">
  <h1 class="title">My Todos</h1>
  <div class="input-row">
    <div id="input-host" class="input-area"></div>
    <div id="btn-host"></div>
  </div>
  <div class="list-area scroll">
    <template for="{Todos}" as="todo">
      <div class="todo-item">{todo}</div>
    </template>
  </div>
  <p class="hint">Adding items updates the list automatically</p>
</div>
```

```css
/* Todo.css */
:root { --bg: #0F172A; --text: #F8FAFC; --accent: #38BDF8; }

.app {
  display: flex; flex-direction: column; padding: 32px;
  background-color: var(--bg); gap: 16px;
}
.title { color: var(--accent); font-size: 24px; font-weight: 700; }
.input-row { display: flex; flex-direction: row; gap: 8px; align-items: flex-end; }
.input-area { flex-grow: 1; }
.list-area { max-height: 250px; overflow: scroll; }
.hint { color: #64748B; font-size: 12px; }
```

---

## Tips & Best Practices

1. **Always use `InlineStyle` for dynamic styling** — `ComputedStyle` is read-only in practice (rebuilt every frame from CSS).

2. **Call `MarkDirty()` after changing InlineStyle or text** — otherwise the change won't be rendered until something else triggers a repaint.

3. **Use `CultureInfo.InvariantCulture`** for float-to-string in InlineStyle — prevents locale-dependent decimal separators breaking CSS values.

4. **Add components to `Root`** for overlays (dialogs) — they need to render above all other content.

5. **Event names are case-insensitive** — `"Click"`, `"click"`, and `"CLICK"` all work.

6. **Use hot reload during development** — edit HTML/CSS and see changes instantly without restarting.

7. **Press F12 for the inspector** — see element bounds, box model, and computed styles.

8. **Check `FrameMetrics`** for performance — if paint time is high, reduce element count or complexity.
