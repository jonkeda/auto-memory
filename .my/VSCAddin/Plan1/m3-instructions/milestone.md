# M3 — Copilot instructions injection

**Goal:** All three strategies for injecting the session-recall recall block into Copilot instructions are fully implemented, idempotent, accessible from the Command Palette, and surfaced as buttons inside the dashboard webview.

**Depends on:** M1 complete

---

## Deliverables

- `InstructionsManager` service with all three strategies
- `auto-memory.addInstructions` command: Quick Pick → delegates to chosen strategy
- Idempotency guard (sentinel comment) for all file-writing strategies
- Multi-root workspace support (prompt to pick folder) for strategies B and C
- Instructions status check (installed / not installed) for webview integration
- The recall block text sourced from a single constant (no duplication)

---

## Recall block content

The block written by all strategies is the contents of `copilot-instructions-template.md` in the repo root, wrapped in sentinel comments:

```markdown
<!-- auto-memory:recall-block:start -->
## Progressive Session Recall — RUN FIRST ON EVERY PROMPT
...
<!-- auto-memory:recall-block:end -->
```

Stored as a TypeScript constant in `src/instructions/recallBlock.ts` — single source of truth.

---

## `InstructionsManager` (`src/instructions/InstructionsManager.ts`)

```
InstructionsManager
  ├── status(strategy)     → { installed: boolean, path: string }
  ├── add(strategy, root?) → void   writes/appends recall block
  ├── remove(strategy)     → void   strips block between sentinel comments
  └── resolveRoot()        → string picks workspace root (Quick Pick if multi-root)
```

---

## Strategy A — Global (`~/.copilot/copilot-instructions.md`)

```
1. Resolve path: os.homedir() + '/.copilot/copilot-instructions.md'
2. Create file if missing (with empty content)
3. Read file; check for sentinel start comment
4. If sentinel absent: append recall block
5. If sentinel present: no-op, notify "already installed"
```

No workspace root needed — always user-scoped.

---

## Strategy B — Scoped instructions file

```
Target: <root>/.github/instructions/session-recall.instructions.md

1. If multi-root: show Quick Pick of workspace folder names → selected = root
2. Ensure .github/instructions/ directory exists
3. If file absent: create with front-matter + recall block
4. If file present: check sentinel; append if missing
```

File written:

```markdown
---
applyTo: "**"
---
<!-- auto-memory:recall-block:start -->
## Progressive Session Recall ...
<!-- auto-memory:recall-block:end -->
```

---

## Strategy C — `settings.json` entry

```
Target: workspace settings.json  (or user settings if no workspace open)

1. Read github.copilot.chat.codeGeneration.instructions array (default [])
2. Check if an entry with file matching session-recall already exists
3. If absent: append { "file": ".github/instructions/session-recall.instructions.md" }
4. Write back via vscode.workspace.getConfiguration().update()
5. Also create the .instructions.md file (Strategy B logic) so the pointer resolves
```

---

## `auto-memory.addInstructions` command flow

```
1. Show Quick Pick:
     ● Add globally  (~/.copilot/copilot-instructions.md)
     ● Add scoped file  (.github/instructions/session-recall.instructions.md)
     ● Add to settings.json  (github.copilot.chat.codeGeneration.instructions)
2. Call InstructionsManager.add(selectedStrategy)
3. Show result notification:
     ✅ "Recall block added to <target path>"
     ℹ️  "Already installed — no changes made"
```

---

## Webview integration (consumed by M4)

`InstructionsManager.status()` returns a plain object that the webview panel requests via `postMessage`. The webview renders three rows:

```
[A] Global      ✅ Installed    [Remove]
[B] Scoped      ⚠️ Not installed [Add]
[C] settings    ⚠️ Not installed [Add]
```

Each button posts a message back to the extension host, which calls `InstructionsManager.add()` / `remove()`, then re-posts the updated status.

---

## Acceptance criteria

- [ ] **Strategy A**: block appended to `~/.copilot/copilot-instructions.md`; running command twice does not duplicate the block
- [ ] **Strategy B**: file created at correct path with front-matter; Quick Pick shown when workspace has multiple roots
- [ ] **Strategy C**: `settings.json` entry added; `.instructions.md` file created alongside
- [ ] Removing via `InstructionsManager.remove()` strips the block cleanly, leaving surrounding content intact
- [ ] `InstructionsManager.status()` correctly detects installed/not-installed for all three strategies independently
- [ ] Works on Windows (CRLF) and Linux/macOS (LF) — sentinel check is newline-agnostic
