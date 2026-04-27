# Step 02 — InstructionsManager core

## Goal
Implement the manager class shell with strategy dispatch + multi-root resolution.

## File: `vscode-extension/src/instructions/InstructionsManager.ts`

```typescript
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

  // Strategies A/B/C implemented in subsequent steps as private methods
  private statusA!: () => Promise<Status>;
  private statusB!: (root: string) => Promise<Status>;
  private statusC!: (root: string) => Promise<Status>;
  private addA!: () => Promise<void>;
  private addB!: (root: string) => Promise<void>;
  private addC!: (root: string) => Promise<void>;
  private removeC!: (root: string) => Promise<void>;
}
```

## Done when
- [ ] Class compiles
- [ ] `resolveRoot()` handles 0/1/N workspace folders
- [ ] Subsequent steps (03-05) replace the `!` placeholders with real impls
