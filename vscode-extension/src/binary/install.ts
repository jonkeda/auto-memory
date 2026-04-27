import * as vscode from 'vscode';
import * as fs from 'fs/promises';
import * as path from 'path';
import { BinaryManager } from './BinaryManager';
import { applyPathStrategy } from './path-strategy';
import { execFile } from 'child_process';

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

  // Copy native dependencies (e.g. e_sqlite3.dll) that live next to the bundled exe
  const srcDir = path.dirname(src);
  const dstDir = path.dirname(dst);
  for (const entry of await fs.readdir(srcDir)) {
    if (entry === path.basename(src)) continue;
    if (!/\.(dll|so|dylib)$/i.test(entry)) continue;
    await fs.copyFile(path.join(srcDir, entry), path.join(dstDir, entry));
  }

  const strategy = vscode.workspace.getConfiguration('autoMemory')
    .get<'userPath'|'vscodePath'|'manual'>('pathStrategy', 'userPath');
  await applyPathStrategy(strategy, mgr.installDir());

  vscode.window.showInformationMessage(`Auto Memory installed to ${dst}`);
}

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

export async function uninstall(mgr: BinaryManager): Promise<void> {
  const ok = await vscode.window.showWarningMessage(
    'Remove session-recall binary and PATH entries?', { modal: true }, 'Yes', 'No');
  if (ok !== 'Yes') return;
  try { await fs.rm(mgr.installDir(), { recursive: true, force: true }); } catch { /* ignore */ }
  // PATH cleanup is best-effort: log message, do not modify setx output
  vscode.window.showInformationMessage('Auto Memory uninstalled. PATH entries may need manual cleanup.');
}

export async function maybeAutoUpdate(mgr: BinaryManager): Promise<void> {
  const installed = await mgr.installedVersion();
  const bundled   = await bundledVersion(mgr);
  if (!bundled || installed === bundled) return;
  
  const action = await vscode.window.showInformationMessage(
    `A new version of session-recall is available: ${bundled}`,
    'Update', 'Skip'
  );
  if (action === 'Update') {
    await installBinary(mgr);
    vscode.window.showInformationMessage(`Updated to ${bundled}`);
  }
}

async function bundledVersion(mgr: BinaryManager): Promise<string | null> {
  const src = mgr.bundledBinaryPath();
  if (!src) return null;
  return new Promise(resolve =>
    execFile(src, ['--version'], (err, stdout) => resolve(err ? null : stdout.trim())));
}

