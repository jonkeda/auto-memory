# Roadmap — VS Code Extension: auto-memory

A VS Code extension that bundles and deploys the `session-recall` .NET binary, puts it on the user's PATH, and wires Copilot instructions into projects — all from the Command Palette with zero terminal work.

---

## Goals

1. Bundle the self-contained `session-recall` binary inside the VSIX (per-platform).
2. Install the binary to a stable location and register it on the user's PATH.
3. Inject the Copilot instructions recall block into a project in one of several ways.
4. Provide a status bar item and a sidebar panel for quick access.

---

## Phase 1 — Project scaffold

**Deliverables**
- New folder `vscode-extension/` at the repo root
- Standard VS Code extension scaffold: `package.json`, `src/extension.ts`, `tsconfig.json`
- Extension ID: `auto-memory.auto-memory`
- Activation events: `onStartupFinished` (lazy — no impact on cold-start)
- Engine: `^1.90.0` (VS Code, aligns with Copilot chat availability)

**Steps**
1. `npm init` extension with `yo code` or manual scaffold.
2. Register Command Palette commands:
   - `auto-memory.install` — Install / update binary
   - `auto-memory.addInstructions` — Add recall block to project
   - `auto-memory.openDashboard` — Open dashboard webview panel
3. Add settings contribution (`contributes.configuration`) for install directory, PATH strategy, and instructions strategy.
4. Add status bar item (right side): `$(database) recall` — click opens dashboard.
5. Register an **Activity Bar sidebar entry** (`contributes.viewsContainers` + `contributes.views`):
   - Icon: database/memory icon in the Activity Bar
   - View ID: `auto-memory.sidebarView`
   - Clicking the icon opens the sidebar; the sidebar hosts the same webview as `openDashboard` (reuses the same `AutoMemoryPanel` class, pinned rather than floating)
   - The sidebar panel is always available without running a command

---

## Phase 2 — Binary bundling and PATH install

### 2a — Bundle the binary

Ship platform-specific binaries inside the VSIX using VS Code's `platform` field in `package.json`:

```json
"contributes": {},
"__metadata": {
  "targetPlatform": "win32-x64"
}
```

Build separate `.vsix` packages per platform:

| Platform | Binary | `targetPlatform` |
|---|---|---|
| Windows x64 | `session-recall-win-x64.exe` | `win32-x64` |
| Linux x64 | `session-recall-linux-x64` | `linux-x64` |
| macOS ARM64 | `session-recall-osx-arm64` | `darwin-arm64` |

Place each binary under `bin/` in the extension folder; set executable bit in a post-install script on Linux/macOS.

On activation, compare the bundled version string (read from the binary's `--version` output) against any previously installed binary. Extract if missing or outdated.

### 2b — Default install location

```
Windows:  %LOCALAPPDATA%\auto-memory\session-recall.exe
Linux:    ~/.local/bin/session-recall
macOS:    ~/.local/bin/session-recall
```

### 2c — PATH registration

Three strategies, user-selectable via `auto-memory.pathStrategy` setting:

| Strategy | Mechanism | Scope |
|---|---|---|
| **User PATH** (default) | Write to `~/.bashrc` / `~/.zshrc` / Windows user PATH registry key | Permanent, all terminals |
| **VS Code terminal PATH** | Inject via `terminal.integrated.env.*` in workspace or user `settings.json` | VS Code terminals only |
| **Manual** | Show install path in a notification with a **Copy path** button | No auto-modification |

After a permanent PATH update, show an information notification:
> *"session-recall added to PATH. Open a new terminal to pick up the change."*

A **Remove from PATH** command is registered for clean uninstall.

### 2d — Auto-update

On extension activation (background, non-blocking):
1. Run installed binary with `--version`.
2. Compare to bundled version.
3. If bundled is newer, extract silently and show a notification: *"session-recall updated to 0.2.0"*.

---

## Phase 3 — Copilot instructions injection

Three strategies, presented as a Quick Pick when running `auto-memory.addInstructions` **and** via buttons inside the dashboard webview. Default is user-configurable via `auto-memory.instructionsStrategy`.

### Strategy A — Global user instructions file

Append the recall block to `~/.copilot/copilot-instructions.md`.

- Applies to all projects on the machine.
- Idempotent: sentinel comment `<!-- auto-memory:recall-block -->` guards against duplicate insertion.
- Command: `auto-memory.addInstructions` → pick **Global (~/.copilot/)**

```
~/.copilot/copilot-instructions.md   ← appended
```

### Strategy B — Scoped `.instructions.md` file

Create `.github/instructions/session-recall.instructions.md` with optional `applyTo` front-matter.

- Modular: sits alongside other instruction files without touching the main file.
- Can be scoped to specific file globs (e.g. `applyTo: "**/*.cs"`).
- Best for teams that already manage a structured instructions directory.
- If the workspace has multiple root folders the extension prompts the user to pick which one.
- Command: `auto-memory.addInstructions` → pick **Scoped instructions file**

```
<workspace>/.github/instructions/session-recall.instructions.md   ← created
```

Front-matter written by the extension:

```markdown
---
applyTo: "**"
---
## Progressive Session Recall ...
```

### Strategy C — VS Code `settings.json` via `github.copilot.chat.codeGeneration.instructions`

Append an instructions entry to the workspace or user `settings.json`:

```json
"github.copilot.chat.codeGeneration.instructions": [
  { "file": ".github/instructions/session-recall.instructions.md" }
]
```

- Zero extra files in the repo root.
- Works with the Copilot settings-driven instructions model.
- Extension writes the settings entry; the instructions file is served from the extension's install directory (no repo file needed).
- Command: `auto-memory.addInstructions` → pick **settings.json entry**

---

## Phase 4 — Dashboard webview panel

The same `AutoMemoryPanel` webview is used in two surfaces:
- **Sidebar** (Activity Bar click) — pinned, always accessible, renders in the side panel
- **Editor tab** (`auto-memory.openDashboard`) — floating, full-width, useful for the search and session views

Both surfaces share the same HTML/JS bundle; the host context (sidebar vs. tab) is passed as a flag.

### Sections

| Section | Content |
|---|---|
| **Install status** | Binary version, install path, PATH strategy in use |
| **Health** | Runs `session-recall health --json`; renders 9 dimensions with colour-coded zone badges |
| **Recent sessions** | Runs `session-recall list --json --limit 10`; clickable list — clicking a session runs `show <id>` and displays result |
| **Search** | Text input → `session-recall search "<term>" --json` → result list with excerpts |
| **Copilot instructions** | Shows current instructions status (installed / not installed, which strategy); three buttons — **Add globally**, **Add scoped file**, **Add to settings.json** — each triggering the corresponding Phase 3 strategy without leaving the webview |

Data is fetched via `child_process.execFile` (never a shell, to avoid injection). Results cached 60 seconds. A **Refresh** button clears the cache.

After clicking an instructions button, the panel re-reads the target file and updates the status badge (✅ Installed / ⚠️ Not installed) in real time.

---

## Phase 5 — Packaging and distribution

| Item | Detail |
|---|---|
| Per-platform VSIX | Built with `vsce package --target <platform>` for all 3 RIDs |
| VS Code Marketplace | Published under the same publisher as the GitHub repo owner |
| Open VSX | Also published to Open VSX Registry for non-Microsoft VS Code builds (Cursor, Windsurf, etc.) |
| CI workflow | `.github/workflows/vscode-ext.yml` — builds and publishes all 3 VSIX files on tag `v*` |
| Version sync | Extension `version` in `package.json` read from `net/Version.props` via a prebuild script — single source of truth |
| Telemetry | Opt-in; reports `install`, `addInstructions`, `dashboard.open` events using the same `session-recall-telemetry.json` ring buffer |

---

## Decisions

| Topic | Decision |
|---|---|
| **Web extension** | Not supported. vscode.dev cannot run native binaries. Show a banner linking to the GitHub releases page for manual download. |
| **WSL** | Implement. When VS Code's remote is WSL, detect via `vscode.env.remoteName === 'wsl'`, extract the linux-x64 binary into the WSL home directory (`~/.local/bin/`), and run it via the WSL shell. |
| **Multi-root workspaces** | Implement. Strategies B and C prompt the user with a Quick Pick listing all workspace folders; selected folder is used as the root. |
| **Copilot instructions format stability** | Acknowledge. Monitor Copilot for VS Code release notes; pin the tested schema version in the extension changelog. |
| **Uninstall hook** | Implement. Provide `auto-memory.uninstall` command that removes the binary from the install directory and undoes PATH changes. Document in the extension README as the clean-removal path. |

---

## Milestone summary

| Milestone | Key deliverable |
|---|---|
| M1 | Scaffold, Command Palette commands, status bar, settings schema |
| M2 | Binary bundled per-platform, PATH install, auto-update |
| M3 | All 3 instructions strategies implemented and tested; instructions panel in webview |
| M4 | Dashboard webview with health + search |
| M5 | Per-platform VSIX on Marketplace and Open VSX, CI pipeline |
