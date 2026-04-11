# Lumi — API Reference

Complete API reference for the Lumi GUI framework. Every public class, property, method, and enum — documented with examples.

---

## Table of Contents

1. [LumiApp](#lumiapp)
2. [Window](#window)
3. [Element](#element)
4. [BoxElement](#boxelement)
5. [TextElement](#textelement)
6. [ImageElement](#imageelement)
7. [InputElement](#inputelement)
8. [Event System](#event-system)
9. [Data Binding](#data-binding)
10. [Navigation](#navigation)
11. [Animation](#animation)
12. [Theming](#theming)
13. [CSS Selectors](#css-selectors)
14. [Enums](#enums)

---

## LumiApp

**Namespace:** `Lumi`

The entry point for every Lumi application. There's exactly one static method — call it and your app runs.

### Methods

| Method | Description |
|--------|-------------|
| `static void Run(Window window)` | Starts the application with the given window. Blocks until the window is closed. |

`Run` creates the SDL3 window, initializes the Skia renderer, sets up input handling, and starts the animation loop. Everything is managed automatically — you just pass a `Window` and go.

```csharp
// Program.cs — the entire entry point
LumiApp.Run(new MainWindow());
```

---

## Window

**Namespace:** `Lumi`

Base class for application windows. Subclass it, load your HTML/CSS, and override lifecycle methods to wire up behavior.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | `"Lumi"` | Window title shown in the title bar. |
| `Width` | `int` | `800` | Window width in pixels. |
| `Height` | `int` | `600` | Window height in pixels. |
| `EnableHotReload` | `bool` | `false` | When `true`, watches HTML/CSS files and reloads on change. |
| `Theme` | `ThemeManager` | — | Theme manager for light/dark mode switching. Read-only. |
| `Root` | `Element` | — | The root element of the visual tree. Read-only. |
| `FrameMetrics` | `FrameMetrics` | — | Per-frame timing metrics. Read-only. |
| `Windows` | `WindowManager?` | — | Window manager for secondary windows. Read-only. |

### Methods

| Method | Description |
|--------|-------------|
| `void LoadTemplate(string path)` | Load an HTML template from a file path. |
| `void LoadTemplateString(string html)` | Load an HTML template from a string. |
| `void LoadStyleSheet(string path)` | Load a CSS stylesheet from a file path. |
| `void LoadStyleSheetString(string css)` | Load a CSS stylesheet from a string. |
| `Element? FindById(string id)` | O(1) indexed lookup by element ID. Returns `null` if not found. |
| `List<Element> FindByClass(string className)` | O(1) indexed lookup by CSS class name. |
| `void RegisterFont(string familyName, string filePath)` | Register a custom font family from a file. |
| `bool SaveScreenshot(string filePath)` | Save a PNG screenshot of the window. Returns `true` on success. |

### Virtual Methods

Override these in your `Window` subclass to hook into the lifecycle:

| Method | Description |
|--------|-------------|
| `void OnReady()` | Called once after template and styles are loaded. Wire up events here. |
| `void OnUpdate()` | Called each frame before painting. Use for per-frame logic. |
| `void OnHtmlReloaded()` | Called after hot reload replaces HTML. Default implementation calls `OnReady()`. |

### Example

```csharp
using Lumi;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "My App";
        Width = 1024;
        Height = 768;
        LoadTemplate("MainWindow.html");
        LoadStyleSheet("MainWindow.css");
    }

    public override void OnReady()
    {
        var btn = FindById("submit-btn");
        btn?.On("Click", (sender, e) =>
        {
            Console.WriteLine("Clicked!");
        });
    }

    public override void OnUpdate()
    {
        // Runs every frame — update animations, check state, etc.
    }
}
```

---

## Element

**Namespace:** `Lumi.Core`

Abstract base class for all elements in the visual tree. Every element in a Lumi UI — boxes, text, images, inputs — inherits from `Element`.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string?` | `null` | Element ID for lookups. |
| `Classes` | `ClassList` | — | CSS class list. |
| `Parent` | `Element?` | `null` | Parent element. Read-only (set internally). |
| `Children` | `IReadOnlyList<Element>` | — | Child elements. Read-only. |
| `ComputedStyle` | `ComputedStyle` | — | Resolved style values after CSS cascade. Read-only. |
| `LayoutBox` | `LayoutBox` | — | Layout position and size after layout pass. |
| `IsDirty` | `bool` | — | Element needs repaint. |
| `IsLayoutDirty` | `bool` | — | Element needs layout recalculation. |
| `ScrollTop` | `float` | `0` | Vertical scroll position. |
| `ScrollLeft` | `float` | `0` | Horizontal scroll position. |
| `ScrollHeight` | `float` | `0` | Total scrollable content height. |
| `ScrollWidth` | `float` | `0` | Total scrollable content width. |
| `IsFocusable` | `bool` | `false` | Whether the element can receive focus. |
| `TabIndex` | `int` | `0` | Tab navigation order. |
| `IsFocused` | `bool` | `false` | Whether the element currently has focus. |
| `IsDraggable` | `bool` | `false` | Whether the element can be dragged. |
| `InlineStyle` | `string?` | `null` | Inline CSS (same as HTML `style` attribute). |
| `Attributes` | `Dictionary<string, string>` | — | HTML attributes. |
| `IsVisible` | `bool` | — | `true` if display is not `none`. Computed, read-only. |
| `DataContext` | `object?` | `null` | Data binding context. Inherited from ancestors when `null`. |
| `TagName` | `string` | — | Tag name (e.g. `"div"`, `"span"`). Abstract, read-only. |
| `Accessibility` | `AccessibilityInfo` | — | Accessibility metadata. Read-only. |

### Methods

| Method | Description |
|--------|-------------|
| `void On(string eventName, RoutedEventHandler handler)` | Subscribe to a routed event by name. |
| `void Off(string eventName, RoutedEventHandler handler)` | Unsubscribe from a routed event. |
| `void AddChild(Element child)` | Append a child element. |
| `void RemoveChild(Element child)` | Remove a child element. |
| `void InsertChild(int index, Element child)` | Insert a child at the given index. |
| `void ClearChildren()` | Remove all children. |
| `void MarkDirty()` | Mark this element for repaint. Propagates up to parent. |
| `void ScrollTo(float x, float y)` | Scroll to an absolute position (clamped to bounds). |
| `void ScrollBy(float dx, float dy)` | Scroll by a relative delta. |
| `Element DeepClone()` | Deep clone this element and its entire subtree. |

### Drag & Drop Events

| Event | Type | Description |
|-------|------|-------------|
| `OnDragStart` | `Action<DragData>?` | Fired when a drag begins. |
| `OnDragOver` | `Action<DragData>?` | Fired while dragging over this element. |
| `OnDrop` | `Action<DragData>?` | Fired when an element is dropped here. |
| `OnDragEnd` | `Action?` | Fired when a drag operation ends. |

### Supported Event Names

These strings can be passed to `On()` and `Off()`:

| Event | Description |
|-------|-------------|
| `"Click"` | Element was clicked. |
| `"MouseDown"` | Mouse button pressed. |
| `"MouseUp"` | Mouse button released. |
| `"MouseMove"` | Mouse moved over element. |
| `"MouseEnter"` | Mouse entered element bounds. |
| `"MouseLeave"` | Mouse left element bounds. |
| `"KeyDown"` | Key pressed while focused. |
| `"KeyUp"` | Key released while focused. |
| `"Focus"` | Element received focus. |
| `"Blur"` | Element lost focus. |
| `"Scroll"` | Element was scrolled. |
| `"input"` | Input value changed. |

### Example

```csharp
var panel = new BoxElement("div") { Id = "panel" };
panel.Classes = new ClassList("card", "shadow");

var label = new TextElement { Text = "Hello, world!" };
panel.AddChild(label);

panel.On("Click", (sender, e) =>
{
    Console.WriteLine($"Clicked on {sender.TagName}");
});
```

---

## BoxElement

**Namespace:** `Lumi.Core`

Generic container element. Maps to `<div>`, `<section>`, `<nav>`, `<header>`, `<footer>`, `<main>`, `<button>`, `<a>`, and other block/container HTML tags.

### Constructor

```csharp
public BoxElement(string tagName = "div")
```

When `tagName` is `"button"` or `"a"`, the element is automatically set to `IsFocusable = true`.

### Example

```html
<div class="toolbar">
    <button id="save-btn">Save</button>
    <button id="cancel-btn">Cancel</button>
</div>
```

```csharp
// Creating elements in code
var toolbar = new BoxElement("div");
toolbar.Classes = new ClassList("toolbar");

var saveBtn = new BoxElement("button") { Id = "save-btn" };
var cancelBtn = new BoxElement("button") { Id = "cancel-btn" };

toolbar.AddChild(saveBtn);
toolbar.AddChild(cancelBtn);
```

---

## TextElement

**Namespace:** `Lumi.Core`

Displays text content. TagName is always `"span"`.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | The text content to display. |

### Example

```html
<span class="greeting">Welcome back!</span>
```

```csharp
var greeting = new TextElement { Text = "Welcome back!" };
greeting.Classes = new ClassList("greeting");
```

---

## ImageElement

**Namespace:** `Lumi.Core`

Displays an image. TagName is always `"img"`.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Source` | `string?` | Path to the image file. |
| `NaturalWidth` | `float` | Original width of the loaded image. |
| `NaturalHeight` | `float` | Original height of the loaded image. |

### Example

```html
<img src="assets/logo.png" class="logo" />
```

```csharp
var logo = new ImageElement { Source = "assets/logo.png" };
logo.Classes = new ClassList("logo");
```

---

## InputElement

**Namespace:** `Lumi.Core`

Interactive input element. TagName is `"input"`. `IsFocusable` is `true` by default.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `InputType` | `string` | `"text"` | Input type: `"text"`, `"password"`, `"checkbox"`, `"range"`, etc. |
| `Value` | `string` | `""` | Current input value. |
| `Placeholder` | `string` | `""` | Placeholder text shown when empty. |
| `IsDisabled` | `bool` | `false` | Disabled state. |
| `IsChecked` | `bool` | `false` | Checked state (for checkboxes). |
| `CursorPosition` | `int` | `0` | Cursor index in the text. |
| `SelectionStart` | `int` | `0` | Start of the selection range. |
| `SelectionEnd` | `int` | `0` | End of the selection range. |
| `HasSelection` | `bool` | — | `true` when a selection exists. Computed, read-only. |

### Events

| Event | Type | Description |
|-------|------|-------------|
| `ValueChanged` | `Action<string>?` | Fired when `Value` changes. |

### Methods

| Method | Description |
|--------|-------------|
| `void DeleteSelection()` | Delete the currently selected text. |
| `void ClearSelection()` | Collapse the selection to the cursor position. |
| `void ResetBlink()` | Reset the caret blink timer. |

### Example

```html
<input id="username" type="text" placeholder="Enter username" />
<input id="remember" type="checkbox" />
```

```csharp
var username = FindById("username") as InputElement;
username!.ValueChanged += (newValue) =>
{
    Console.WriteLine($"Username: {newValue}");
};

var remember = FindById("remember") as InputElement;
remember!.On("Click", (sender, e) =>
{
    var checkbox = (InputElement)sender;
    Console.WriteLine($"Remember me: {checkbox.IsChecked}");
});
```

---

## Event System

**Namespace:** `Lumi.Core`

Lumi uses a routed event system inspired by WPF. Events propagate through the tree in three phases: **Tunnel** (root → target), **Direct** (at target), and **Bubble** (target → root). Standard handlers registered with `On()` fire during the Direct and Bubble phases.

### RoutedEvent

Base class for all routed events.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Event name (e.g. `"Click"`). |
| `Target` | `Element` | The element that triggered the event. |
| `Source` | `Element` | The original source element. |
| `Handled` | `bool` | Set to `true` to stop propagation. |
| `Phase` | `RoutingPhase` | Current routing phase. |

### RoutedMouseEvent

Extends `RoutedEvent` with mouse-specific data.

| Property | Type | Description |
|----------|------|-------------|
| `X` | `float` | Mouse X coordinate. |
| `Y` | `float` | Mouse Y coordinate. |
| `Button` | `int` | Mouse button index. |

### RoutedKeyEvent

Extends `RoutedEvent` with keyboard-specific data.

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Key name. |
| `Shift` | `bool` | Shift modifier held. |
| `Ctrl` | `bool` | Ctrl modifier held. |
| `Alt` | `bool` | Alt modifier held. |

### RoutedEventHandler

```csharp
delegate void RoutedEventHandler(Element sender, RoutedEvent e);
```

### RoutingPhase

| Value | Description |
|-------|-------------|
| `Tunnel` | Root → target (capture phase). |
| `Direct` | At the target element. |
| `Bubble` | Target → root. |

### Example

```csharp
// Stop a click from bubbling up
button.On("Click", (sender, e) =>
{
    e.Handled = true;
    Console.WriteLine("Button clicked — event won't bubble.");
});

// Check for modifier keys
panel.On("KeyDown", (sender, e) =>
{
    var ke = (RoutedKeyEvent)e;
    if (ke.Ctrl && ke.Key == "S")
    {
        Console.WriteLine("Ctrl+S pressed!");
        e.Handled = true;
    }
});
```

---

## Data Binding

**Namespace:** `Lumi.Core.Binding`

Lumi's data binding system connects your C# objects to the UI. Implement `INotifyPropertyChanged` on your view models and bindings stay in sync automatically.

### BindingEngine

Manages all active bindings between source objects and UI elements.

| Method | Description |
|--------|-------------|
| `void Bind(Element target, string targetProperty, object source, BindingExpression expr)` | Create a binding between a source property and a target element property. |
| `void UpdateAll()` | Force-update all active bindings. |
| `void ClearAll()` | Remove all bindings. |

```csharp
engine.Bind(element, "Text", viewModel, new BindingExpression
{
    Path = "Name",
    Mode = BindingMode.OneWay
});
```

### BindingExpression

Describes a single binding. Can be created in code or parsed from a binding string.

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | Dot-separated property path (e.g. `"User.Name"`). |
| `Mode` | `BindingMode` | `OneWay`, `TwoWay`, or `OneTime`. |
| `Converter` | `string?` | Optional converter name (e.g. `"upper"`). |
| `FallbackValue` | `string?` | Value to use when the source is `null`. |

**Static methods:**

| Method | Description |
|--------|-------------|
| `BindingExpression Parse(string expr)` | Parse a binding expression string. |
| `bool IsBindingExpression(string? value)` | Check if a string is a binding expression. |

**Binding expression syntax:**

```
{Binding PropertyPath, Mode=TwoWay, Converter=upper, FallbackValue=N/A}
```

### BindingMode

| Value | Description |
|-------|-------------|
| `OneWay` | Source → target (default). |
| `TwoWay` | Source ↔ target. |
| `OneTime` | Source → target, once at bind time. |

### TemplateEngine

Resolves template directives in HTML.

```csharp
TemplateEngine.Apply(root, dataContext);
```

**Supported directives:**

| Directive | Description |
|-----------|-------------|
| `<template for="item in Items">` | Iterate over a collection. Use `{item.PropertyName}` inside. |
| `<template if="IsVisible">` | Conditionally render content based on a boolean property. |

```html
<template for="task in Tasks">
    <div class="task-item">
        <span>{task.Title}</span>
        <span class="status">{task.Status}</span>
    </div>
</template>

<template if="HasError">
    <div class="error-banner">Something went wrong.</div>
</template>
```

### BindingContext

Static helper for resolving the effective data context.

```csharp
var ctx = BindingContext.GetEffectiveDataContext(element);
```

Walks up the parent chain to find the nearest non-null `DataContext`.

### ItemsRenderer

Renders a collection into child elements, with support for `INotifyCollectionChanged`.

```csharp
renderer.BindItemsSource(container, collection, templateFactory);
```

When items are added, removed, or moved in the collection, the UI updates automatically.

### Full Example

```csharp
public class TodoViewModel : INotifyPropertyChanged
{
    private string _newItem = "";
    public string NewItem
    {
        get => _newItem;
        set { _newItem = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TodoItem> Items { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}
```

```html
<div data-context="{Binding}">
    <input id="new-item" value="{Binding NewItem, Mode=TwoWay}" placeholder="Add a task..." />
    <template for="item in Items">
        <div class="todo-item">
            <span>{item.Title}</span>
        </div>
    </template>
</div>
```

---

## Navigation

**Namespace:** `Lumi.Core.Navigation`

Client-side routing for single-window apps with multiple "pages."

### Router

Manages route-to-page mappings, including parameterized routes.

| Member | Type | Description |
|--------|------|-------------|
| `Router(Element container)` | Constructor | Create a router that swaps content inside `container`. |
| `void Register(string pattern, Func<Element> pageFactory)` | Method | Register a route with a page factory. |
| `void Register(string pattern, Func<RouteParameters, Element> pageFactory)` | Method | Register a parameterized route (e.g. `"user/{id}"`). |
| `void Navigate(string route)` | Method | Navigate to a route. |
| `void GoBack()` | Method | Navigate to the previous route. |
| `CurrentRoute` | `string` | The current route. Read-only. |
| `CanGoBack` | `bool` | `true` if there's history to go back to. Read-only. |
| `RouteChanged` | `Action<string>?` | Event fired when the route changes. |

### NavigationHost

Convenience wrapper that pairs a container element with a `Router`.

| Property | Type | Description |
|----------|------|-------------|
| `Root` | `Element` | The container element. |
| `Router` | `Router` | The router instance. |

### RouteParameters

A case-insensitive `Dictionary<string, string>` containing extracted route parameters.

### Example

```csharp
var container = FindById("page-container")!;
var router = new Router(container);

router.Register("home", () => BuildHomePage());
router.Register("settings", () => BuildSettingsPage());
router.Register("user/{id}", (parameters) =>
{
    var userId = parameters["id"];
    return BuildUserPage(userId);
});

router.RouteChanged += (route) =>
{
    Console.WriteLine($"Navigated to: {route}");
};

router.Navigate("home");
```

```html
<nav>
    <button id="nav-home">Home</button>
    <button id="nav-settings">Settings</button>
</nav>
<div id="page-container"></div>
```

---

## Animation

**Namespace:** `Lumi.Core.Animation`

Lumi supports three animation approaches: the fluent tween API, CSS transitions, and CSS `@keyframes`.

### Fluent Tween API (AnimationBuilder)

Chain methods to build animations in code. Call `Animate()` on any element to start.

```csharp
element.Animate()
    .Property("opacity", 0, 1)
    .Property("width", 100, 300)
    .Duration(0.5f)
    .Easing(Easing.EaseOutCubic)
    .Delay(0.1f)
    .OnComplete(() => Console.WriteLine("Done"))
    .Start();
```

**AnimationBuilder methods:**

| Method | Description |
|--------|-------------|
| `.Property(string name, float from, float to)` | Animate a CSS property between two values. |
| `.Duration(float seconds)` | Set animation duration. |
| `.Easing(Func<float, float> easing)` | Set easing function. |
| `.Delay(float seconds)` | Set delay before animation starts. |
| `.OnComplete(Action callback)` | Callback when animation finishes. |
| `.Start()` | Start the animation. |

**Animatable properties:**

`opacity`, `width`, `height`, `border-radius`, `font-size`, `margin-top`, `margin-right`, `margin-bottom`, `margin-left`, `padding-top`, `padding-right`, `padding-bottom`, `padding-left`

### Easing Functions

| Function | Description |
|----------|-------------|
| `Easing.Linear` | Constant speed. |
| `Easing.EaseInCubic` | Slow start, fast end. |
| `Easing.EaseOutCubic` | Fast start, slow end. |
| `Easing.EaseInOutCubic` | Slow start and end. |
| `Easing.EaseInQuad` | Quadratic ease in. |
| `Easing.EaseOutQuad` | Quadratic ease out. |

**Resolve by name:**

```csharp
var easing = Easing.FromName("ease-out"); // Returns EaseOutCubic
```

Supported names: `"linear"`, `"ease"`, `"ease-in"`, `"ease-out"`, `"ease-in-out"`

### CSS Transitions (TransitionManager)

CSS transitions are managed automatically. Define them in CSS and Lumi handles the interpolation.

```css
.card {
    opacity: 1;
    width: 200px;
    transition: opacity 0.3s ease, width 0.5s ease-out;
}

.card.collapsed {
    opacity: 0;
    width: 0px;
}
```

**Transitionable properties:**

`opacity`, `width`, `height`, `margin-top`, `margin-right`, `margin-bottom`, `margin-left`, `padding-top`, `padding-right`, `padding-bottom`, `padding-left`, `border-radius`, `font-size`, `background-color`

### CSS @keyframes (KeyframePlayer)

| Method | Description |
|--------|-------------|
| `void Register(KeyframeAnimation animation)` | Register a keyframe animation. |
| `void Play(Element element, string animationName, float duration, int iterationCount, AnimationDirection direction)` | Play a registered animation on an element. |

**AnimationDirection:**

| Value | Description |
|-------|-------------|
| `Normal` | Plays forward. |
| `Reverse` | Plays backward. |
| `Alternate` | Alternates forward and backward. |
| `AlternateReverse` | Alternates backward and forward. |

**AnimationFillMode:**

| Value | Description |
|-------|-------------|
| `None` | No styles applied outside animation. |
| `Forwards` | Retains final keyframe styles. |
| `Backwards` | Applies first keyframe styles during delay. |
| `Both` | Combines Forwards and Backwards. |

```css
@keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
}

.modal {
    animation: fadeIn 0.3s ease forwards;
}
```

---

## Theming

**Namespace:** `Lumi.Core`

Built-in light and dark theme support with CSS variables.

### ThemeManager

| Member | Type | Description |
|--------|------|-------------|
| `Mode` | `ThemeMode` | Current theme: `Light`, `Dark`, or `System`. |
| `IsDarkMode` | `bool` | Resolved dark-mode flag. Read-only. |
| `CurrentVariables` | `IReadOnlyDictionary<string, string>` | Active CSS variable values. Read-only. |
| `void SetTheme(ThemeMode mode)` | Method | Set the theme mode. |
| `void Toggle()` | Method | Toggle between light and dark. |
| `void SetSystemPreference(bool systemIsDark)` | Method | Update the OS preference. |
| `void ApplyTo(Element root)` | Method | Apply CSS variables to a root element. |
| `ThemeChanged` | `Action<bool>?` | Event fired when dark mode changes. Argument is `true` for dark. |

### ThemeMode

| Value | Description |
|-------|-------------|
| `Light` | Force light theme. |
| `Dark` | Force dark theme. |
| `System` | Follow the OS preference. |

### Built-in CSS Variables

Use these in your CSS with `var(--variable-name)`. They automatically switch between light and dark palettes.

| Variable | Light | Dark |
|----------|-------|------|
| `--bg-primary` | `#ffffff` | `#0f172a` |
| `--bg-secondary` | `#f1f5f9` | `#1e293b` |
| `--bg-tertiary` | `#e2e8f0` | `#334155` |
| `--text-primary` | `#0f172a` | `#f8fafc` |
| `--text-secondary` | `#475569` | `#94a3b8` |
| `--text-muted` | `#94a3b8` | `#64748b` |
| `--border-color` | `#cbd5e1` | `#475569` |
| `--accent` | `#3b82f6` | `#3b82f6` |
| `--accent-hover` | `#2563eb` | `#60a5fa` |
| `--error` | `#ef4444` | `#f87171` |
| `--success` | `#22c55e` | `#4ade80` |
| `--warning` | `#f59e0b` | `#fbbf24` |

### Example

```csharp
// Toggle on button click
FindById("theme-toggle")?.On("Click", (s, e) =>
{
    Theme.Toggle();
});

// React to theme changes
Theme.ThemeChanged += (isDark) =>
{
    Console.WriteLine(isDark ? "Dark mode" : "Light mode");
};
```

```css
body {
    background-color: var(--bg-primary);
    color: var(--text-primary);
}

.card {
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
}

.btn-primary {
    background: var(--accent);
}

.btn-primary:hover {
    background: var(--accent-hover);
}
```

---

## CSS Selectors

Lumi supports a broad subset of CSS selectors.

### Simple Selectors

| Selector | Example | Description |
|----------|---------|-------------|
| Type | `div` | Matches elements by tag name. |
| ID | `#my-id` | Matches an element by its ID. |
| Class | `.my-class` | Matches elements by class name. |
| Universal | `*` | Matches all elements. |

### Combinators

| Combinator | Example | Description |
|------------|---------|-------------|
| Descendant | `div span` | Any `span` inside a `div`. |
| Child | `div > span` | Direct child `span` of a `div`. |
| Adjacent sibling | `h1 + p` | `p` immediately after an `h1`. |
| General sibling | `h1 ~ p` | Any `p` after an `h1` at the same level. |

### Compound & Grouped

| Pattern | Example | Description |
|---------|---------|-------------|
| Compound | `div.foo#bar:hover` | Multiple selectors on one element. |
| Grouped | `.a, .b` | Matches `.a` or `.b`. |

### Pseudo-classes

| Pseudo-class | Description |
|--------------|-------------|
| `:hover` | Mouse is over the element. |
| `:focus` | Element has focus. |
| `:active` | Element is being activated (pressed). |
| `:disabled` | Element is disabled. |
| `:root` | The root element. |
| `:first-child` | First child of its parent. |
| `:last-child` | Last child of its parent. |
| `:nth-child(An+B)` | Matches by position formula. |
| `:nth-last-child(An+B)` | Matches by position from the end. |
| `:not(selector)` | Negation — matches elements that don't match the inner selector. |
| `:is(selector)` | Matches any element that matches the inner selector. |

### Attribute Selectors

| Selector | Description |
|----------|-------------|
| `[attr]` | Has the attribute. |
| `[attr="value"]` | Attribute equals value. |
| `[attr~="value"]` | Attribute contains word. |
| `[attr\|="value"]` | Attribute equals or starts with value followed by `-`. |
| `[attr^="value"]` | Attribute starts with value. |
| `[attr$="value"]` | Attribute ends with value. |
| `[attr*="value"]` | Attribute contains value. |

### At-rules

| Rule | Description |
|------|-------------|
| `@media` | Media queries for responsive layouts. |
| `@keyframes` | Keyframe animations. |

---

## Enums

Reference for all public enums in the framework.

### Layout

| Enum | Values |
|------|--------|
| `DisplayMode` | `Block`, `Flex`, `Grid`, `None` |
| `Position` | `Relative`, `Absolute`, `Fixed` |
| `FlexDirection` | `Row`, `RowReverse`, `Column`, `ColumnReverse` |
| `FlexWrap` | `NoWrap`, `Wrap`, `WrapReverse` |
| `JustifyContent` | `FlexStart`, `FlexEnd`, `Center`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `AlignItems` | `FlexStart`, `FlexEnd`, `Center`, `Stretch`, `Baseline` |
| `Overflow` | `Visible`, `Hidden`, `Scroll` |
| `Visibility` | `Visible`, `Hidden` |
| `BoxSizing` | `ContentBox`, `BorderBox` |

### Text

| Enum | Values |
|------|--------|
| `TextAlign` | `Left`, `Right`, `Center` |
| `FontStyle` | `Normal`, `Italic` |
| `WhiteSpace` | `Normal`, `NoWrap`, `Pre` |
| `TextOverflow` | `Clip`, `Ellipsis` |
| `WordBreak` | `Normal`, `BreakAll` |
| `TextDecoration` | `None`, `Underline`, `LineThrough` |
| `TextTransform` | `None`, `Uppercase`, `Lowercase`, `Capitalize` |

### Border

| Enum | Values |
|------|--------|
| `BorderStyle` | `None`, `Solid`, `Dashed`, `Dotted`, `Double` |
