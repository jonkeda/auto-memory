# Step 06 — Launch configuration

## Goal
`.vscode/launch.json` and `.vscode/tasks.json` so F5 in `vscode-extension/` launches the Extension Development Host with the extension loaded.

## Files

`vscode-extension/.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Run Extension",
      "type": "extensionHost",
      "request": "launch",
      "args": ["--extensionDevelopmentPath=${workspaceFolder}"],
      "outFiles": ["${workspaceFolder}/out/**/*.js"],
      "preLaunchTask": "npm: build"
    }
  ]
}
```

`vscode-extension/.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "npm: build",
      "type": "npm",
      "script": "build",
      "group": { "kind": "build", "isDefault": true },
      "problemMatcher": ["$tsc"]
    }
  ]
}
```

## Done when
- [ ] Opening `vscode-extension/` as a folder and pressing F5 launches the host
- [ ] Build runs automatically before launch
