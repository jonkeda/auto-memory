# Dashboard layout

The panel is rendered by [vscode-extension/src/panel/html.ts](../../../vscode-extension/src/panel/html.ts)
into both:

- **Sidebar view** — Activity Bar → AutoMemory icon → narrow column.
- **Editor tab** — `AutoMemory: Open Dashboard` command opens a full-width tab.

The same `panel.html` skeleton is used for both. The wrapper element carries
`data-surface="sidebar"` or `data-surface="tab"` so CSS can adapt density.

## Section order (top → bottom)

```
┌──────────────────────────────────────────────┐
│ install-status (header)                      │ ← version • install dir • PATH • counts
├──────────────────────────────────────────────┤
│ HEALTH                                       │ ← overall score, dimensions, hints
├──────────────────────────────────────────────┤
│ RECENT SESSIONS                              │ ← list of session rows, click → details
├──────────────────────────────────────────────┤
│ SEARCH                                       │ ← input + button + result list
├──────────────────────────────────────────────┤
│ COPILOT INSTRUCTIONS                         │ ← A / B / C strategies with Add/Remove
├──────────────────────────────────────────────┤
│ [ Refresh ]                                  │
└──────────────────────────────────────────────┘
```

Each section is a `<section>` with a sentence-case `<h2>` heading rendered as
small uppercase via CSS (`text-transform: uppercase; opacity: 0.7`).

## Wireframe — sidebar surface (≈ 320 px wide)

```
┌────────────────────────────────────────┐
│ 0.1.0 • C:\…\bin • PATH: user          │ install-status
│ • 29 Copilot CLI sessions              │
│ • 86 VS Code Chat sessions             │
├────────────────────────────────────────┤
│ HEALTH                                 │
│                                        │
│ ┌──────────────────────────────────┐   │
│ │ ⓘ  Flat-file mode                │   │ banner.info  (only when no SQLite)
│ │    29 CLI + 86 VS Code Chat      │   │
│ └──────────────────────────────────┘   │
│                                        │
│       8.4 / 10                         │ overall score (large, color-coded)
│                                        │
│ 🟢 Freshness                    10.0   │ row
│ 🟢 Corpus Size                  10.0   │
│ 🔴 Repo Coverage                 2.0   │
│ 🟢 Summary Coverage             10.0   │
│ 🟢 Recent Activity              10.0   │
├────────────────────────────────────────┤
│ RECENT SESSIONS                        │
│ ● VS Code Chat sessions                │ source badge
│ 2026-04-25  jonkeda/repo  Refactor…    │ session-row (clickable)
│ 2026-04-24  jonkeda/repo  M7 step 3    │
│ 2026-04-24  -             VSIX build   │
│ …                                      │
├────────────────────────────────────────┤
│ SEARCH                                 │
│ ┌────────────────────────────────┐     │
│ │ search sessions…               │ Go  │
│ └────────────────────────────────┘     │
│ [vscode-chat] panel layout… 04-23      │
│ [session-state] CLI install… 04-22     │
├────────────────────────────────────────┤
│ COPILOT INSTRUCTIONS                   │
│ A — Global              [ Add ]        │ instructions-row
│   ⚠️ Not installed — ~/.copilot/…      │
│ B — Scoped              [ Remove ]     │
│   ✅ Installed — .github/instructions… │
│ C — Settings            [ Add ]        │
│   ⚠️ Not installed — github.copilot…   │
├────────────────────────────────────────┤
│                          [ Refresh ]   │
└────────────────────────────────────────┘
```

## Wireframe — editor-tab surface (≥ 800 px wide)

The tab surface keeps the same vertical section order but allows wider rows.
Search results may show a `.full-detail` block that is hidden in the sidebar
via `[data-surface="sidebar"] #search-results .full-detail { display: none; }`.

Long-term design intent (not yet implemented): two-column flow on the tab
surface — Health + Instructions on the left, Sessions + Search on the right.
This is a future enhancement and should not be implemented without a step
spec.

## Spacing & density rules

- Outer page padding: `12px` (set on `<body>`).
- Section separator: `1px solid var(--vscode-panel-border)` top border, `8px`
  padding above the content.
- Section heading top margin: `16px`, bottom margin: `8px`.
- Row vertical padding: `4px 0`.
- Banner padding: `8px 12px`, `border-radius: 4px`, `margin-bottom: 12px`.
- Buttons: `4px 10px`, no border, theme-button colors.

## Refresh + cache

- Initial render is triggered by `requestAll()` at the bottom of `panel.js`.
- The `Refresh` button posts `{ type: 'refresh', payload: { section: 'all' } }`.
- The extension host's [runner.ts](../../../vscode-extension/src/panel/runner.ts)
  serves cached results for ~60 s; explicit refresh bypasses the cache.
