# Step 04 — Strategy B (scoped file)

## Goal
Write `<root>/.github/instructions/session-recall.instructions.md` with frontmatter.

## Edits

```typescript
private scopedPath(root: string): string {
  return path.join(root, '.github', 'instructions', 'session-recall.instructions.md');
}

private async statusB(root: string): Promise<Status> {
  const p = this.scopedPath(root);
  try {
    const content = await fs.readFile(p, 'utf8');
    return { installed: hasBlock(content), path: p };
  } catch { return { installed: false, path: p }; }
}

private async addB(root: string): Promise<void> {
  const p = this.scopedPath(root);
  await fs.mkdir(path.dirname(p), { recursive: true });
  let content = '';
  try { content = await fs.readFile(p, 'utf8'); } catch { /* new file */ }
  if (hasBlock(content)) return;
  if (content.length === 0) {
    content = '---\napplyTo: "**"\n---\n';
  } else if (!content.endsWith('\n')) {
    content += '\n';
  }
  await fs.writeFile(p, content + RECALL_BLOCK);
}
```

## Done when
- [ ] New file created with frontmatter `applyTo: "**"`
- [ ] Existing file: block appended without disturbing frontmatter
- [ ] Idempotent
