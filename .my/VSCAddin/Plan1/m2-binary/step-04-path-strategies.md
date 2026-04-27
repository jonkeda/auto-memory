# Step 04 — PATH strategies

## Goal
Implement the three PATH strategies behind `applyPathStrategy(strategy, dir)`.

## File: `vscode-extension/src/binary/path-strategy.ts`

```typescript
import * as vscode from 'vscode';
import { execFile } from 'child_process';

export type PathStrategy = 'userPath' | 'vscodePath' | 'manual';

export async function applyPathStrategy(strategy: PathStrategy, dir: string): Promise<void> {
  switch (strategy) {
    case 'userPath':   return userPath(dir);
    case 'vscodePath': return vscodePath(dir);
    case 'manual':     return manualInstructions(dir);
  }
}

async function userPath(dir: string): Promise<void> {
  if (process.platform === 'win32') {
    // setx PATH "%PATH%;<dir>" — user-scope, persists
    await new Promise<void>((res, rej) =>
      execFile('setx', ['PATH', `${process.env.PATH};${dir}`], err => err ? rej(err) : res()));
  } else {
    // append to ~/.profile if not already present
    const fs = await import('fs/promises');
    const os = await import('os');
    const path = await import('path');
    const profile = path.join(os.homedir(), '.profile');
    const line = `\n# auto-memory\nexport PATH="$PATH:${dir}"\n`;
    let existing = '';
    try { existing = await fs.readFile(profile, 'utf8'); } catch { /* missing */ }
    if (!existing.includes('# auto-memory')) {
      await fs.appendFile(profile, line);
    }
  }
  vscode.window.showInformationMessage(`Added ${dir} to user PATH (restart terminal to take effect).`);
}

async function vscodePath(dir: string): Promise<void> {
  const cfg = vscode.workspace.getConfiguration('terminal.integrated.env');
  const platKey = process.platform === 'win32' ? 'windows' : process.platform === 'darwin' ? 'osx' : 'linux';
  const current = cfg.get<Record<string, string>>(platKey) ?? {};
  const sep = process.platform === 'win32' ? ';' : ':';
  current.PATH = `${current.PATH ?? '${env:PATH}'}${sep}${dir}`;
  await cfg.update(platKey, current, vscode.ConfigurationTarget.Global);
}

async function manualInstructions(dir: string): Promise<void> {
  const msg = `Add to PATH manually: ${dir}`;
  const action = await vscode.window.showInformationMessage(msg, 'Copy path');
  if (action === 'Copy path') await vscode.env.clipboard.writeText(dir);
}
```

## Done when
- [ ] `userPath` on Windows: `setx PATH` runs, `where session-recall` works in a new terminal
- [ ] `userPath` on Linux: `~/.profile` updated, idempotent on re-run
- [ ] `vscodePath`: `terminal.integrated.env.<platform>` updated; integrated terminal sees binary
- [ ] `manual`: information message shown with copy-to-clipboard action
