# Step 04 — Status bar

## Goal
Add a status bar item showing `$(database) auto-memory` on the right side; clicking it runs `auto-memory.openDashboard`.

## File: `vscode-extension/src/status-bar.ts`

```typescript
import * as vscode from 'vscode';

export function createStatusBar(): vscode.StatusBarItem {
  const item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
  item.text = '$(database) auto-memory';
  item.tooltip = 'Auto Memory — click to open dashboard';
  item.command = 'auto-memory.openDashboard';
  item.show();
  return item;
}
```

## Done when
- [ ] Status bar item visible after extension activates
- [ ] Clicking the item invokes `auto-memory.openDashboard`
