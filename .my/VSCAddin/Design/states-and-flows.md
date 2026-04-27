# States & flows

## Data sources

The CLI (`session-recall`) auto-detects which backend to read at every
invocation. The dashboard never picks a backend — it reflects whatever the
CLI returned via the `source` field on each JSON payload.

| Backend | Detected when | `source` value | Origin |
|---|---|---|---|
| Copilot CLI SQLite | `~/.copilot/session-store.db` exists | *omitted* (default) | Original M1–M5 |
| Session-state flat-files | DB missing AND `~/.copilot/session-state/` populated | `vscode-session-state` | M6 |
| VS Code Chat transcripts | DB missing AND no session-state AND `workspaceStorage/.../GitHub.copilot-chat/transcripts/*.jsonl` exists | `vscode-chat` | M7 |
| Flat-file health | DB missing (any flat backend present) | `vscode-flat` (health only) | M8 |
| None | DB missing AND no flat data | warning JSON only | — |

## State matrix per section

### Health section

| Condition | Render |
|---|---|
| `error` field set | Plain error text |
| `overall_score != null` | Score + dimensions; if `source === 'vscode-flat'` show `.banner.info` |
| `warning` set, `overall_score == null` | `renderNoDb(...)` block |
| Anything else | Literal `"No health data available"` |

The order matters: the JS checks `overall_score` *before* `warning`, because
the M8 flat-file path returns both a score and a warning.

### Recent sessions section

| Condition | Render |
|---|---|
| `error` set | Plain error text |
| `warning` set AND `sessions` empty | `renderNoDb(...)` |
| `source === 'vscode-session-state'` | Source badge `● VS Code Copilot sessions`, then rows |
| `source === 'vscode-chat'` | Source badge `● VS Code Chat sessions`, then rows |
| SQLite (default) | Rows only, no badge |

### Search section

Only renders results once the user submits a query. A `warning` collapses to
plain text inline in the results container — there is no `renderNoDb` here
because search is initiated, not auto-loaded.

### Install-status header

Always rendered. Shows version • install dir • PATH strategy, plus dotted
counts per backend when present:

```
0.1.0 • C:\…\bin • PATH: user • 29 Copilot CLI sessions • 86 VS Code Chat sessions
```

### Instructions section

Independent of session data sources. Three strategies (A / B / C), each with
`installed: bool` and `path: string`. The B and C rows disable their button
when no workspace is open.

## "No database" state — `renderNoDb`

The fallback rendered when SQLite is missing and the section's data is also
missing.

```
┌──────────────────────────────────────────────────────┐
│ VS Code Copilot sessions detected (115 sessions)     │
│ 29 Copilot CLI sessions in ~/.copilot/session-state/ │
│ 86 Copilot Chat sessions in workspaceStorage/        │
│ Both are readable. Use Reload to refresh.            │
│                                                      │
│ [ Reload ]                                           │
└──────────────────────────────────────────────────────┘
```

When neither flat backend has data:

```
┌──────────────────────────────────────────────────────┐
│ No session database found. The GitHub Copilot CLI    │
│ creates it automatically when you run your first     │
│ agentic session.                                     │
│ Expected: ~/.copilot/session-store.db                │
│                                                      │
│ [ Reload ]                                           │
└──────────────────────────────────────────────────────┘
```

## Message protocol

All messages between the webview and the extension host use
`{ type, payload }` envelopes.

### Webview → Host

| `type` | Payload | Effect |
|---|---|---|
| `refresh` | `{ section: 'all' | 'health' | 'sessions' | 'install' | 'instructions' }` | Re-runs CLI commands, bypassing cache for the named section |
| `search` | `{ query: string }` | Runs `session-recall search --json -- <query>` |
| `showSession` | `{ id: string }` | Opens session detail (currently logs / future modal) |
| `addInstructions` | `{ strategy: 'A' \| 'B' \| 'C' }` | Writes the strategy file |
| `removeInstructions` | `{ strategy }` | Deletes the strategy file |

### Host → Webview

| `type` | Payload shape (key fields) |
|---|---|
| `health` | `{ overall_score, dims \| dimensions, top_hints, source, backends, warning?, db_path? }` |
| `sessions` | `{ sessions: [...], source?, warning?, session_state_count?, vscode_chat_count? }` |
| `searchResult` | `{ results: [{source, excerpt, date}], warning? }` |
| `installStatus` | `{ version, installDir, pathStrategy, vscode_session_count?, vscode_chat_count? }` |
| `instructionsStatus` | `{ A: {installed, path}, B: {...}, C: {...} }` |
| `refreshRequested` | `{}` — triggers `requestAll()` in webview |
| `error` | `{ message: string }` — shows top-of-page error banner for 5 s |

## Loading + error handling

- **Loading.** Each section's `<div class="content">` starts with the literal
  text `Loading…`. There is no skeleton animation. First payload arrival
  replaces it.
- **Error.** Per-section errors land in the section's content. Cross-cutting
  errors (CLI exec failure, JSON parse failure) post `{ type: 'error' }`,
  which prepends a transient red banner.

The 5-second timeout for the error banner is hard-coded in `showError()`.

## Refresh flow

```
[ Refresh button ]
       │ click
       ▼
postMessage 'refresh' { section: 'all' }
       │
       ▼
host runner.invalidateCache('all')
       │
       ▼
spawn session-recall (one per section)
       │
       ▼
postMessage 'health' / 'sessions' / 'installStatus' / 'instructionsStatus'
       │
       ▼
section render functions update DOM
```

`requestAll()` runs once at panel boot. After that, every fetch is driven by
explicit user action or by a host-pushed `refreshRequested` (e.g. after an
install/remove instructions write).
