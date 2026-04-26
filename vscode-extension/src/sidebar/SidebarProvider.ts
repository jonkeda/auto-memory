import * as vscode from 'vscode';
import { AutoMemoryPanel } from '../panel/AutoMemoryPanel';
import { BinaryManager } from '../binary/BinaryManager';
import { InstructionsManager } from '../instructions/InstructionsManager';

export class SidebarProvider implements vscode.WebviewViewProvider {
  private view?: vscode.WebviewView;
  private panel?: AutoMemoryPanel;

  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly binMgr: BinaryManager,
    private readonly insMgr: InstructionsManager,
  ) {}

  resolveWebviewView(view: vscode.WebviewView): void {
    this.view = view;
    this.panel = new AutoMemoryPanel(
      view.webview,
      this.context.extensionUri,
      'sidebar',
      this.binMgr,
      this.insMgr,
    );
  }

  refresh(): void {
    if (this.panel) void this.panel.refreshAll();
  }
}
