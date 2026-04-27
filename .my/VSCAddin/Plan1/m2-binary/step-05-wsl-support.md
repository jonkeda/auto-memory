# Step 05 — WSL support

## Goal
Detect when running in WSL remote and install Linux binary into `/usr/local/bin` (or `~/.local/bin` if not root).

## Edits to `BinaryManager`

```typescript
isWsl(): boolean {
  return vscode.env.remoteName === 'wsl';
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

bundledBinaryPath(): string | null {
  // Under WSL, force linux-x64 even though process.platform may report 'win32' from the host
  if (this.isWsl()) {
    return path.join(this.context.extensionPath, 'bin', 'linux-x64', 'session-recall');
  }
  // ... existing logic
}
```

## Done when
- [ ] Opening a WSL Remote window: `BinaryManager.isWsl()` returns `true`
- [ ] Install command places `session-recall` (no `.exe`) in `~/.local/bin/`
- [ ] Binary is executable inside WSL (`./session-recall --version` works)
