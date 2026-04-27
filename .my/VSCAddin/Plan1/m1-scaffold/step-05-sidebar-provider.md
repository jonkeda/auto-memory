# Step 05 — Sidebar provider (placeholder)

## Goal
Implement `SidebarProvider` as a `WebviewViewProvider` that renders a placeholder HTML page. Real dashboard content lands in M4.

## File: `vscode-extension/src/sidebar/SidebarProvider.ts`

```typescript
import * as vscode from 'vscode';

export class SidebarProvider implements vscode.WebviewViewProvider {
  private view?: vscode.WebviewView;

  constructor(private readonly context: vscode.ExtensionContext) {}

  resolveWebviewView(view: vscode.WebviewView): void {
    this.view = view;
    view.webview.options = { enableScripts: true };
    view.webview.html = this.html();
  }

  refresh(): void {
    if (this.view) this.view.webview.html = this.html();
  }

  private html(): string {
    return `<!DOCTYPE html>
<html>
<head>
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline';">
  <style>
    body { font-family: var(--vscode-font-family); padding: 12px; color: var(--vscode-foreground); }
    h2 { font-size: 14px; margin-top: 0; }
    .placeholder { opacity: 0.7; font-size: 12px; }
  </style>
</head>
<body>
  <h2>Auto Memory</h2>
  <p class="placeholder">Dashboard coming in M4.</p>
  <p class="placeholder">Use the Command Palette to access available commands.</p>
</body>
</html>`;
  }
}
```

## Done when
- [ ] Sidebar opens when Activity Bar icon is clicked
- [ ] Placeholder HTML renders with theme-coloured text
- [ ] `refreshSidebar` command re-renders without errors
