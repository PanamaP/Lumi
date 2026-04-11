# Lumi CSS Property Reference

Complete reference for all CSS properties supported by the Lumi GUI framework, derived from the engine source (`PropertyApplier.cs`, `ComputedStyle.cs`).

---

## Table of Contents

- [Box Model — Sizing](#box-model--sizing)
- [Margin](#margin)
- [Padding](#padding)
- [Border](#border)
- [Box Sizing](#box-sizing)
- [Display](#display)
- [Position](#position)
- [Flex Layout](#flex-layout)
- [Grid Layout](#grid-layout)
- [Offsets](#offsets)
- [Z-Index](#z-index)
- [Overflow](#overflow)
- [Visual](#visual)
- [Text / Typography](#text--typography)
- [Transitions](#transitions)
- [Animation](#animation)
- [Transform](#transform)
- [CSS Custom Properties](#css-custom-properties)
- [Selectors](#selectors)
- [Units](#units)
- [Color Formats](#color-formats)
- [@-Rules](#-rules)
- [Transitionable Properties](#transitionable-properties)

---

## Box Model — Sizing

| Property | Values | Default | Description |
|------------|----------------------|---------|--------------------------------------|
| `width` | length, `auto` | `auto` | Element width. `auto` sizes to content or parent. |
| `height` | length, `auto` | `auto` | Element height. |
| `min-width` | length | `0` | Minimum width constraint. |
| `max-width` | length, `none` | `none` (∞) | Maximum width constraint. |
| `min-height` | length | `0` | Minimum height constraint. |
| `max-height` | length, `none` | `none` (∞) | Maximum height constraint. |

```css
.card {
  width: 300px;
  height: auto;
  min-width: 200px;
  max-width: 100%;
  min-height: 50px;
  max-height: none;
}
```

---

## Margin

| Property | Values | Default | Description |
|-----------------|--------|---------|--------------------------------------|
| `margin` | 1–4 lengths | `0` | Shorthand for all four margins. |
| `margin-top` | length | `0` | Top margin. |
| `margin-right` | length | `0` | Right margin. |
| `margin-bottom` | length | `0` | Bottom margin. |
| `margin-left` | length | `0` | Left margin. |

The `margin` shorthand follows standard CSS expansion:

| Values | Expansion |
|--------|-----------|
| `margin: 10px` | All four sides = `10px` |
| `margin: 10px 20px` | Top/bottom = `10px`, left/right = `20px` |
| `margin: 10px 20px 30px` | Top = `10px`, left/right = `20px`, bottom = `30px` |
| `margin: 10px 20px 30px 40px` | Top, right, bottom, left (clockwise) |

```css
.spaced {
  margin: 16px;
  margin-bottom: 24px;
}
```

---

## Padding

| Property | Values | Default | Description |
|------------------|--------|---------|--------------------------------------|
| `padding` | 1–4 lengths | `0` | Shorthand for all four paddings. |
| `padding-top` | length | `0` | Top padding. |
| `padding-right` | length | `0` | Right padding. |
| `padding-bottom` | length | `0` | Bottom padding. |
| `padding-left` | length | `0` | Left padding. |

The `padding` shorthand follows the same 1–4 value expansion as `margin`.

```css
.container {
  padding: 12px 24px;
}
```

---

## Border

### Border Width

| Property | Values | Default | Description |
|------------------------|--------|---------|--------------------------------------|
| `border-width` | 1–4 lengths | `0` | Shorthand for all border widths. |
| `border-top-width` | length | `0` | Top border width. |
| `border-right-width` | length | `0` | Right border width. |
| `border-bottom-width` | length | `0` | Bottom border width. |
| `border-left-width` | length | `0` | Left border width. |

### Border Color

| Property | Values | Default | Description |
|-------------------------|--------|---------|--------------------------------------|
| `border-color` | color | — | Shorthand for all border colors. |
| `border-top-color` | color | — | Top border color. |
| `border-right-color` | color | — | Right border color. |
| `border-bottom-color` | color | — | Bottom border color. |
| `border-left-color` | color | — | Left border color. |

### Border Style

| Property | Values | Default | Description |
|-------------------------|----------------------------------------------|---------|--------------------------------------|
| `border-style` | `none` \| `solid` \| `dashed` \| `dotted` \| `double` | `none` | Shorthand for all border styles. |
| `border-top-style` | `none` \| `solid` \| `dashed` \| `dotted` \| `double` | `none` | Top border style. |
| `border-right-style` | `none` \| `solid` \| `dashed` \| `dotted` \| `double` | `none` | Right border style. |
| `border-bottom-style` | `none` \| `solid` \| `dashed` \| `dotted` \| `double` | `none` | Bottom border style. |
| `border-left-style` | `none` \| `solid` \| `dashed` \| `dotted` \| `double` | `none` | Left border style. |

### Border Radius

| Property | Values | Default | Description |
|-------------------------------|--------|---------|--------------------------------------|
| `border-radius` | 1–4 lengths | `0` | Shorthand (TL TR BR BL). |
| `border-top-left-radius` | length | `0` | Top-left corner radius. |
| `border-top-right-radius` | length | `0` | Top-right corner radius. |
| `border-bottom-right-radius` | length | `0` | Bottom-right corner radius. |
| `border-bottom-left-radius` | length | `0` | Bottom-left corner radius. |

### Border Shorthand

| Property | Values | Description |
|----------|--------------------------------|--------------------------------------|
| `border` | `[width] [style] [color]` | Sets width, style, and color at once. |

```css
.bordered {
  border: 1px solid #333;
  border-radius: 8px;
}

.custom-border {
  border-top-width: 2px;
  border-top-style: dashed;
  border-top-color: red;
  border-bottom-left-radius: 12px;
  border-bottom-right-radius: 12px;
}
```

---

## Box Sizing

| Property | Values | Default | Description |
|------------|----------------------------------|------------|--------------------------------------|
| `box-sizing` | `content-box` \| `border-box` | `border-box` | How width/height are calculated. `border-box` includes padding and border in the element's total size. |

```css
.element {
  box-sizing: border-box;
  width: 200px;
  padding: 16px;
  border: 2px solid gray;
  /* Total rendered width: 200px */
}
```

---

## Display

| Property | Values | Default | Description |
|-----------|------------------------------------------|---------|--------------------------------------|
| `display` | `block` \| `flex` \| `grid` \| `none` | `block` | Layout mode. `none` hides the element entirely. |

```css
.hidden { display: none; }
.row    { display: flex; }
.grid   { display: grid; }
```

---

## Position

| Property | Values | Default | Description |
|------------|----------------------------------------------|----------|--------------------------------------|
| `position` | `relative` \| `absolute` \| `fixed` | `relative` | Positioning scheme. `absolute` positions relative to the nearest positioned ancestor. `fixed` positions relative to the viewport. |

```css
.overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
}
```

---

## Flex Layout

| Property | Values | Default | Description |
|-------------------|------------------------------------------------------------------|------------|--------------------------------------|
| `flex-direction` | `row` \| `row-reverse` \| `column` \| `column-reverse` | `column` | Main axis direction. |
| `flex-wrap` | `nowrap` \| `wrap` \| `wrap-reverse` | `nowrap` | Whether items wrap to new lines. |
| `justify-content` | `flex-start` \| `flex-end` \| `center` \| `space-between` \| `space-around` \| `space-evenly` | `flex-start` | Alignment along the main axis. |
| `align-items` | `flex-start` \| `flex-end` \| `center` \| `stretch` \| `baseline` | `stretch` | Alignment along the cross axis. |
| `align-self` | `flex-start` \| `flex-end` \| `center` \| `stretch` \| `baseline` | — | Per-item cross-axis override. |
| `flex-grow` | number | `0` | How much an item grows relative to siblings. |
| `flex-shrink` | number | `1` | How much an item shrinks relative to siblings. |
| `flex-basis` | length, `auto` | `auto` | Initial main-axis size before grow/shrink. |
| `flex` | `none` \| `auto` \| `initial` \| `<grow> [<shrink>] [<basis>]` | — | Shorthand for grow, shrink, and basis. |
| `gap` | length | `0` | Gap between flex items (both axes). |
| `row-gap` | length | `0` | Gap between rows. |
| `column-gap` | length | `0` | Gap between columns. |

`flex` shorthand expansion:

| Value | Equivalent |
|-------------|-----------------------------------|
| `none` | `flex: 0 0 auto` |
| `auto` | `flex: 1 1 auto` |
| `initial` | `flex: 0 1 auto` |
| `flex: 2` | `flex: 2 1 0` |
| `flex: 1 0 200px` | grow=1, shrink=0, basis=200px |

> **Note:** The default `flex-direction` in Lumi is `column`, which differs from the CSS specification default of `row`.

```css
.toolbar {
  display: flex;
  flex-direction: row;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}

.sidebar {
  flex: 0 0 250px;
}

.content {
  flex: 1;
}
```

---

## Grid Layout

| Property | Values | Default | Description |
|--------------------------|--------|---------|--------------------------------------|
| `grid-template-columns` | string | — | Column track definitions (e.g., `1fr 2fr 1fr`). |
| `grid-template-rows` | string | — | Row track definitions. |
| `grid-gap` | length | `0` | Gap between grid cells. |

```css
.dashboard {
  display: grid;
  grid-template-columns: 1fr 2fr 1fr;
  grid-template-rows: auto 1fr auto;
  grid-gap: 16px;
}
```

---

## Offsets

| Property | Values | Default | Description |
|----------|----------------|---------|--------------------------------------|
| `top` | length, `auto` | `auto` | Offset from the top edge. |
| `right` | length, `auto` | `auto` | Offset from the right edge. |
| `bottom` | length, `auto` | `auto` | Offset from the bottom edge. |
| `left` | length, `auto` | `auto` | Offset from the left edge. |

Used with `position: absolute` or `position: fixed`.

```css
.tooltip {
  position: absolute;
  top: 100%;
  left: 50%;
}
```

---

## Z-Index

| Property | Values | Default | Description |
|-----------|---------|---------|--------------------------------------|
| `z-index` | integer | `0` | Stacking order. Higher values render on top. |

```css
.modal   { z-index: 100; }
.overlay { z-index: 99; }
```

---

## Overflow

| Property | Values | Default | Description |
|--------------|----------------------------------------------|-----------|--------------------------------------|
| `overflow` | `visible` \| `hidden` \| `scroll` | `visible` | Shorthand for both axes. |
| `overflow-x` | `visible` \| `hidden` \| `scroll` | `visible` | Horizontal overflow behavior. |
| `overflow-y` | `visible` \| `hidden` \| `scroll` | `visible` | Vertical overflow behavior. |

```css
.scrollable {
  overflow-y: scroll;
  overflow-x: hidden;
  max-height: 400px;
}
```

---

## Visual

| Property | Values | Default | Description |
|--------------------|---------------------------------------------------------------|---------------|--------------------------------------|
| `background-color` | color | `transparent` | Element background color. |
| `background-image` | `url(...)` \| `linear-gradient(...)` \| `radial-gradient(...)` | — | Background image or gradient. |
| `background` | color, `url(...)`, gradient | — | Shorthand for background properties. |
| `opacity` | `0`..`1` | `1` | Element transparency. `0` = fully transparent. |
| `visibility` | `visible` \| `hidden` | `visible` | Whether element is painted. Hidden elements still occupy space. |
| `box-shadow` | `offsetX offsetY [blur [spread]] color [inset]` \| `none` | `none` | Drop shadow around the element. |
| `cursor` | string | `"default"` | Mouse cursor style. |
| `pointer-events` | `auto` \| `none` | `auto` | `none` disables hit testing on the element. |

```css
.panel {
  background-color: #ffffff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  opacity: 0.95;
}

.gradient-bg {
  background-image: linear-gradient(to bottom, #6200ee, #3700b3);
}

.ghost {
  pointer-events: none;
  opacity: 0.5;
}
```

---

## Text / Typography

| Property | Values | Default | Description |
|--------------------------|----------------------------------------------------------------|-------------|--------------------------------------|
| `color` | color | `black` | Text color. |
| `font-family` | string | `"sans-serif"` | Font family name. |
| `font-size` | length | `16px` | Text size. |
| `font-weight` | `normal` \| `bold` \| `lighter` \| `bolder` \| `100`–`900` | `400` | Text weight. `normal` = 400, `bold` = 700. |
| `font-style` | `normal` \| `italic` | `normal` | Text style. |
| `font` | `[style] [weight] size[/line-height] family` | — | Shorthand for font properties. |
| `line-height` | number | `1.2` | Line height as a multiplier of font size. |
| `text-align` | `left` \| `right` \| `center` | `left` | Horizontal text alignment. |
| `letter-spacing` | length | `0` | Space between characters. |
| `white-space` | `normal` \| `nowrap` \| `pre` | `normal` | Whitespace handling. |
| `text-overflow` | `clip` \| `ellipsis` | `clip` | How overflowed text is displayed. |
| `word-break` | `normal` \| `break-all` | `normal` | Word breaking behavior. |
| `text-decoration` | `none` \| `underline` \| `line-through` | `none` | Text decoration line. |
| `text-decoration-line` | `none` \| `underline` \| `line-through` | `none` | Alias for `text-decoration`. |
| `text-transform` | `none` \| `uppercase` \| `lowercase` \| `capitalize` | `none` | Text case transformation. |

```css
.heading {
  font: bold 24px/1.4 "Segoe UI";
  color: #1a1a1a;
  letter-spacing: -0.5px;
}

.label {
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.truncated {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
```

---

## Transitions

| Property | Values | Default | Description |
|------------------------------|----------------------------------------------------------------|---------|--------------------------------------|
| `transition-property` | property name(s), comma-separated, or `all` | — | Which properties to animate on change. |
| `transition-duration` | time (`s` or `ms`) | — | Duration of the transition. |
| `transition-timing-function` | `linear` \| `ease` \| `ease-in` \| `ease-out` \| `ease-in-out` | — | Easing curve for the transition. |

```css
.button {
  background-color: #6200ee;
  transition-property: background-color, opacity;
  transition-duration: 0.2s;
  transition-timing-function: ease-in-out;
}

.button:hover {
  background-color: #3700b3;
}
```

---

## Animation

| Property | Values | Default | Description |
|-------------------------------|------------------------------------------------------------------|---------|--------------------------------------|
| `animation-name` | keyframe name | — | Name of the `@keyframes` rule to use. |
| `animation-duration` | time (`s` or `ms`) | — | Length of one animation cycle. |
| `animation-delay` | time (`s` or `ms`) | `0s` | Delay before animation starts. |
| `animation-iteration-count` | number \| `infinite` | `1` | How many times to repeat. |
| `animation-direction` | `normal` \| `reverse` \| `alternate` \| `alternate-reverse` | `normal` | Direction of playback. |
| `animation-fill-mode` | `none` \| `forwards` \| `backwards` \| `both` | `none` | How styles apply before/after animation. |
| `animation-timing-function` | `linear` \| `ease` \| `ease-in` \| `ease-out` \| `ease-in-out` | `ease` | Easing curve. |
| `animation` | `name duration [timing-function] [delay] [iteration-count] [direction] [fill-mode]` | — | Shorthand for all animation properties. |

```css
@keyframes fade-in {
  from { opacity: 0; }
  to   { opacity: 1; }
}

@keyframes slide-up {
  0%   { transform: translateY(20px); opacity: 0; }
  100% { transform: translateY(0);    opacity: 1; }
}

.modal {
  animation: fade-in 0.3s ease-out forwards;
}

.toast {
  animation-name: slide-up;
  animation-duration: 0.4s;
  animation-timing-function: ease-out;
  animation-fill-mode: forwards;
}
```

---

## Transform

| Property | Values | Default | Description |
|--------------------|-----------------------------------------------------------------|---------|--------------------------------------|
| `transform` | See transform functions below \| `none` | `none` | Apply 2D transformations. |
| `transform-origin` | `x y` (keywords or %) | `50% 50%` | Origin point for transformations. |

### Transform Functions

| Function | Syntax | Description |
|----------------|-------------------------|--------------------------------------|
| `translate` | `translate(x, y)` | Move element by x and y. |
| `translateX` | `translateX(x)` | Move element horizontally. |
| `translateY` | `translateY(y)` | Move element vertically. |
| `scale` | `scale(x, y)` | Scale element. |
| `scaleX` | `scaleX(x)` | Scale horizontally. |
| `scaleY` | `scaleY(y)` | Scale vertically. |
| `rotate` | `rotate(deg)` | Rotate element. |
| `skew` | `skew(x, y)` | Skew element. |
| `skewX` | `skewX(x)` | Skew horizontally. |
| `skewY` | `skewY(y)` | Skew vertically. |

`transform-origin` accepts keyword values (`left`, `center`, `right`, `top`, `bottom`) or percentage values.

```css
.rotate-on-hover:hover {
  transform: rotate(5deg) scale(1.05);
  transform-origin: center center;
}

.slide-left {
  transform: translateX(-100%);
}
```

---

## CSS Custom Properties

Define custom properties (CSS variables) with the `--` prefix and reference them with `var()`.

| Syntax | Description |
|-------------------------------|--------------------------------------|
| `--name: value` | Define a custom property. |
| `var(--name)` | Use a custom property value. |
| `var(--name, fallback)` | Use with a fallback if undefined. |

Custom properties cascade and inherit like any other CSS property. Define them on `:root` for global scope or on any element for local scope.

```css
:root {
  --primary: #6200ee;
  --radius: 8px;
  --spacing-sm: 8px;
  --spacing-md: 16px;
  --spacing-lg: 24px;
}

.button {
  background-color: var(--primary);
  border-radius: var(--radius);
  padding: var(--spacing-sm) var(--spacing-md);
}

.card {
  border-radius: var(--radius);
  padding: var(--spacing-lg);
  background-color: var(--card-bg, #ffffff);
}
```

---

## Selectors

### Basic Selectors

| Selector | Syntax | Description |
|-----------|-------------|--------------------------------------|
| Type | `div` | Matches elements by tag name. |
| ID | `#id` | Matches by `id` attribute. |
| Class | `.class` | Matches by `class` attribute. |
| Universal | `*` | Matches all elements. |

### Combinator Selectors

| Combinator | Syntax | Description |
|------------------|-------------|--------------------------------------|
| Descendant | `div span` | Matches `span` anywhere inside `div`. |
| Child | `div > span` | Matches `span` that is a direct child of `div`. |
| Adjacent sibling | `h1 + p` | Matches `p` immediately following `h1`. |
| General sibling | `h1 ~ p` | Matches any `p` sibling after `h1`. |
| Grouped | `.a, .b` | Matches `.a` or `.b`. |

### Pseudo-Classes

| Pseudo-Class | Description |
|----------------------------|--------------------------------------|
| `:hover` | Element under the mouse pointer. |
| `:focus` | Element that has focus. |
| `:active` | Element being activated (e.g., pressed). |
| `:disabled` | Disabled element. |
| `:root` | Root element. |
| `:first-child` | First child of its parent. |
| `:last-child` | Last child of its parent. |
| `:nth-child(An+B)` | Matches by position formula. |
| `:nth-last-child(An+B)` | Matches by position from end. |
| `:not(selector)` | Negation — matches elements not matching the selector. |
| `:is(selector)` | Matches any element matching the selector list. |

### Attribute Selectors

| Selector | Description |
|---------------------|--------------------------------------|
| `[attr]` | Has the attribute. |
| `[attr="val"]` | Attribute equals value. |
| `[attr~="val"]` | Attribute word list contains value. |
| `[attr\|="val"]` | Attribute equals or starts with `val-`. |
| `[attr^="val"]` | Attribute starts with value. |
| `[attr$="val"]` | Attribute ends with value. |
| `[attr*="val"]` | Attribute contains value. |

### Selector Examples

```css
/* Type + class */
button.primary { background-color: var(--primary); }

/* Descendant */
.sidebar .nav-item { padding: 8px 16px; }

/* Direct child */
.menu > .menu-item { border-bottom: 1px solid #eee; }

/* Adjacent sibling */
h2 + p { margin-top: 0; }

/* Pseudo-classes */
.button:hover { opacity: 0.9; }
.button:active { transform: scale(0.98); }
.input:focus { border-color: var(--primary); }
.input:disabled { opacity: 0.5; pointer-events: none; }

/* Structural pseudo-classes */
li:first-child { margin-top: 0; }
li:last-child { border-bottom: none; }
tr:nth-child(2n) { background-color: #f5f5f5; }

/* Negation */
.item:not(:last-child) { margin-bottom: 8px; }

/* Attribute selectors */
[data-theme="dark"] { background-color: #1a1a1a; color: #fff; }
```

---

## Units

### Length Units

| Unit | Description |
|--------|--------------------------------------|
| `px` | Pixels (absolute). |
| `em` | Relative to the element's font size. |
| `rem` | Relative to the root font size. |
| `pt` | Points (1pt = 1.333px). |
| `%` | Percentage of the parent's corresponding dimension. |
| `vh` | 1% of viewport height. |
| `vw` | 1% of viewport width. |
| `vmin` | 1% of the smaller viewport dimension. |
| `vmax` | 1% of the larger viewport dimension. |

### calc() Expressions

Use `calc()` to combine units and perform arithmetic:

```css
.sidebar {
  width: calc(100% - 250px);
  padding: calc(8px + 1em);
  font-size: calc(14px + 0.25vw);
}
```

### Time Units

| Unit | Description |
|------|--------------------------------------|
| `s` | Seconds. |
| `ms` | Milliseconds. |

### Angle Units

| Unit | Description |
|--------|--------------------------------------|
| `deg` | Degrees (360 per full rotation). |
| `rad` | Radians (2π per full rotation). |
| `turn` | Turns (1 = full rotation). |

---

## Color Formats

### Hex

| Format | Example | Description |
|------------|-------------|--------------------------------------|
| `#RGB` | `#f00` | Short hex (red). |
| `#RGBA` | `#f008` | Short hex with alpha. |
| `#RRGGBB` | `#ff0000` | Full hex. |
| `#RRGGBBAA` | `#ff000080` | Full hex with alpha. |

### Functional

| Format | Example | Description |
|--------|--------------------------------|--------------------------------------|
| `rgb` | `rgb(255, 0, 0)` | Red, green, blue (0–255). |
| `rgba` | `rgba(255, 0, 0, 0.5)` | RGB with alpha (0–1). |
| `hsl` | `hsl(0, 100%, 50%)` | Hue (0–360), saturation, lightness. |
| `hsla` | `hsla(0, 100%, 50%, 0.5)` | HSL with alpha (0–1). |

### Named Colors

All 147 CSS named colors are supported via the built-in `CssColors` table. Examples:

```css
.examples {
  color: red;
  background-color: cornflowerblue;
  border-color: transparent;
}
```

---

## @-Rules

### @media

Viewport-based media queries for responsive layouts.

```css
@media (max-width: 768px) {
  .sidebar {
    display: none;
  }
  .content {
    width: 100%;
  }
}

@media (min-width: 1200px) {
  .container {
    max-width: 1140px;
  }
}
```

### @keyframes

Define animation keyframes referenced by `animation-name`.

```css
@keyframes pulse {
  0%   { transform: scale(1); }
  50%  { transform: scale(1.05); }
  100% { transform: scale(1); }
}

.icon {
  animation: pulse 2s ease-in-out infinite;
}
```

---

## Transitionable Properties

The following properties are auto-detected as transitionable and can be animated with `transition-property: all` or by name:

| Property | Category |
|-------------------|--------------------|
| `opacity` | Visual |
| `width` | Sizing |
| `height` | Sizing |
| `margin-top` | Margin |
| `margin-right` | Margin |
| `margin-bottom` | Margin |
| `margin-left` | Margin |
| `padding-top` | Padding |
| `padding-right` | Padding |
| `padding-bottom` | Padding |
| `padding-left` | Padding |
| `border-radius` | Border |
| `font-size` | Typography |
| `background-color`| Visual |

```css
.card {
  background-color: #fff;
  opacity: 1;
  padding: 16px;
  transition-property: background-color, opacity, padding;
  transition-duration: 0.3s;
  transition-timing-function: ease;
}

.card:hover {
  background-color: #f5f5f5;
  padding: 20px;
}
```
