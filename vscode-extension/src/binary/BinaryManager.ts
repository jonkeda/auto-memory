import * as vscode from 'vscode';
import { execFile } from 'child_process';
import * as path from 'path';
import * as fs from 'fs/promises';
import * as os from 'os';

export type Platform = 'win32-x64' | 'linux-x64' | 'darwin-arm64';

export class BinaryManager {
  constructor(private readonly context: vscode.ExtensionContext) {}

  isWsl(): boolean {
    return vscode.env.remoteName === 'wsl';
  }

  platform(): Platform | null {
    const p = process.platform, a = process.arch;
    if (p === 'win32'  && a === 'x64')   return 'win32-x64';
    if (p === 'linux'  && a === 'x64')   return 'linux-x64';
    if (p === 'darwin' && a === 'arm64') return 'darwin-arm64';
    return null;
  }

  bundledBinaryPath(): string | null {
    // Under WSL, force linux-x64 even though process.platform may report 'win32' from the host
    if (this.isWsl()) {
      return path.join(this.context.extensionPath, 'bin', 'linux-x64', 'session-recall');
    }
    const plat = this.platform();
    if (!plat) return null;
    const exe = plat.startsWith('win') ? 'session-recall.exe' : 'session-recall';
    return path.join(this.context.extensionPath, 'bin', plat, exe);
  }

  installDir(): string {
    if (this.isWsl()) {
      // WSL: prefer ~/.local/bin (no sudo needed)
      return path.join(os.homedir(), '.local', 'bin');
    }
    if (process.platform === 'win32')
      return path.join(process.env.LOCALAPPDATA ?? os.homedir(), 'auto-memory');
    return path.join(os.homedir(), '.local', 'auto-memory');
  }

  installedBinaryPath(): string {
    const exe = process.platform === 'win32' ? 'session-recall.exe' : 'session-recall';
    return path.join(this.installDir(), exe);
  }

  /** Resolved binary to invoke: setting override > installed > bundled. */
  async resolve(): Promise<string | null> {
    const cfg = vscode.workspace.getConfiguration('autoMemory').get<string>('binaryPath');
    if (cfg && await this.exists(cfg)) return cfg;
    const installed = this.installedBinaryPath();
    if (await this.exists(installed)) return installed;
    const bundled = this.bundledBinaryPath();
    if (bundled && await this.exists(bundled)) return bundled;
    return null;
  }

  async installedVersion(): Promise<string | null> {
    const bin = await this.resolve();
    if (!bin) return null;
    return new Promise(resolve => {
      execFile(bin, ['--version'], { timeout: 5_000 }, (err, stdout) =>
        resolve(err ? null : stdout.trim())
      );
    });
  }

  private async exists(p: string): Promise<boolean> {
    try { await fs.access(p); return true; } catch { return false; }
  }
}
