# Step 02 — AutoMemoryPanel class

## Goal
Single class that powers both the sidebar webview view AND the editor tab webview panel.

## File: `vscode-extension/src/panel/AutoMemoryPanel.ts`

```typescript
import * as vscode from 'vscode';
import { BinaryManager } from '../binary/BinaryManager';
import { InstructionsManager } from '../instructions/InstructionsManager';
import { TtlCache } from './cache';
import { run } from './runner';
import { renderHtml } from './html';
import { handleMessage } from './messages';

export type Surface = 'sidebar' | 'tab';

export class AutoMemoryPanel {
  static current: AutoMemoryPanel | null = null;
  private cache = new TtlCache(60_000);

  constructor(
    private readonly webview: vscode.Webview,
    private readonly extensionUri: vscode.Uri,
    private readonly surface: Surface,
    private readonly binMgr: BinaryManager,
    private readonly insMgr: InstructionsManager,
  ) {
    webview.options = { enableScripts: true, localResourceRoots: [extensionUri] };
    webview.html = renderHtml(webview, extensionUri, surface);
    webview.onDidReceiveMessage(msg => handleMessage(this, msg));
    void this.refreshAll();
  }

  static openTab(context: vscode.ExtensionContext, binMgr: BinaryManager, insMgr: InstructionsManager): void {
    const panel = vscode.window.createWebviewPanel(
      'autoMemory.dashboard', 'Auto Memory', vscode.ViewColumn.Active,
      { enableScripts: true, retainContextWhenHidden: true });
    new AutoMemoryPanel(panel.webview, context.extensionUri, 'tab', binMgr, insMgr);
  }

  post(type: string, payload: unknown): void {
    void this.webview.postMessage({ type, payload });
  }

  cli(): TtlCache { return this.cache; }
  binary(): BinaryManager { return this.binMgr; }
  instructions(): InstructionsManager { return this.insMgr; }
  runCli(args: string[]): Promise<string> { return this.runWithBin(args); }

  private async runWithBin(args: string[]): Promise<string> {
    const bin = await this.binMgr.resolve();
    if (!bin) throw new Error('session-recall binary not found');
    return run(bin, args);
  }

  async refreshAll(): Promise<void> {
    this.cache.invalidate();
    // Trigger webview to request all sections
    this.post('refreshRequested', null);
  }
}
```

Update `SidebarProvider.resolveWebviewView` to construct `AutoMemoryPanel(view.webview, ..., 'sidebar', ...)`.

Update `auto-memory.openDashboard` command to call `AutoMemoryPanel.openTab(...)`.

## Done when
- [ ] Sidebar opens and renders dashboard HTML
- [ ] `auto-memory.openDashboard` opens an editor-tab dashboard
- [ ] Both surfaces respond to messages
