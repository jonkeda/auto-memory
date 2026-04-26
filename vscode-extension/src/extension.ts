import * as vscode from 'vscode';
import { SidebarProvider } from './sidebar/SidebarProvider';
import { createStatusBar } from './status-bar';
import { BinaryManager } from './binary/BinaryManager';
import { installBinary, updateBinary, uninstall, maybeAutoUpdate } from './binary/install';
import { InstructionsManager } from './instructions/InstructionsManager';
import { addInstructionsCommand } from './instructions/command';
import { AutoMemoryPanel } from './panel/AutoMemoryPanel';

export function activate(context: vscode.ExtensionContext): void {
  // Binary manager instance
  const mgr = new BinaryManager(context);

  // Instructions manager instance
  const instructionsMgr = new InstructionsManager();

  // Sidebar webview view
  const sidebar = new SidebarProvider(context, mgr, instructionsMgr);
  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider('autoMemory.sidebar', sidebar)
  );

  // Status bar
  context.subscriptions.push(createStatusBar());

  // Command stubs — real implementations land in M2/M3/M4
  const commands: Array<[string, () => void | Promise<void>]> = [
    ['auto-memory.openDashboard',    () => AutoMemoryPanel.openTab(context, mgr, instructionsMgr)],
    ['auto-memory.installBinary',    () => installBinary(mgr)],
    ['auto-memory.updateBinary',     () => updateBinary(mgr)],
    ['auto-memory.uninstall',        () => uninstall(mgr)],
    ['auto-memory.addInstructions',  () => addInstructionsCommand(instructionsMgr)],
    ['auto-memory.runHealth',        () => vscode.window.showInformationMessage('Health — coming in M4')],
    ['auto-memory.refreshSidebar',   () => sidebar.refresh()],
  ];

  for (const [id, fn] of commands) {
    context.subscriptions.push(vscode.commands.registerCommand(id, fn));
  }

  // Auto-update check (fire-and-forget)
  if (vscode.workspace.getConfiguration('autoMemory').get<boolean>('autoUpdate', true)) {
    void maybeAutoUpdate(mgr);
  }
}

export function deactivate(): void { /* nothing */ }
