# Interactions

Behavioural rules: navigation, refresh, keyboard, copy/share, focus, and
error recovery. Anything not listed here defaults to plain HTML semantics.

## Navigation

- **Tab switch** — clicking a tab updates the active tab in webview state
  and persists it under `workspaceState['autoMemory.activeTab']`. On panel
  reopen, the last-active tab is restored unless setup is incomplete (then
  Setup tab wins, exactly once).
- **Repo chip** — click toggles between *current repo* and *all repos*.
  When VS Code's `workspace.workspaceFolders` change, the chip updates and
  the list payloads are re-requested.
- **Refresh icon** — bypasses the 60 s cache. Hover tooltip:
  `Refresh — last updated 23s ago`.
- **Overflow `⋯`** — small menu with: `Open dashboard in editor tab`,
  `Diagnostics`, `Open settings`, `Reset preferences`.

## Tab routing

The active tab is part of the URL-style state: `#/glance`, `#/find`,
`#/setup`, `#/find/<id>`, `#/glance/dim/<name>`. The host can post a
`navigate` message to push the panel to any route — used by the Command
Palette commands:

- `AutoMemory: Glance` → `#/glance`
- `AutoMemory: Find` → `#/find` (focus search)
- `AutoMemory: Open Last Session` → `#/find/<latest-id>`
- `AutoMemory: Setup` → `#/setup`
- `AutoMemory: Diagnostics` → `#/setup` (auto-expanded)

## Keyboard

| Surface | Key | Action |
|---|---|---|
| Anywhere | `Esc` | Close detail / clear search |
| Anywhere | `Ctrl/Cmd + R` | Refresh (when panel focused) |
| Anywhere | `1` / `2` / `3` | Switch to Glance / Find / Setup |
| Find search input | `Enter` | Submit |
| Find search input | `↓` | Move focus to first result |
| Result list | `↑` `↓` | Move selection |
| Result list | `Enter` | Open detail |
| Result list | `Ctrl + C` | Copy summary of selected |
| Detail | `Tab` | Cycle action buttons |

Keyboard shortcuts are documented in the overflow menu under `Keyboard
shortcuts` — clicking it opens a small modal listing the bindings.

## Refresh model

```
            ┌─────────────────────┐
panel boot  │ requestAll()        │
            └─────────┬───────────┘
                      ▼
            ┌─────────────────────┐
            │ host runner cache   │ 60s TTL
            │ + file watcher      │
            └─────────┬───────────┘
                      ▼
            ┌─────────────────────┐
            │ post 'glance'       │
            │ post 'find'         │
            │ post 'setup'        │
            └─────────────────────┘
```

Three triggers invalidate the cache:

1. User clicks `⟳` (refresh button).
2. Host file watcher fires for `~/.copilot/session-state/**` or
   `workspaceStorage/**/GitHub.copilot-chat/transcripts/**`.
3. Workspace folder list changes.

The webview never owns a timer — it only re-renders when the host pushes a
new payload.

## New-session highlight

When a payload arrives that contains a session id newer than any
previously-seen id:

1. The Glance "Last session" card fades-in via opacity transition.
2. In Find, the new row gets a 1.5 s background pulse using
   `--vscode-list-highlightForeground` at low alpha.
3. No popup, no toast — the highlight is the notification.

## Search interactions

- Input auto-focuses on tab activation.
- Empty submit re-runs the default "recent sessions" query.
- Debounced live-search at 300 ms is a future enhancement; v1 only
  searches on Enter / Go click.
- Results are virtualised when count > 200 to keep scroll smooth.
- Highlighted match terms use `<mark>` styled with
  `--vscode-editor-findMatchHighlightBackground`.

## Copy / share

| Action | Source | Result on clipboard |
|---|---|---|
| `Copy summary` | session row / detail header | one-line summary string |
| `Copy as markdown` | detail header | markdown rendering of summary + turns |
| `Copy id` | row kebab | session id |
| `Copy raw JSON` | row kebab | full session JSON record |
| `Copy report` | diagnostics | markdown report |

A subtle 1.5 s status-bar message confirms each copy: `AutoMemory: copied`.
No toast / no popup.

## Detail open behaviour

- **Sidebar** click on a row → inline expand under the row, replacing
  visible body of that row only. Other rows stay collapsed. A small
  `Open in editor tab` button is in the expanded body.
- **Editor tab** click on a row → right-side detail panel updates. The
  list keeps its scroll position; the previously-selected row loses its
  accent.

## Health dimension expand

- Sidebar: click row → row expands inline; only one dimension expanded at
  a time. The auto-expanded worst-zone dimension counts; clicking another
  collapses it.
- Editor tab: click row → right-side card replaces previous card.
- Closing always returns the right pane to the worst-zone default.

## Error recovery

Every error state has a primary recovery action. Mapping:

| Failure | Primary action | Secondary |
|---|---|---|
| `spawn session-recall ENOENT` | `Reinstall binary` | `Open issue` |
| Binary returned non-zero | `Retry` | `Copy diagnostics` |
| JSON parse failed | `Retry` | `Open issue` |
| File watcher disconnected | `Refresh` | — |
| Network/git lookup for repo failed | none — fall back to folder name silently | — |

The transient red error banner (current `showError`) is removed; errors are
shown inline in the relevant tab so the user knows which surface failed.

## Settings

A minimal settings surface is exposed via:

- `AutoMemory: Open settings` (overflow menu) → opens VS Code settings
  filtered to `auto-memory.*`.
- New keys (additive — none of the existing keys are renamed):
  - `auto-memory.dashboard.defaultTab` — `glance` (default) / `find` /
    `setup`.
  - `auto-memory.dashboard.density` — `comfortable` (default) / `compact`.
  - `auto-memory.dashboard.autoFilterByRepo` — boolean, default `true`.
  - `auto-memory.dashboard.sources` — array of enabled source ids.

## Accessibility

- All interactive elements are real `<button>` / `<input>` / `<a>` so VS
  Code's screen reader pipeline works.
- Tab semantics: the tab bar uses `role="tablist"` with
  `role="tab"` / `aria-selected` per tab.
- Focus is visible: VS Code's default focus ring is used; never disabled.
- Color is never the sole signal — every health zone also has an icon and
  a text score.
- Live regions: the new-session highlight uses `aria-live="polite"` on
  the Glance "Last session" card so screen readers announce updates.
