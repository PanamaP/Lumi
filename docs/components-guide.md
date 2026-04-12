# Component Usage Guide

Lumi ships a set of ready-made UI components in the `Lumi.Core.Components` namespace.
Each component wraps one or more `Element` nodes, exposes a clean property-and-callback API,
and styles itself automatically using the built-in dark theme.

Every component has a **`Root`** property — that's the `Element` you add to your
element tree with `AddChild()`. Build the component, wire up its events, drop the
root into a parent, and you're done.

```csharp
using Lumi.Core.Components;

var btn = new LumiButton { Text = "Save" };
btn.OnClick = () => Console.WriteLine("Saved!");
container.AddChild(btn.Root);
```

> All components live in `Lumi.Core.Components` — add a single `using` and you have
> access to every widget described below.

---

## Table of Contents

- [Getting Started with Components](#getting-started-with-components)
- [LumiButton](#lumibutton)
- [LumiCheckbox](#lumicheckbox)
- [LumiDialog](#lumidialog)
- [LumiDropdown](#lumidropdown)
- [LumiList](#lumilist)
- [LumiSlider](#lumislider)
- [LumiTextBox](#lumitextbox)
- [LumiRadioGroup](#lumiradiogroup)
- [LumiToggle](#lumitoggle)
- [LumiProgressBar](#lumiprogressbar)
- [LumiTabControl](#lumitabcontrol)
- [LumiTooltip](#lumitooltip)
- [ComponentStyles](#componentstyles)
- [Common Patterns](#common-patterns)

---

## Getting Started with Components

### 1. Add the namespace

```csharp
using Lumi.Core.Components;
```

### 2. Create a component

Every component is a plain class — instantiate it, set properties, and attach callbacks:

```csharp
var textBox = new LumiTextBox
{
    Label = "Email",
    Placeholder = "you@example.com"
};

textBox.OnValueChanged = value => Console.WriteLine($"Typed: {value}");
```

### 3. Add it to the tree

Components are **not** elements themselves. They *contain* an element exposed via `Root`.
Add that root to any parent in your element tree:

```csharp
myPanel.AddChild(textBox.Root);
```

### 4. Read values any time

Input components keep their state in regular properties — read them whenever you need:

```csharp
string email = textBox.Value;
```

That's the entire workflow: **create → configure → add → read**.

---

## LumiButton

A styled button with text label and variant support.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `Text` | `string` | Button label text |
| `IsDisabled` | `bool` | Disabled state — grays out and ignores clicks |
| `Variant` | `ButtonVariant` | Visual style: `Primary`, `Secondary`, or `Danger` (default: `Primary`) |
| `OnClick` | `Action?` | Invoked when the button is clicked |

### ButtonVariant Enum

```csharp
public enum ButtonVariant
{
    Primary,
    Secondary,
    Danger
}
```

### Example

```csharp
var save = new LumiButton { Text = "Save", Variant = ButtonVariant.Primary };
save.OnClick = () => SaveData();

var cancel = new LumiButton { Text = "Cancel", Variant = ButtonVariant.Secondary };
cancel.OnClick = () => GoBack();

var delete = new LumiButton { Text = "Delete", Variant = ButtonVariant.Danger };
delete.IsDisabled = true; // disabled until user confirms

toolbar.AddChild(save.Root);
toolbar.AddChild(cancel.Root);
toolbar.AddChild(delete.Root);
```

---

## LumiCheckbox

A toggle checkbox with a text label.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `IsChecked` | `bool` | Current checked state |
| `Label` | `string` | Label shown beside the checkbox |
| `OnChanged` | `Action<bool>?` | Fired when the checked state changes |

### Example

```csharp
var agree = new LumiCheckbox { Label = "I agree to the terms" };

agree.OnChanged = isChecked =>
{
    submitButton.IsDisabled = !isChecked;
};

form.AddChild(agree.Root);
```

---

## LumiDialog

A modal dialog with title bar, close button, content area, and backdrop overlay.
When opened, it covers the screen with a semi-transparent overlay and centers the
dialog panel.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root overlay element (add to top-level container) |
| `Title` | `string` | Dialog title shown in the title bar |
| `Content` | `Element?` | Content element — setting replaces the previous content |
| `IsOpen` | `bool` | Show or hide the dialog (toggles `display`) |
| `OnClose` | `Action?` | Invoked when the close button is clicked |

### Example

```csharp
var dialog = new LumiDialog { Title = "Confirm Action" };

// Build dialog content
var message = new TextElement("Are you sure you want to proceed?");

var confirmBtn = new LumiButton { Text = "Yes, proceed" };
confirmBtn.OnClick = () =>
{
    PerformAction();
    dialog.IsOpen = false;
};

var wrapper = new BoxElement();
wrapper.AddChild(message);
wrapper.AddChild(confirmBtn.Root);
dialog.Content = wrapper;

dialog.OnClose = () => dialog.IsOpen = false;

// Add to root — hidden by default
root.AddChild(dialog.Root);

// Open later
dialog.IsOpen = true;
```

> ⚠️ Add the dialog's `Root` to a top-level container so the overlay covers the
> entire window. Setting `Content` removes any previously set content element.

---

## LumiDropdown

A dropdown select control with a toggleable item list.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `Items` | `List<string>` | Item list — setting rebuilds the dropdown |
| `SelectedIndex` | `int` | Index of the currently selected item |
| `IsOpen` | `bool` | Whether the dropdown list is visible (read-only) |
| `OnSelectionChanged` | `Action<int>?` | Fired when the selection changes (receives new index) |

### Example

```csharp
var dropdown = new LumiDropdown
{
    Items = new List<string> { "Small", "Medium", "Large" }
};

dropdown.OnSelectionChanged = index =>
{
    Console.WriteLine($"Selected: {dropdown.Items[index]}");
};

form.AddChild(dropdown.Root);

// Read current selection
int chosen = dropdown.SelectedIndex;
```

---

## LumiList

A scrollable list of styled rows.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `Items` | `List<string>` | Item list — setting rebuilds the row elements |
| `OnItemClick` | `Action<int>?` | Fired when a row is clicked (receives item index) |

### Example

```csharp
var fileList = new LumiList
{
    Items = new List<string> { "README.md", "Program.cs", "App.css" }
};

fileList.OnItemClick = index =>
{
    OpenFile(fileList.Items[index]);
};

sidebar.AddChild(fileList.Root);
```

---

## LumiSlider

A range slider with a track, fill indicator, and draggable thumb.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `Value` | `float` | Current value (clamped to `Min`..`Max`) |
| `Min` | `float` | Minimum value (default: `0`) |
| `Max` | `float` | Maximum value (default: `1`) |
| `OnValueChanged` | `Action<float>?` | Fired when the value changes |

### Example

```csharp
var volume = new LumiSlider { Min = 0, Max = 100, Value = 75 };

volume.OnValueChanged = val =>
{
    Console.WriteLine($"Volume: {val:F0}%");
};

panel.AddChild(volume.Root);
```

---

## LumiTextBox

A text input field with an optional label.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `Label` | `string?` | Optional label displayed above the input |
| `Value` | `string` | Current text value |
| `Placeholder` | `string` | Placeholder text shown when empty |
| `IsReadOnly` | `bool` | Read-only mode — prevents editing |
| `InputElement` | `InputElement` | Direct access to the underlying `InputElement` |
| `OnValueChanged` | `Action<string>?` | Fired when the text value changes |

### Example

```csharp
var nameField = new LumiTextBox
{
    Label = "Full Name",
    Placeholder = "Jane Doe"
};

nameField.OnValueChanged = text =>
{
    Console.WriteLine($"Name: {text}");
};

form.AddChild(nameField.Root);

// Read value later
string name = nameField.Value;
```

---

## LumiRadioGroup

A group of radio buttons where only one option can be selected at a time.

### API

| Member | Type | Description |
|--------|------|-------------|
| Constructor | `LumiRadioGroup(List<string> options)` | Takes the list of option labels |
| `Root` | `Element` | Root element |
| `Options` | `IReadOnlyList<string>` | The option labels (set at construction) |
| `SelectedIndex` | `int` | Index of the currently selected option |
| `OnSelectionChanged` | `Action<int>?` | Fired when the selection changes (receives new index) |

### Example

```csharp
var priority = new LumiRadioGroup(new List<string> { "Low", "Medium", "High" });

priority.OnSelectionChanged = index =>
{
    Console.WriteLine($"Priority: {priority.Options[index]}");
};

form.AddChild(priority.Root);
```

---

## LumiToggle

A toggle switch with an optional label.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `IsOn` | `bool` | Current toggle state |
| `Label` | `string?` | Optional label — shown/hidden automatically |
| `OnToggle` | `Action<bool>?` | Fired when the toggle state changes |

### Example

```csharp
var darkMode = new LumiToggle { Label = "Dark Mode", IsOn = true };

darkMode.OnToggle = isOn =>
{
    ApplyTheme(isOn ? "dark" : "light");
};

settings.AddChild(darkMode.Root);
```

---

## LumiProgressBar

A progress bar supporting both determinate and indeterminate modes.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `Value` | `float` | Progress from `0` to `1` (clamped) |
| `IsIndeterminate` | `bool` | When `true`, shows an animated indeterminate state |

### Example

```csharp
// Determinate progress
var progress = new LumiProgressBar { Value = 0.0f };
panel.AddChild(progress.Root);

// Update as work completes
progress.Value = 0.5f;  // 50%
progress.Value = 1.0f;  // done

// Indeterminate (loading spinner style)
var loader = new LumiProgressBar { IsIndeterminate = true };
panel.AddChild(loader.Root);
```

---

## LumiTabControl

A tab strip with switchable content panels.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root element |
| `SelectedIndex` | `int` | Index of the active tab |
| `AddTab(string, Element)` | method | Add a tab with a title and content element |
| `OnTabChanged` | `Action<int>?` | Fired when the active tab changes (receives new index) |

### Example

```csharp
var tabs = new LumiTabControl();

var generalPanel = BuildGeneralSettings();
var advancedPanel = BuildAdvancedSettings();

tabs.AddTab("General", generalPanel);
tabs.AddTab("Advanced", advancedPanel);

tabs.OnTabChanged = index =>
{
    Console.WriteLine($"Switched to tab {index}");
};

container.AddChild(tabs.Root);
```

---

## LumiTooltip

A tooltip that appears on mouse hover. Typically attached to another element using the
static `Attach` helper.

### API

| Member | Type | Description |
|--------|------|-------------|
| `Root` | `Element` | Root tooltip element |
| `Text` | `string` | Tooltip text content |
| `Attach(Element, string)` | static method | Attaches a tooltip to a target element (auto show/hide on `mouseenter`/`mouseleave`) — returns the `LumiTooltip` instance |

### Example

```csharp
var saveBtn = new LumiButton { Text = "Save" };
LumiTooltip.Attach(saveBtn.Root, "Save your changes (Ctrl+S)");

toolbar.AddChild(saveBtn.Root);
```

> The `Attach` method wires `mouseenter` and `mouseleave` events automatically.
> You don't need to manage visibility yourself.

---

## ComponentStyles

`ComponentStyles` is a static helper class that all built-in components use for
consistent theming. You normally don't need to call it directly — every component
styles itself on construction — but it's available if you're building custom components.

### Color Palette (Dark Theme Fallbacks)

| Constant | Value | Usage |
|----------|-------|-------|
| `Background` | `#1E293B` | Window / page background |
| `Accent` | `#38BDF8` | Primary actions, active states |
| `TextColor` | `#F8FAFC` | Default text |
| `Subtle` | `#94A3B8` | Secondary text, placeholders |
| `Danger` | `#EF4444` | Destructive actions, errors |
| `Surface` | `#334155` | Cards, panels, input backgrounds |
| `Border` | `#475569` | Borders and dividers |
| `Disabled` | `#64748B` | Disabled element styling |

### Key Methods

| Method | Description |
|--------|-------------|
| `ApplyButton(Element, ButtonVariant)` | Style an element as a themed button |
| `ApplyDisabledButton(Element)` | Apply disabled button appearance |
| `ApplyLabel(Element)` | Style a label element |
| `ApplyTextInput(Element)` | Style a text input element |
| `ApplyContainer(Element, FlexDirection)` | Style a flex container |
| `ApplyOverlay(Element)` | Style a full-screen overlay |
| `ApplyDialogPanel(Element)` | Style a dialog panel |
| `ApplyListContainer(Element)` | Style a scrollable list |
| `ApplyListRow(Element)` | Style a list row |
| `ApplyRadioGroup(Element)` | Style a radio group container |
| `ApplyToggleTrack(Element, bool)` | Style a toggle track (on/off) |
| `ApplyProgressTrack(Element)` | Style a progress bar track |
| `ApplyTabHeader(Element)` | Style a tab header strip |
| `ApplyTooltip(Element)` | Style a tooltip element |
| `SetVisible(Element, bool)` | Toggle element visibility via `display` |
| `AppendStyle(Element, string)` | Append raw CSS to an element's `InlineStyle` |
| `ToRgba(Color)` | Convert a `Color` to a CSS `rgba(...)` string |

---

## Common Patterns

### Adding Components to a Window

In your window's `OnReady()` method, create components and add them to the element tree.
Use `FindById()` to locate placeholder elements defined in markup, or build the tree
entirely in code:

```csharp
protected override void OnReady()
{
    var root = Document.Body;

    var heading = new TextElement("My App");

    var btn = new LumiButton { Text = "Click Me" };
    btn.OnClick = () => Console.WriteLine("Clicked!");

    root.AddChild(heading);
    root.AddChild(btn.Root);
}
```

### Building a Form (TextBox + Dropdown + Button)

Combine multiple input components, read their values on submit:

```csharp
protected override void OnReady()
{
    var form = Document.Body;

    var name = new LumiTextBox { Label = "Name", Placeholder = "Enter your name" };
    var role = new LumiDropdown { Items = new List<string> { "Developer", "Designer", "Manager" } };
    var agree = new LumiCheckbox { Label = "I accept the terms" };

    var submit = new LumiButton { Text = "Submit" };
    submit.OnClick = () =>
    {
        if (!agree.IsChecked)
        {
            Console.WriteLine("Please accept the terms.");
            return;
        }

        Console.WriteLine($"Name: {name.Value}");
        Console.WriteLine($"Role: {role.Items[role.SelectedIndex]}");
    };

    form.AddChild(name.Root);
    form.AddChild(role.Root);
    form.AddChild(agree.Root);
    form.AddChild(submit.Root);
}
```

### Using a Dialog for Confirmation

Create the dialog once, toggle `IsOpen` to show or hide it:

```csharp
// Create dialog (added once, reused)
var dialog = new LumiDialog { Title = "Success" };
dialog.OnClose = () => dialog.IsOpen = false;
root.AddChild(dialog.Root);

// Later — show with dynamic content
var msg = new TextElement("Your changes have been saved.");
dialog.Content = msg;
dialog.IsOpen = true;
```

### Setting Up Tabs

Build each tab's content as an `Element`, then add tabs one by one:

```csharp
var tabs = new LumiTabControl();

// Tab 1 — Profile
var profilePanel = new BoxElement();
var nameField = new LumiTextBox { Label = "Display Name" };
profilePanel.AddChild(nameField.Root);

// Tab 2 — Preferences
var prefsPanel = new BoxElement();
var darkToggle = new LumiToggle { Label = "Dark Mode", IsOn = true };
var fontSize = new LumiSlider { Min = 12, Max = 24, Value = 16 };
prefsPanel.AddChild(darkToggle.Root);
prefsPanel.AddChild(fontSize.Root);

tabs.AddTab("Profile", profilePanel);
tabs.AddTab("Preferences", prefsPanel);

tabs.OnTabChanged = index =>
{
    Console.WriteLine($"Active tab: {index}");
};

root.AddChild(tabs.Root);
```

### Attaching Tooltips to Any Element

Use `LumiTooltip.Attach()` on any component's `Root` or on plain elements:

```csharp
var slider = new LumiSlider { Min = 0, Max = 100, Value = 50 };
LumiTooltip.Attach(slider.Root, "Adjust the volume level");

var deleteBtn = new LumiButton { Text = "Delete", Variant = ButtonVariant.Danger };
LumiTooltip.Attach(deleteBtn.Root, "Permanently remove this item");
```

---

> **Tip:** Components use `InlineStyle` for layout and appearance. If you need to
> customize a component beyond what its properties expose, you can append additional
> CSS with `ComponentStyles.AppendStyle(component.Root, "your: css;")`.
