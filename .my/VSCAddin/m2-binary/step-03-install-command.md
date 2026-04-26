# Step 03 — Install command

## Goal
Implement `auto-memory.installBinary` command: copies bundled binary to `installDir()`, sets executable bit on Unix, prompts for PATH strategy.

## File: `vscode-extension/src/binary/install.ts`

```typescript
import * as vscode from 'vscode';
import * as fs from 'fs/promises';
import * as path from 'path';
import { BinaryManager } from './BinaryManager';
import { applyPathStrategy } from './path-strategy';

export async function installBinary(mgr: BinaryManager): Promise<void> {
  const src = mgr.bundledBinaryPath();
  if (!src) {
    vscode.window.showErrorMessage('Auto Memory: no bundled binary for this platform');
    return;
  }
  const dst = mgr.installedBinaryPath();
  await fs.mkdir(path.dirname(dst), { recursive: true });
  await fs.copyFile(src, dst);
  if (process.platform !== 'win32') await fs.chmod(dst, 0o755);

  const strategy = vscode.workspace.getConfiguration('autoMemory')
    .get<'userPath'|'vscodePath'|'manual'>('pathStrategy', 'userPath');
  await applyPathStrategy(strategy, mgr.installDir());

  vscode.window.showInformationMessage(`Auto Memory installed to ${dst}`);
}
```

Wire into `extension.ts`:

```typescript
const mgr = new BinaryManager(context);
// ... replace stub:
['auto-memory.installBinary', () => installBinary(mgr)],
```

## Done when
- [ ] Running command copies binary to install dir
- [ ] Executable bit set on Linux/macOS (verify `stat`)
- [ ] Re-running command overwrites cleanly without error
