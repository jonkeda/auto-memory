# UI components

Atomic and composite pieces used in the panel. Class names are exactly those
in [panel.css](../../../vscode-extension/media/panel.css) and
[panel.js](../../../vscode-extension/media/panel.js).

## Banner — `.banner.info`

Used to surface a non-blocking informational message at the top of a section.
Currently used by the Health section when the CLI runs in flat-file mode.

```
┌──────────────────────────────────────────────────────┐
│ Flat-file mode — 29 CLI + 86 VS Code Chat sessions   │
│ Install Copilot CLI for full SQLite-backed health.   │
└──────────────────────────────────────────────────────┘
```

CSS:

```css
.banner.info {
  background: var(--vscode-editorInfo-background, #e8f4fd);
  color:      var(--vscode-editorInfo-foreground, #014361);
  border-left: 4px solid var(--vscode-editorInfo-border, #2196f3);
  padding: 8px 12px;
  margin-bottom: 12px;
  border-radius: 4px;
  font-size: 0.9em;
}
```

Variants reserved for future use (not yet styled): `.banner.warning`,
`.banner.error`. Add them when an actual code path needs them — do not
pre-emptively style.

## Row — `.row`

Generic two-column flex row used by Health dimensions, instructions, and
session lists.

```
┌──────────────────────────────────────────────────────┐
│ left content                          right content  │
└──────────────────────────────────────────────────────┘
```

```css
.row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 0;
}
```

## Session row — `.row.session-row`

A clickable variant of `.row`. Renders `date  repo  summary` as plain text
with `cursor: pointer`. Click posts `{ type: 'showSession', payload: { id } }`
to the host.

Future intent: separate spans for date / repo / summary so each can have its
own truncation rules. Today the row is a single `textContent` assignment.

## Source badge

Small uppercase-style label above a list to indicate which backend produced
the data. Inline-styled today; should migrate to a `.source-badge` class.

```
● VS Code Copilot sessions
● VS Code Chat sessions
```

Rendered when `data.source === 'vscode-session-state'` (M6) or
`'vscode-chat'` (M7). No badge is shown when the SQLite backend is active —
that's the implicit default.

## Overall health score

A large numeric display, color-coded by `scoreColour(score)`:

| Score range | Token | Visual |
|---|---|---|
| `>= 7` | `--vscode-testing-iconPassed` | green |
| `>= 4` | `--vscode-testing-iconQueued` | amber |
| `< 4`  | `--vscode-testing-iconFailed` | red |
| `null` | `inherit` | default fg |

```
8.4 / 10        ← 24px, color-coded
```

## Zone icon

Single emoji prefix on dimension rows. Function `zoneIcon(zone)` in
`panel.js` maps `'green' → 🟢`, `'amber' → 🟡`, anything else → 🔴.

Note: the JSON contract uses uppercase zones (`GREEN` / `AMBER` / `RED` /
`CALIBRATING`). The current `zoneIcon` lower-case-checks; this is a known
mismatch tracked separately. CALIBRATING currently renders as 🔴 — should be
a neutral icon (e.g. ⚪). Fix in a follow-up step.

## Status badges — `.badge-ok`, `.badge-warn`

Inline text colors used by the instructions section title.

```css
.badge-ok   { color: var(--vscode-testing-iconPassed); }
.badge-warn { color: var(--vscode-testing-iconQueued); }
```

## Buttons

All buttons share a single style:

```css
button {
  background: var(--vscode-button-background);
  color:      var(--vscode-button-foreground);
  border: 0;
  padding: 4px 10px;
  cursor: pointer;
}
button:hover { background: var(--vscode-button-hoverBackground); }
```

Three button placements:

1. `#refresh` — bottom of panel.
2. `#search-btn` — inline with the search input.
3. Per-row Add/Remove in the instructions section. Disabled with a
   `title="Open a workspace folder first"` tooltip when the strategy targets a
   workspace file but no folder is open.

## Input — search

```css
input {
  background: var(--vscode-input-background);
  color:      var(--vscode-input-foreground);
  border: 1px solid var(--vscode-input-border);
  padding: 4px;
  width: 100%;
  box-sizing: border-box;
}
```

Single search input with adjacent `Go` button. Enter key support is *not*
currently wired — the user must click. A future step should add an Enter
listener.

## Instructions row

Composite row used for each of the three instruction strategies (A / B / C).
Two-line left side (title + path), button on the right.

```
┌──────────────────────────────────────────────────────┐
│ A — Global                                  [ Add  ] │
│   ⚠️ Not installed — ~/.copilot/copilot-instr…       │
└──────────────────────────────────────────────────────┘
```

The path label uses `opacity: 0.6; font-size: 11px` for de-emphasis.

## Hints list — `.hints`

Below dimensions, when `data.top_hints` is non-empty:

```css
.hints {
  margin-top: 8px;
  padding-left: 20px;
  font-size: 12px;
  opacity: 0.8;
}
```

Today the dimensions emit empty hints, so this list is rarely shown. Design
intent: when M8 dimensions return non-empty `Hint` strings on RED/AMBER
zones, the dashboard renders the top three.
