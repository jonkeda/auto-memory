export const SENTINEL_START = '<!-- auto-memory:recall-block:start -->';
export const SENTINEL_END   = '<!-- auto-memory:recall-block:end -->';

export const RECALL_BLOCK = `${SENTINEL_START}
## Progressive Session Recall — RUN FIRST ON EVERY PROMPT

**Run \`session-recall\` FIRST on every prompt before doing anything else.** It costs ~50 tokens and prevents expensive blind searches.

\`\`\`bash
session-recall files --json --limit 10  # recently touched files
session-recall files --days 7 --json    # files touched in last 7 days
session-recall list --json --limit 5    # recent sessions
session-recall list --days 2 --json     # sessions from last 2 days
session-recall search '<term>' --json   # full-text search
session-recall search '<term>' --days 5 # search last 5 days only
session-recall checkpoints --days 3     # checkpoints from last 3 days
session-recall show <id> --json         # drill into one session
session-recall health --json            # 8-dimension health check
session-recall schema-check             # validate DB schema (run after Copilot CLI upgrade)
\`\`\`

**\`--days N\` works on all 4 query commands** (\`list\`, \`files\`, \`checkpoints\`, \`search\`) — filters to sessions/files/checkpoints from the last N days.

Only use filesystem tools (grep, glob, find) if session-recall returns nothing useful.
If \`session-recall\` errors, continue silently — it's a convenience, not a blocker.
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
