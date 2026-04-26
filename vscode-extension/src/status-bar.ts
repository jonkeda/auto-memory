import * as vscode from 'vscode';

export function createStatusBar(): vscode.StatusBarItem {
  const item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
  item.text = '$(database) auto-memory';
  item.tooltip = 'Auto Memory — click to open dashboard';
  item.command = 'auto-memory.openDashboard';
  item.show();
  return item;
}
