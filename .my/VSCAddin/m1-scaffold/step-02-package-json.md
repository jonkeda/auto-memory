# Step 02 — package.json contributions

## Goal
Add `contributes` block to `package.json`: commands, Activity Bar view container, sidebar view, configuration schema.

## Edits to `vscode-extension/package.json`

Add top-level keys:

```json
{
  "activationEvents": ["onStartupFinished"],
  "contributes": {
    "commands": [
      { "command": "auto-memory.openDashboard", "title": "Auto Memory: Open Dashboard" },
      { "command": "auto-memory.installBinary",  "title": "Auto Memory: Install CLI" },
      { "command": "auto-memory.updateBinary",   "title": "Auto Memory: Update CLI" },
      { "command": "auto-memory.uninstall",      "title": "Auto Memory: Uninstall" },
      { "command": "auto-memory.addInstructions","title": "Auto Memory: Add Copilot Instructions" },
      { "command": "auto-memory.runHealth",      "title": "Auto Memory: Run Health Check" },
      { "command": "auto-memory.refreshSidebar", "title": "Auto Memory: Refresh Sidebar" }
    ],
    "viewsContainers": {
      "activitybar": [
        { "id": "autoMemory", "title": "Auto Memory", "icon": "$(database)" }
      ]
    },
    "views": {
      "autoMemory": [
        { "type": "webview", "id": "autoMemory.sidebar", "name": "Dashboard" }
      ]
    },
    "configuration": {
      "title": "Auto Memory",
      "properties": {
        "autoMemory.binaryPath":    { "type": "string", "default": "", "description": "Override path to session-recall binary" },
        "autoMemory.pathStrategy":  { "type": "string", "enum": ["userPath","vscodePath","manual"], "default": "userPath" },
        "autoMemory.autoUpdate":    { "type": "boolean", "default": true },
        "autoMemory.dbPath":        { "type": "string", "default": "" },
        "autoMemory.telemetryEnabled": { "type": "boolean", "default": false }
      }
    }
  }
}
```

## Done when
- [ ] `npm run build` still succeeds
- [ ] `package.json` validates against the VS Code extension manifest schema
