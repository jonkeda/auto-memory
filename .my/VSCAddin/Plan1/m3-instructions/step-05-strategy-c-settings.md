# Step 05 — Strategy C (settings.json)

## Goal
Add an entry to `github.copilot.chat.codeGeneration.instructions` array; also create the underlying `.instructions.md` file (Strategy B logic) so the pointer resolves.

## Edits

```typescript
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
```

## Done when
- [ ] `settings.json` shows new entry in the array
- [ ] Underlying `.instructions.md` file is created via Strategy B path
- [ ] Re-running `addC` does not duplicate the entry
- [ ] `removeC` filters out the entry without disturbing other entries
