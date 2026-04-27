# Step 03 — Strategy A (global)

## Goal
Implement `statusA` and `addA` for `~/.copilot/copilot-instructions.md`.

## Edits to `InstructionsManager.ts`

```typescript
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
```

## Done when
- [ ] `addA()` creates `~/.copilot/` dir if missing
- [ ] Block appended cleanly with single separating newline
- [ ] Re-running `addA()` does NOT duplicate the block
- [ ] `statusA()` reflects file state correctly
