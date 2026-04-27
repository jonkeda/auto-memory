# Visual language

Tokens, sizes, density tiers, motion. The redesign keeps the principle that
every color and font comes from a `--vscode-*` variable.

## Color tokens

| Role | Token | Fallback |
|---|---|---|
| Page background | `--vscode-editor-background` | — |
| Page text | `--vscode-foreground` | — |
| Muted text (sub-labels, paths, helper text) | `--vscode-descriptionForeground` | `--vscode-foreground` w/ 0.7 alpha |
| Section divider | `--vscode-panel-border` | — |
| List row hover | `--vscode-list-hoverBackground` | — |
| Selected row accent | `--vscode-list-activeSelectionBackground` + `--vscode-list-activeSelectionForeground` | — |
| Buttons (primary) | `--vscode-button-background` / `…-foreground` / `…-hoverBackground` | — |
| Buttons (secondary) | `--vscode-button-secondaryBackground` / `…-secondaryForeground` | — |
| Inputs | `--vscode-input-background` / `…-foreground` / `…-border` | — |
| Focus ring | `--vscode-focusBorder` | — |
| Link | `--vscode-textLink-foreground` / `…-activeForeground` | — |
| Match highlight | `--vscode-editor-findMatchHighlightBackground` | — |
| Info banner | `--vscode-editorInfo-foreground` / `…-background` | `#014361` / `#e8f4fd` |
| Warning banner | `--vscode-editorWarning-foreground` / `…-background` | `#bf8803` / `#fff8e1` |
| Error banner | `--vscode-editorError-foreground` / `…-background` | `#b71c1c` / `#ffebee` |
| Health green | `--vscode-testing-iconPassed` | — |
| Health amber | `--vscode-testing-iconQueued` | — |
| Health red | `--vscode-testing-iconFailed` | — |
| Health calibrating | `--vscode-descriptionForeground` | — |

The current panel uses `opacity: 0.6/0.7` for muted text. Replace with
`color: var(--vscode-descriptionForeground)` so high-contrast themes
remain readable.

## Iconography

Use VS Code codicons exclusively. The webview already has access to them
via `vscode.codicon` font; load with:

```html
<link rel="stylesheet" href="${codiconCssUri}">
```

| Old emoji | New codicon |
|---|---|
| 🟢 | `$(pass)` (or `$(circle-large-filled)` colored green) |
| 🟡 | `$(warning)` |
| 🔴 | `$(error)` |
| ⚠️ | `$(alert)` |
| ✅ | `$(check)` |
| ● | `$(circle-large-filled)` |

Codicons inherit `color` so theme tokens apply directly.

## Typography

Single font family (`var(--vscode-font-family)`). One-axis scale, four
sizes:

| Class / role | Size |
|---|---|
| `--am-text-xs` (status bar, helper) | 11 px |
| `--am-text-sm` (default body) | 13 px |
| `--am-text-md` (section headings) | 15 px |
| `--am-text-lg` (overall score) | 24 px |

Headings use `font-weight: 600`; everything else inherits. Uppercase
treatment is dropped — section titles are sentence-case for readability.

## Spacing scale

```
--am-space-1: 4px
--am-space-2: 8px
--am-space-3: 12px
--am-space-4: 16px
--am-space-5: 24px
```

Use the scale; no arbitrary px values in CSS.

## Density tiers

Two density modes, controlled by `auto-memory.dashboard.density`:

| Tier | Row padding | Section gap |
|---|---|---|
| `comfortable` (default) | `--am-space-2` (8 px) vertical | `--am-space-4` (16 px) |
| `compact` | `--am-space-1` (4 px) vertical | `--am-space-3` (12 px) |

The setting is per-user (global), not per-workspace. Compact is suggested
to power-users via the overflow menu after the panel has been open for
> 3 minutes.

## Surface adaptation

Toggle via the existing `data-surface` attribute on `<html>`:

```css
[data-surface="sidebar"] {
  --am-master-detail: 1fr;     /* single column */
}
[data-surface="tab"] {
  --am-master-detail: 320px 1fr; /* master / detail */
}
```

Components that have side-by-side modes wrap their root in
`display: grid; grid-template-columns: var(--am-master-detail);`. The
sidebar gets a single column; the tab gets the master/detail layout for
free.

## Motion

Keep it minimal — VS Code users are sensitive to animation.

| Element | Transition | Duration |
|---|---|---|
| Tab underline | `transform: translateX(...)` | 120 ms ease-out |
| Row expand | `max-height` + `opacity` | 160 ms ease-out |
| New-session pulse | background fade in/out | 1500 ms total, single pulse |
| Hover backgrounds | `background` | 80 ms |

Respect `prefers-reduced-motion`: when set, all transitions become 1 ms.

```css
@media (prefers-reduced-motion: reduce) {
  * { transition-duration: 1ms !important; animation-duration: 1ms !important; }
}
```

## Banner shape

Three banner variants, sharing a single shape:

```css
.banner {
  border-left: 4px solid var(--am-banner-accent);
  background: var(--am-banner-bg);
  color: var(--am-banner-fg);
  padding: var(--am-space-2) var(--am-space-3);
  margin-bottom: var(--am-space-3);
  border-radius: 4px;
  font-size: var(--am-text-sm);
}
.banner.info    { --am-banner-bg: var(--vscode-editorInfo-background); --am-banner-fg: var(--vscode-editorInfo-foreground); --am-banner-accent: var(--vscode-editorInfo-border, #2196f3); }
.banner.warning { --am-banner-bg: var(--vscode-editorWarning-background); --am-banner-fg: var(--vscode-editorWarning-foreground); --am-banner-accent: var(--vscode-editorWarning-border, #ff9800); }
.banner.error   { --am-banner-bg: var(--vscode-editorError-background); --am-banner-fg: var(--vscode-editorError-foreground); --am-banner-accent: var(--vscode-editorError-border, #f44336); }
```

Each banner has a leading codicon, headline, body, and an optional
inline-button row. Empty banners (no body) collapse to a single line.

## High-contrast and Light themes

- Replace all `opacity` muting with `--vscode-descriptionForeground`.
- Test that `--vscode-editorInfo-*` exists in `Default Light Modern` and
  `Default Dark Modern`. If a theme is missing the token, the hex
  fallbacks above kick in.
- The selected row accent must use the `activeSelection` token pair (not
  just an opaque blue), or high-contrast themes will lose the indication.
- Codicons inherit `color`; setting `color: var(--vscode-testing-iconFailed)`
  on a codicon span paints it red.

## CSS file shape

A single `panel.css` keeps shipping (CSP-friendly). Suggested structure:

```
/* tokens */
:root { --am-space-1: 4px; ... }

/* base */
body { ... }
.muted { color: var(--vscode-descriptionForeground); }

/* primitives */
.row { ... }
.button { ... }
.banner { ... }

/* navigation */
.context-bar { ... }
.tab-bar { ... }

/* glance */
.last-session { ... }
.dimension-row { ... }

/* find */
.search-bar { ... }
.result-row { ... }
.source-chip { ... }

/* setup */
.backend-row { ... }
.instructions-row { ... }
```

No utility framework, no CSS-in-JS. The current file is ~10 lines; the
redesigned one fits comfortably under 200.
