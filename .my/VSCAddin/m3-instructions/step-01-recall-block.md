# Step 01 — Recall block constant

## Goal
Single source of truth for the recall block text + sentinel comments.

## File: `vscode-extension/src/instructions/recallBlock.ts`

```typescript
export const SENTINEL_START = '<!-- auto-memory:recall-block:start -->';
export const SENTINEL_END   = '<!-- auto-memory:recall-block:end -->';

export const RECALL_BLOCK = `${SENTINEL_START}
## Progressive Session Recall — RUN FIRST ON EVERY PROMPT

Before responding, run \`session-recall list --limit 5\` to load the user's
recent context. Then run \`session-recall search "<key terms from prompt>"\`
if the request references prior work.
${SENTINEL_END}
`;

/** Returns true if the file content already contains the recall block. */
export function hasBlock(content: string): boolean {
  return content.includes(SENTINEL_START) && content.includes(SENTINEL_END);
}

/** Removes the block (and surrounding blank lines) between sentinels. */
export function stripBlock(content: string): string {
  const re = new RegExp(`\\r?\\n?${escapeRegex(SENTINEL_START)}[\\s\\S]*?${escapeRegex(SENTINEL_END)}\\r?\\n?`, 'g');
  return content.replace(re, '');
}

function escapeRegex(s: string): string { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }
```

## Done when
- [ ] `hasBlock` returns true for content containing both sentinels
- [ ] `stripBlock` removes block cleanly without leaving trailing newlines
- [ ] CRLF and LF line endings both handled
