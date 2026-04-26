import * as vscode from 'vscode';
import * as fs from 'fs/promises';
import * as path from 'path';
import * as os from 'os';
import { RECALL_BLOCK, hasBlock, stripBlock } from './recallBlock';

export type Strategy = 'A' | 'B' | 'C';
export interface Status { installed: boolean; path: string; }

export class InstructionsManager {
  async status(strategy: Strategy, root?: string): Promise<Status> {
    switch (strategy) {
      case 'A': return this.statusA();
      case 'B': return this.statusB(root ?? await this.resolveRoot());
      case 'C': return this.statusC(root ?? await this.resolveRoot());
    }
  }

  async add(strategy: Strategy, root?: string): Promise<void> {
    const r = root ?? (strategy === 'A' ? '' : await this.resolveRoot());
    switch (strategy) {
      case 'A': return this.addA();
      case 'B': return this.addB(r);
      case 'C': return this.addC(r);
    }
  }

  async remove(strategy: Strategy, root?: string): Promise<void> {
    const r = root ?? (strategy === 'A' ? '' : await this.resolveRoot());
    const s = await this.status(strategy, r);
    if (!s.installed) return;
    if (strategy === 'C') return this.removeC(r);
    const content = await fs.readFile(s.path, 'utf8');
    await fs.writeFile(s.path, stripBlock(content));
  }

  async resolveRoot(): Promise<string> {
    const folders = vscode.workspace.workspaceFolders ?? [];
    if (folders.length === 0) throw new Error('No workspace folder open');
    if (folders.length === 1) return folders[0].uri.fsPath;
    const pick = await vscode.window.showQuickPick(
      folders.map(f => f.name), { placeHolder: 'Select workspace folder' });
    if (!pick) throw new Error('Cancelled');
    return folders.find(f => f.name === pick)!.uri.fsPath;
  }

  // Strategy A — Global
  private globalPath(): string {
    return path.join(os.homedir(), '.copilot', 'copilot-instructions.md');
  }

  private async statusA(): Promise<Status> {
    const p = this.globalPath();
    try {
      const content = await fs.readFile(p, 'utf8');
      return { installed: hasBlock(content), path: p };
    } catch {
      return { installed: false, path: p };
    }
  }

  private async addA(): Promise<void> {
    const p = this.globalPath();
    await fs.mkdir(path.dirname(p), { recursive: true });
    let content = '';
    try { content = await fs.readFile(p, 'utf8'); } catch { /* missing */ }
    if (hasBlock(content)) return;
    const newline = content.length > 0 && !content.endsWith('\n') ? '\n' : '';
    await fs.writeFile(p, content + newline + RECALL_BLOCK);
  }

  // Strategy B — Scoped file
  private scopedPath(root: string): string {
    return path.join(root, '.github', 'instructions', 'session-recall.instructions.md');
  }

  private async statusB(root: string): Promise<Status> {
    const p = this.scopedPath(root);
    try {
      const content = await fs.readFile(p, 'utf8');
      return { installed: hasBlock(content), path: p };
    } catch { return { installed: false, path: p }; }
  }

  private async addB(root: string): Promise<void> {
    const p = this.scopedPath(root);
    await fs.mkdir(path.dirname(p), { recursive: true });
    let content = '';
    try { content = await fs.readFile(p, 'utf8'); } catch { /* new file */ }
    if (hasBlock(content)) return;
    if (content.length === 0) {
      content = '---\napplyTo: "**"\n---\n';
    } else if (!content.endsWith('\n')) {
      content += '\n';
    }
    await fs.writeFile(p, content + RECALL_BLOCK);
  }

  // Strategy C — settings.json
  private settingsKey = 'github.copilot.chat.codeGeneration.instructions';
  private settingsPointer = { file: '.github/instructions/session-recall.instructions.md' };

  private async statusC(root: string): Promise<Status> {
    const cfg = vscode.workspace.getConfiguration(undefined, vscode.Uri.file(root));
    const arr = cfg.get<Array<{ file?: string }>>(this.settingsKey) ?? [];
    const installed = arr.some(e => e.file === this.settingsPointer.file);
    return { installed, path: `${root} (settings.json)` };
  }

  private async addC(root: string): Promise<void> {
    // Ensure the file referenced by the pointer exists
    await this.addB(root);
    const cfg = vscode.workspace.getConfiguration(undefined, vscode.Uri.file(root));
    const arr = cfg.get<Array<{ file?: string }>>(this.settingsKey) ?? [];
    if (arr.some(e => e.file === this.settingsPointer.file)) return;
    arr.push(this.settingsPointer);
    await cfg.update(this.settingsKey, arr, vscode.ConfigurationTarget.Workspace);
  }

  private async removeC(root: string): Promise<void> {
    const cfg = vscode.workspace.getConfiguration(undefined, vscode.Uri.file(root));
    const arr = (cfg.get<Array<{ file?: string }>>(this.settingsKey) ?? [])
      .filter(e => e.file !== this.settingsPointer.file);
    await cfg.update(this.settingsKey, arr, vscode.ConfigurationTarget.Workspace);
  }
}
