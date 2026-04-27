# M1 — Scaffold, Command Palette, Status Bar, Settings

**Goal:** Produce a loadable VS Code extension with all entry points registered and a working status bar item. No binary logic yet — commands show placeholder notifications.

---

## Deliverables

- `vscode-extension/` folder at the repo root with full extension scaffold
- All commands registered and invokable from the Command Palette
- Activity Bar sidebar entry visible
- Status bar item visible and clickable
- Settings schema declared (no logic behind them yet)
- Extension loads without errors on VS Code `^1.90.0`

---

## File structure

```
vscode-extension/
  package.json          ← manifest: commands, views, configuration, activationEvents
  tsconfig.json
  .vscodeignore
  src/
    extension.ts        ← activate() / deactivate()
    commands/
      install.ts        ← placeholder
      addInstructions.ts← placeholder
      openDashboard.ts  ← placeholder
      uninstall.ts      ← placeholder
    sidebar/
      SidebarProvider.ts← WebviewViewProvider stub
    ui/
      statusBar.ts      ← status bar item registration
  bin/                  ← empty; populated in M2
  resources/
    icon.png            ← Activity Bar icon (16×16 + 32×32)
```

---

## `package.json` contributions

### Commands

| Command ID | Title |
|---|---|
| `auto-memory.install` | auto-memory: Install / Update binary |
| `auto-memory.addInstructions` | auto-memory: Add Copilot recall instructions |
| `auto-memory.openDashboard` | auto-memory: Open Dashboard |
| `auto-memory.uninstall` | auto-memory: Uninstall binary and remove from PATH |

### Views

```json
"contributes": {
  "viewsContainers": {
    "activitybar": [{
      "id": "auto-memory",
      "title": "auto-memory",
      "icon": "resources/icon.png"
    }]
  },
  "views": {
    "auto-memory": [{
      "type": "webview",
      "id": "auto-memory.sidebarView",
      "name": "Session Recall"
    }]
  }
}
```

### Configuration settings

| Setting | Type | Default | Description |
|---|---|---|---|
| `auto-memory.installDir` | string | `""` (auto) | Override binary install directory |
| `auto-memory.pathStrategy` | enum | `"userPath"` | `userPath` / `vscodePath` / `manual` |
| `auto-memory.instructionsStrategy` | enum | `"global"` | `global` / `scoped` / `settings` |

---

## Acceptance criteria

- [ ] `Developer: Show Running Extensions` lists `auto-memory.auto-memory` without errors
- [ ] All four commands appear in the Command Palette under `auto-memory:`
- [ ] Activity Bar shows the extension icon; clicking it opens the sidebar panel
- [ ] Status bar right side shows `$(database) recall`; clicking it fires `openDashboard`
- [ ] All three settings appear in `File → Preferences → Settings` under `auto-memory`
- [ ] `vsce package` produces a `.vsix` without warnings
