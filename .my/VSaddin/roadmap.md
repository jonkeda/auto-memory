# Roadmap — Visual Studio Add-in: auto-memory

A Visual Studio extension (VSIX) that bundles and deploys the `session-recall` .NET binary, puts it on the user's PATH, and wires Copilot instructions into projects — all from inside the IDE with zero terminal work.

---

## Goals

1. Bundle the self-contained `session-recall` binary inside the VSIX (win-x64).
2. Install the binary to a stable location and register it on the user's PATH.
3. Inject the Copilot instructions recall block into a project in one of several ways.
4. Provide a Tools menu entry and an Options page for configuration.

---

## Phase 1 — Project scaffold

**Deliverables**
- New solution `net/AutoMemory.VsAddin/AutoMemory.VsAddin.sln`
- Project type: `AsyncPackage` (Community.VisualStudio.SDK)
- Targets: Visual Studio 2022+ (17.x), `net472` (VS extensibility requirement)
- References `AutoMemory.Core` for version constants and schema-check logic

**Steps**
1. Create VSIX project with `AsyncPackage` base class.
2. Add `source.extension.vsixmanifest` with correct GUIDs, metadata, and VS 2022 prerequisite.
3. Wire up `IVsPackage.Initialize` / `InitializeAsync`.
4. Add entry to `Tools` menu: **auto-memory → Open Dashboard**.
5. Add `ToolsOptionsPage` for: install directory, auto-update, instructions strategy (see Phase 3).

---

## Phase 2 — Binary bundling and PATH install

### 2a — Bundle the binary

- Embed `session-recall-win-x64.exe` as a VSIX asset (build action: `Content`, `IncludeInVSIX=true`).
- On first load, extract to install directory if not already present or if version is newer.
- Version check: compare `AssemblyInformationalVersion` of the bundled EXE vs. any existing install.

**Default install location**

```
%LOCALAPPDATA%\auto-memory\session-recall.exe
```

### 2b — PATH registration

Three strategies (user-configurable in Options page):

| Strategy | Mechanism | Scope |
|---|---|---|
| **User PATH** (default) | Append to `HKCU\Environment\PATH` via registry, broadcast `WM_SETTINGCHANGE` | Current user, permanent |
| **Session PATH** | Inject into VS's own process environment | IDE session only, non-invasive |
| **Manual** | Show the install path in a dialog; user adds it themselves | No auto-modification |

**Implementation notes**
- Read current `PATH`, check if install dir is already present before writing.
- After PATH update, show an info bar: *"session-recall added to PATH. Restart any open terminals to pick up the change."*
- Provide a **Remove from PATH** button in the Options page.

### 2c — Auto-update

- On IDE startup (background thread), compare bundled version vs. installed version.
- If newer: extract silently, show notification bar with **What's new** link.
- If GitHub releases are accessible: optionally check latest release tag and prompt to download.

---

## Phase 3 — Copilot instructions injection

Four strategies for getting the recall block into a project. The user picks one per project (or globally) via the Options page or a right-click menu.

### Strategy A — Global user instructions file (simplest)

Append the recall block to `%USERPROFILE%\.copilot\copilot-instructions.md`.

- Applies to all projects and all Copilot surfaces.
- Idempotent: check for the sentinel comment `<!-- auto-memory recall block -->` before appending.
- Menu entry: **auto-memory → Install to global Copilot instructions**.

```
~/.copilot/copilot-instructions.md   ← appended
```

### Strategy B — VS Code workspace instructions file

Write (or append to) `.github/copilot-instructions.md` in the solution root.

- Picked up automatically by VS Code Copilot and GitHub Copilot in Editors.
- Suitable for teams: checked into source control so every developer gets the recall block.
- Idempotent sentinel check as above.
- Menu entry: **auto-memory → Add to project Copilot instructions**.

```
<solution-root>/.github/copilot-instructions.md   ← created or appended
```

### Strategy C — Per-solution `.instructions.md` file

Create `.copilot/instructions.md` (or `.github/instructions/session-recall.instructions.md`) in the solution root.

- Fine-grained: can be scoped with `applyTo` front-matter to specific file globs.
- Does not pollute the global file.
- Best for teams who already have instructions files and want modular additions.

```
<solution-root>/.github/instructions/session-recall.instructions.md   ← created
```

### Strategy D — MSBuild property injection (build-time)

Inject a `<CopilotInstructions>` property or `<AdditionalFiles>` item into the project file, pointing at a shared instructions file installed by the extension.

- Zero file clutter in the repo: instructions are served from the extension's install directory.
- Requires a Copilot/IDE feature that reads `AdditionalFiles` for instructions (currently experimental).
- Mark this strategy as **Experimental** in the UI.

---

## Phase 4 — Dashboard tool window

A VS tool window (**View → Other Windows → auto-memory Dashboard**) showing:

| Panel | Content |
|---|---|
| **Health** | Runs `session-recall health --json` and renders the 9 dimensions with colour-coded zone icons |
| **Recent sessions** | Runs `session-recall list --json --limit 10` and shows a clickable list |
| **Search** | Text box → `session-recall search "<term>" --json` → results list |
| **Install status** | Binary version, install path, PATH status, last telemetry entry |

Refresh button triggers a new run. Results are cached for 60 seconds to avoid hammering the DB.

---

## Phase 5 — Packaging and distribution

| Item | Detail |
|---|---|
| VSIX signed | Code-sign with publisher cert before upload |
| VS Marketplace | Publish to Visual Studio Marketplace under same publisher as the repo owner |
| CI workflow | `.github/workflows/vsix.yml` — builds VSIX on every tag `v*`, attaches to GitHub release alongside the CLI binaries |
| Version sync | `vsixmanifest` version read from `net/Version.props` — single source of truth |
| Telemetry | Extension install/uninstall events reported via existing `Telemetry.cs` ring buffer (opt-in, same privacy model as CLI) |

---

## Open questions

- **VS for Mac / Rider**: separate effort; out of scope for v1.
- **ARM64 Windows**: bundle both `win-x64` and `win-arm64` binaries; detect architecture at install time.
- **Multi-root workspaces**: Strategy B/C need to handle solutions with multiple `.sln` files in the same folder.
- **Copilot instructions format stability**: strategies B/C depend on the `.github/copilot-instructions.md` contract — track changes in the Copilot for VS release notes.
- **Uninstall**: VSIX uninstall should offer to remove the binary and PATH entry (prompt, never silent delete).

---

## Milestone summary

| Milestone | Key deliverable | Target |
|---|---|---|
| M1 | Scaffold, Tools menu, Options page | Phase 1 done |
| M2 | Binary bundled, PATH install working, auto-update | Phase 2 done |
| M3 | All 4 instructions strategies implemented and tested | Phase 3 done |
| M4 | Dashboard tool window with health + search | Phase 4 done |
| M5 | Signed VSIX on Marketplace, CI pipeline | Phase 5 done |
