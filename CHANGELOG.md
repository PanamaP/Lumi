# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0-preview.1] - 2026-04-12

Initial preview release of Lumi, a .NET 10 native C# GUI framework using HTML/CSS, SkiaSharp, and SDL3.

### Added

- **Core framework:** Element tree, routed events, and HTML/CSS templating engine
- **CSS engine:** 86+ properties, selectors, cascade, specificity, CSS variables, `calc()`, media queries, and keyframe animations
- **Layout:** Yoga-based flexbox and CSS Grid layout engine
- **Rendering:** SkiaSharp GPU-accelerated rendering with CPU fallback, image loading, and caching
- **Text:** HarfBuzz text shaping with multi-script and emoji font fallback support
- **Components:** Button, Checkbox, Slider, Dialog, Dropdown, TextBox, List, RadioGroup, Toggle, ProgressBar, TabControl, and Tooltip
- **Data binding:** One-way, two-way, and one-time binding with `INotifyPropertyChanged` support
- **Animations:** Tween engine, easing functions, and CSS transitions
- **Developer tools:** Hot reload, F12 inspector, and F5 screenshot capture
- **Source generator:** `[Observable]` attribute for automatic `INotifyPropertyChanged` boilerplate generation
- **Platform:** SDL3 windowing, clipboard integration, and system preferences detection
- **Navigation:** Router with route parameters and history management
- **Accessibility:** Foundation with ARIA attribute parsing and high-contrast theme support
- **CI/CD:** GitHub Actions workflows for build/test and NuGet package publishing
- **Samples:** 5 sample applications — HelloWorld, TodoApp, Dashboard, FormDemo, and StressTest
- **Documentation:** Getting Started guide, CSS Reference, API Reference, and Components Guide
- **Templates:** `dotnet new lumi` project template for quick project scaffolding

### Fixed

- **InputElement height:** Added intrinsic measure function and `min-height: 36px` so text inputs render at a usable size
- **Dropdown overlay:** Dropdown list now renders at the root element with `position: absolute` and `z-index: 10000`, preventing it from pushing sibling elements down
- **Tooltip positioning:** Tooltips render at the root element with auto-positioning (tries right → left → bottom → top) to stay fully visible; `pointer-events: none` prevents hover interference
- **Slider interaction:** Increased thumb size to 24px, track width to 300px, and fixed thumb centering so dragging no longer requires pixel-perfect mouse placement
- **AlignSelf default:** Changed default `align-self` from `stretch` to `auto` (CSS spec), so children correctly inherit their parent's `align-items` value — fixes nav link centering, checkbox indicator centering, and bar chart alignment
- **CSS variable resolution:** Split style resolution into two passes (custom properties first, then regular properties) so `var()` references resolve against final custom property values — fixes theme switching when higher-specificity rules override CSS variables
- **GetAbsoluteBounds:** Fixed double-counting of parent positions since LayoutBox already stores absolute coordinates

[Unreleased]: https://github.com/PanamaP/Lumi/compare/v0.3.0-preview.1...HEAD
[0.3.0-preview.1]: https://github.com/PanamaP/Lumi/releases/tag/v0.3.0-preview.1
