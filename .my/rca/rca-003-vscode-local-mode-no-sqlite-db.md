# RCA-003 — "no database found" because VS Code Copilot uses flat-file storage, not SQLite

**Date:** 2026-04-26  
**Severity:** P1 — extension is entirely non-functional for VS Code Copilot users  
**Status:** Partial fix (detection + guidance); full flat-file reader is tracked separately

---

## Symptom

Dashboard shows "no database found" even though the user has 44+ Copilot sessions stored locally at:

```
C:\Users\<user>\.copilot\session-state\<uuid>\
  workspace.yaml
  events.jsonl
  vscode.metadata.json
  checkpoints\index.md
  files\
```

No file named `session-store.db` exists anywhere under `~/.copilot`.

---

## Root Cause

The `session_recall` / `session-recall` tool was designed for the **GitHub Copilot CLI** (`gh copilot` / agentic terminal sessions). The CLI agent writes a single SQLite database:

```
~/.copilot/session-store.db
```

**VS Code Copilot** (inline chat, agent panel, sidebar) uses a completely different storage backend: one folder per session under `~/.copilot/session-state/<uuid>/`. This folder contains:

| File | Contents |
|---|---|
| `workspace.yaml` | Session metadata: id, repository, branch, summary, created\_at |
| `events.jsonl` | Full event stream (user messages, assistant turns, tool calls) |
| `vscode.metadata.json` | VS Code–specific metadata (firstUserMessage, timestamps) |
| `checkpoints/index.md` | Checkpoint index |
| `files/` | Files touched during the session |

The `session-recall` binary only knows how to open a SQLite connection. It has no code path for the flat-file format. When `~/.copilot/session-store.db` is absent, it throws `DatabaseNotFoundException` and the dashboard shows the no-DB onboarding card — which was written assuming the user just hadn't run any CLI sessions yet.

### Why this was missed

The tool was originally built by the project author who uses the Copilot CLI agent. The VS Code Copilot storage format was never in scope because the author's machine had a populated `session-store.db`. The two storage formats coexist silently.

---

## Fix — Phase 1 (this PR): Detection + guidance

The CLI's `DatabaseNotFoundException` handler (in `HealthCommand` and `ListCommand`) now:

1. Checks whether `~/.copilot/session-state/` exists and contains at least one session folder
2. If so, includes `"storage_format": "vscode-session-state"` and `"session_state_count": N` in the no-DB JSON response
3. The extension renders a distinct onboarding card explaining the VS Code vs CLI distinction

## Fix — Phase 2 (future): Flat-file reader

Add a `VsCodeSessionReader` to `AutoMemory.Core` that reads `workspace.yaml` + `events.jsonl` from each `session-state/<uuid>/` folder and exposes the same query interface as the SQLite path. This allows `list`, `files`, `checkpoints`, `search`, and `show` to work without a `session-store.db`.

Tracked in: ROADMAP.md (new item to be added)

---

## Verification

After Phase 1 fix:

- Machine with `session-state/` folders but no `session-store.db` → dashboard shows "VS Code sessions detected (N sessions). The dashboard reads from the Copilot CLI database format. VS Code session support is coming."
- Machine with `session-store.db` → no change
- Machine with neither → shows existing "no database found" onboarding card
