# M4 — Dashboard webview panel

**Goal:** A polished webview panel is available both as a pinned sidebar view and as a full-width editor tab. It shows health, recent sessions, search, install status, and Copilot instructions management — all without leaving VS Code.

**Depends on:** M1 (scaffold), M2 (binary), M3 (instructions)

---

## Deliverables

- `AutoMemoryPanel` class: single implementation, renders in both sidebar and editor tab
- All five sections implemented and data-backed
- Live instructions status badges with Add/Remove buttons wired to `InstructionsManager`
- `child_process.execFile`-based runner (no shell, injection-safe)
- 60-second result cache with manual Refresh
- Basic HTML/CSS styling (VS Code theme variables — no external CSS framework)

---

## Panel surfaces

| Surface | How opened | Width | Persistence |
|---|---|---|---|
| Sidebar | Activity Bar icon click | Narrow (~300px) | Pinned, survives navigation |
| Editor tab | `auto-memory.openDashboard` command or status bar click | Full editor width | Closes on tab close |

The `AutoMemoryPanel` constructor receives a `context: 'sidebar' \| 'tab'` flag. The sidebar variant collapses the session detail view; the tab variant shows it inline.

---

## Architecture

```
src/
  panel/
    AutoMemoryPanel.ts     ← WebviewPanel + WebviewViewProvider (dual-mode)
    runner.ts              ← execFile wrapper, returns parsed JSON
    cache.ts               ← 60-second TTL cache keyed by command string
  sidebar/
    SidebarProvider.ts     ← implements WebviewViewProvider, delegates to AutoMemoryPanel
  webview/
    panel.html             ← single HTML template
    panel.css              ← VS Code theme-variable styles
    panel.js               ← message handler, section rendering
```

### Message protocol (extension ↔ webview)

All messages are typed objects `{ type: string, payload: unknown }`.

| Direction | `type` | Payload |
|---|---|---|
| ext → web | `health` | `HealthResult` JSON |
| ext → web | `sessions` | `ListResult` JSON |
| ext → web | `searchResult` | `SearchResult` JSON |
| ext → web | `installStatus` | `{ version, installDir, pathStrategy }` |
| ext → web | `instructionsStatus` | `{ a, b, c }` each `{ installed, path }` |
| web → ext | `refresh` | `{ section: 'health'\|'sessions'\|'search'\|'all' }` |
| web → ext | `search` | `{ query: string }` |
| web → ext | `showSession` | `{ id: string }` |
| web → ext | `addInstructions` | `{ strategy: 'A'\|'B'\|'C' }` |
| web → ext | `removeInstructions` | `{ strategy: 'A'\|'B'\|'C' }` |

---

## Sections

### 1 — Install status

Displayed as a compact header bar at the top of the panel.

```
session-recall 0.1.0  •  ~/.local/auto-memory  •  PATH: userPath  [Reinstall]
```

Data source: `BinaryManager.installedVersion()` + settings read.

### 2 — Health

- Calls `session-recall health --json`
- Renders a table: zone icon + dimension name + score bar (0–10) + detail text
- Overall score shown as a large number with colour (green ≥7, amber ≥4, red <4)
- Hint list below the table (non-green dimensions only)

### 3 — Recent sessions

- Calls `session-recall list --json --limit 10`
- Renders as a scrollable list: date chip + repo tag + summary text
- Clicking a row sends `showSession` message → extension calls `session-recall show <id> --json` → result rendered in an expandable detail pane below the list

### 4 — Search

- Text input + **Search** button
- Sends `search` message with query string
- Results rendered as cards: source type badge + excerpt + date + session link
- Empty query shows the warning returned by the binary (exit 0 with warning JSON)

### 5 — Copilot instructions

```
Strategy A  Global (~/.copilot/)         ✅ Installed    [Remove]
Strategy B  Scoped (.github/instructions) ⚠️ Not installed [Add]
Strategy C  settings.json entry           ⚠️ Not installed [Add]
```

- Status loaded on panel open via `instructionsStatus` message
- Each **Add** / **Remove** button sends `addInstructions` / `removeInstructions` message
- Extension responds with updated `instructionsStatus` — panel re-renders badges without full reload

---

## Runner (`src/panel/runner.ts`)

```typescript
async function run(binary: string, args: string[]): Promise<string> {
  return new Promise((resolve, reject) => {
    execFile(binary, args, { timeout: 10_000 }, (err, stdout) => {
      if (err) reject(err);
      else resolve(stdout);
    });
  });
}
```

- `binary` is the absolute path from `BinaryManager.binaryPath()` — never a shell string
- Timeout: 10 seconds (health command may run multiple DB queries)
- Stderr is discarded for display; errors surface as a red banner in the webview

---

## Acceptance criteria

- [ ] Sidebar panel renders on Activity Bar click with all five sections visible
- [ ] Editor tab panel opens via Command Palette / status bar; shows same data
- [ ] Health section renders all 9 dimensions with correct zone colours
- [ ] Session list loads; clicking a row shows session detail inline
- [ ] Search returns results for a known term; empty query shows warning (no crash)
- [ ] Instructions section shows correct ✅/⚠️ for each strategy based on actual file state
- [ ] Add/Remove buttons update the badge without closing or reloading the panel
- [ ] Refresh button re-runs the underlying command and updates all stale sections
- [ ] Panel renders correctly with both light and dark VS Code themes
- [ ] No `eval()`, no `innerHTML` with unsanitised content, CSP header set on webview
