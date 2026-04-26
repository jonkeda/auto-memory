# Step 06 — Update + uninstall commands

## Goal
Implement `auto-memory.updateBinary` and `auto-memory.uninstall` commands.

## Update flow

```typescript
export async function updateBinary(mgr: BinaryManager): Promise<void> {
  const installed = await mgr.installedVersion();
  const bundled   = await bundledVersion(mgr);
  if (!bundled) {
    vscode.window.showErrorMessage('No bundled binary available');
    return;
  }
  if (installed === bundled) {
    vscode.window.showInformationMessage(`Already up to date (${installed})`);
    return;
  }
  await installBinary(mgr);  // copies bundled over installed
  vscode.window.showInformationMessage(`Updated ${installed ?? 'none'} → ${bundled}`);
}

async function bundledVersion(mgr: BinaryManager): Promise<string | null> {
  const src = mgr.bundledBinaryPath();
  if (!src) return null;
  return new Promise(resolve =>
    execFile(src, ['--version'], (err, stdout) => resolve(err ? null : stdout.trim())));
}
```

## Auto-update on activation

In `extension.ts` activate(), after registering commands:

```typescript
if (vscode.workspace.getConfiguration('autoMemory').get<boolean>('autoUpdate', true)) {
  // fire-and-forget; never block activation
  void maybeAutoUpdate(mgr);
}
```

`maybeAutoUpdate` checks installed vs bundled silently; if differ, shows a notification with **Update** / **Skip** buttons.

## Uninstall flow

```typescript
export async function uninstall(mgr: BinaryManager): Promise<void> {
  const ok = await vscode.window.showWarningMessage(
    'Remove session-recall binary and PATH entries?', { modal: true }, 'Yes', 'No');
  if (ok !== 'Yes') return;
  const fs = await import('fs/promises');
  try { await fs.rm(mgr.installDir(), { recursive: true, force: true }); } catch { /* ignore */ }
  // PATH cleanup is best-effort: log message, do not modify setx output
  vscode.window.showInformationMessage('Auto Memory uninstalled. PATH entries may need manual cleanup.');
}
```

## Done when
- [ ] `updateBinary` no-ops when versions match, copies otherwise
- [ ] Auto-update prompt appears when bundled differs from installed
- [ ] `uninstall` requires modal confirmation, removes install dir
