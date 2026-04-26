# Step 06 — Add-instructions command

## Goal
Wire `auto-memory.addInstructions` to a Quick Pick and call `InstructionsManager`.

## File: `vscode-extension/src/instructions/command.ts`

```typescript
import * as vscode from 'vscode';
import { InstructionsManager, Strategy } from './InstructionsManager';

export async function addInstructionsCommand(mgr: InstructionsManager): Promise<void> {
  const pick = await vscode.window.showQuickPick([
    { label: 'A — Global',   description: '~/.copilot/copilot-instructions.md', value: 'A' as Strategy },
    { label: 'B — Scoped',   description: '.github/instructions/session-recall.instructions.md', value: 'B' as Strategy },
    { label: 'C — settings', description: 'workspace settings.json entry', value: 'C' as Strategy },
  ], { placeHolder: 'Choose where to add the recall block' });
  if (!pick) return;
  try {
    const before = await mgr.status(pick.value);
    if (before.installed) {
      vscode.window.showInformationMessage(`Already installed at ${before.path}`);
      return;
    }
    await mgr.add(pick.value);
    const after = await mgr.status(pick.value);
    vscode.window.showInformationMessage(`Recall block added to ${after.path}`);
  } catch (e) {
    vscode.window.showErrorMessage(`Failed: ${(e as Error).message}`);
  }
}
```

Wire into `extension.ts`:

```typescript
const im = new InstructionsManager();
['auto-memory.addInstructions', () => addInstructionsCommand(im)],
```

## Done when
- [ ] Quick Pick shows three options
- [ ] Selecting an option runs the right strategy
- [ ] Already-installed case shows info message, doesn't write
- [ ] Errors surface as error messages, not throws
