# Step 03 — Extension entry point

## Goal
Create `src/extension.ts` with `activate(context)` that registers all `auto-memory.*` commands as stubs, plus the SidebarProvider and StatusBar.

## File: `vscode-extension/src/extension.ts`

```typescript
import * as vscode from 'vscode';
import { SidebarProvider } from './sidebar/SidebarProvider';
import { createStatusBar } from './status-bar';

export function activate(context: vscode.ExtensionContext): void {
  // Sidebar webview view
  const sidebar = new SidebarProvider(context);
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider('autoMemory.sidebar', sidebar)
  );

  // Status bar
  context.subscriptions.push(createStatusBar());

  // Command stubs — real implementations land in M2/M3/M4
  const commands: Array<[string, () => void | Promise<void>]> = [
    ['auto-memory.openDashboard',    () => vscode.window.showInformationMessage('Dashboard — coming in M4')],
    ['auto-memory.installBinary',    () => vscode.window.showInformationMessage('Install — coming in M2')],
    ['auto-memory.updateBinary',     () => vscode.window.showInformationMessage('Update — coming in M2')],
    ['auto-memory.uninstall',        () => vscode.window.showInformationMessage('Uninstall — coming in M2')],
    ['auto-memory.addInstructions',  () => vscode.window.showInformationMessage('Instructions — coming in M3')],
    ['auto-memory.runHealth',        () => vscode.window.showInformationMessage('Health — coming in M4')],
    ['auto-memory.refreshSidebar',   () => sidebar.refresh()],
  ];

  for (const [id, fn] of commands) {
    context.subscriptions.push(vscode.commands.registerCommand(id, fn));
  }
}

export function deactivate(): void { /* nothing */ }
```

## Done when
- [ ] `npm run build` succeeds
- [ ] F5 launches Extension Host; all 7 commands appear in Command Palette
- [ ] No errors in Extension Host Developer Tools console
