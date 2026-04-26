# M2 — Binary bundling, PATH install, auto-update

**Goal:** The extension extracts the correct platform binary on first activation, places it in the install directory, registers it on the PATH, and silently updates it when a newer version is bundled.

**Depends on:** M1 complete (scaffold, commands registered)

---

## Deliverables

- Per-platform binaries bundled under `bin/` in the extension
- `BinaryManager` service: extract, version-check, update
- PATH registration with all three strategies working
- `auto-memory.install` command fully implemented
- `auto-memory.uninstall` command fully implemented
- Auto-update check on activation (background, non-blocking)
- WSL remote support

---

## Binary layout

```
vscode-extension/
  bin/
    win32-x64/
      session-recall.exe
    linux-x64/
      session-recall
    darwin-arm64/
      session-recall
```

Each platform VSIX includes only its own `bin/<platform>/` subtree (via `vsce package --target`).

---

## `BinaryManager` (`src/binary/BinaryManager.ts`)

```
BinaryManager
  ├── bundledVersion()       → string   reads --version from bundled binary
  ├── installedVersion()     → string?  reads --version from install dir (null if missing)
  ├── install()              → void     copies bundle → installDir, chmod +x on posix
  ├── needsUpdate()          → boolean  semver compare
  ├── resolveInstallDir()    → string   %LOCALAPPDATA%\auto-memory or ~/.local/auto-memory
  └── binaryPath()           → string   full path to installed binary
```

### WSL handling

- Detect: `vscode.env.remoteName === 'wsl'`
- Install dir in WSL: `~/.local/bin/` (inside WSL filesystem)
- Binary: linux-x64 (always, regardless of Windows host arch)
- Execution: use `wsl.exe` or VS Code's WSL terminal API to invoke the binary

---

## PATH strategies (`src/binary/PathRegistrar.ts`)

### `userPath` (default)

- **Windows**: read `HKCU\Environment\PATH`, append install dir if absent, write back, broadcast `WM_SETTINGCHANGE` via PowerShell one-liner
- **Linux/macOS**: detect active shell (`$SHELL`), append `export PATH="..."` to `~/.bashrc` or `~/.zshrc`, guarded by sentinel comment

### `vscodePath`

- Write `terminal.integrated.env.windows` / `linux` / `osx` to **user** `settings.json` via `vscode.workspace.getConfiguration`
- Only affects VS Code integrated terminals

### `manual`

- Show information message with install path and a **Copy** action
- No file system modifications

---

## `auto-memory.install` command flow

```
1. resolveInstallDir()
2. install()           ← extract binary
3. PathRegistrar based on pathStrategy setting
4. Show notification: "session-recall 0.1.0 installed at <path>"
5. Offer "Open Dashboard" action in notification
```

---

## `auto-memory.uninstall` command flow

```
1. Confirm via modal dialog ("Remove binary and PATH entry?")
2. Delete binary from installDir
3. PathRegistrar.remove()   ← undo PATH modification
4. Show notification: "auto-memory uninstalled"
```

---

## Auto-update (on activation)

```typescript
// runs in background after 3s delay to not block startup
async function checkForUpdate(ctx: BinaryManager) {
  const installed = await ctx.installedVersion();
  const bundled   = ctx.bundledVersion();
  if (!installed || semverLt(installed, bundled)) {
    await ctx.install();
    vscode.window.showInformationMessage(
      `session-recall updated to ${bundled}`
    );
  }
}
```

---

## Acceptance criteria

- [ ] On first install: binary appears in install dir, `session-recall --version` works from a new terminal
- [ ] `userPath` strategy: closing and reopening a terminal shows `session-recall` in PATH
- [ ] `vscodePath` strategy: VS Code integrated terminal resolves binary without system PATH change
- [ ] `manual` strategy: notification shown, no files modified beyond the binary itself
- [ ] Older installed binary is replaced silently on extension update
- [ ] WSL remote: linux-x64 binary installed inside WSL, invocable from WSL terminal
- [ ] `auto-memory.uninstall`: binary removed, PATH entry cleaned up, confirmation dialog shown
