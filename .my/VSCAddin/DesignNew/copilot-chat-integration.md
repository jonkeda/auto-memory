# Copilot Chat integration

How AutoMemory plugs *into* the Copilot Chat panel itself, not just sits next
to it. Today the extension is a pure dashboard — a webview reading session
files. This doc describes a layered set of integrations with the Chat panel
so recall context flows in both directions.

## Surfaces VS Code exposes

VS Code's Chat extensibility surface (Apr 2026) gives third parties four
distinct hooks:

1. **Chat participant** (`@autoMemory`) — owns the prompt when @-mentioned.
2. **Slash commands** (`@autoMemory /find`) — shortcuts on a participant.
3. **Language-model tool** (`#recall`) — agent-mode invocable function the
   LLM can call autonomously.
4. **Context contribution** — tools/participants can stream
   `stream.reference(...)` and `stream.anchor(...)` so prior content shows
   up in Copilot's "References" tray.

We use all four, with deliberate scoping.

## Why integrate (vs. staying a dashboard)

Today, recall lives outside the conversation. The user has to:

1. See a hint in our panel (`Repo Coverage: RED — Open Copilot Chat`).
2. Switch to the Chat view.
3. Re-type the question.
4. Hope Copilot has enough context.

After integration the user types `#recall react hooks` (or just asks a
question while the tool is enabled), Copilot fetches matching past sessions
via our tool, and uses them as context. Recall becomes part of the prompt,
not a separate app.

## Integration tiers

### Tier A — `#recall` language-model tool (highest leverage)

A registered tool that the Copilot agent can call autonomously to retrieve
relevant past sessions and turn excerpts.

**`package.json` contribution:**

```json
{
  "contributes": {
    "languageModelTools": [
      {
        "name": "auto-memory_recall",
        "displayName": "Recall past sessions",
        "modelDescription": "Search the local AutoMemory index of past Copilot Chat and Copilot CLI sessions for prior context that is relevant to the current user request. Use this tool when the user references something they 'did before', when the conversation needs continuity across sessions, when the current repo or task has likely been discussed in prior sessions, or when the user asks 'how did I solve X'. Returns up to N session excerpts with date, repo, source, and 1–3 turn snippets each. Do not call for trivia / world-knowledge questions; this tool only retrieves the user's own past sessions.",
        "toolReferenceName": "recall",
        "canBeReferencedInPrompt": true,
        "icon": "$(history)",
        "userDescription": "Look up your past Copilot sessions",
        "tags": ["auto-memory", "memory", "context"],
        "inputSchema": {
          "type": "object",
          "properties": {
            "query": {
              "type": "string",
              "description": "Free-text query to match against session summaries and turn content."
            },
            "limit": {
              "type": "number",
              "description": "Maximum number of sessions to return. Default 5, max 20."
            },
            "repo_only": {
              "type": "boolean",
              "description": "Restrict to sessions associated with the currently open workspace repo. Default true."
            },
            "days": {
              "type": "number",
              "description": "Restrict to sessions from the last N days. Default 90."
            }
          },
          "required": ["query"]
        }
      }
    ]
  }
}
```

**Implementation sketch:**

```ts
class RecallTool implements vscode.LanguageModelTool<RecallParams> {
  async prepareInvocation(opts) {
    return {
      invocationMessage: `Searching ${opts.input.repo_only ? 'this repo' : 'all repos'} for "${opts.input.query}"…`,
      // No confirmation: read-only over local files. VS Code still shows the
      // generic extension confirmation on first use.
    };
  }

  async invoke(opts, token) {
    const { query, limit = 5, repo_only = true, days = 90 } = opts.input;
    const out = await runner.run([
      'search', '--json', '--limit', String(Math.min(limit, 20)),
      '--days', String(days),
      ...(repo_only ? ['--repo', currentRepo()] : []),
      '--', query,
    ]);
    const json = JSON.parse(out);
    return new vscode.LanguageModelToolResult([
      new vscode.LanguageModelTextPart(formatForLlm(json)),
    ]);
  }
}
```

`formatForLlm` returns a compact deterministic markdown like:

```
## Recall — 3 sessions matching "react hooks"

### 1. 2026-04-12 · repo-A · chat
Summary: Refactor login form to useReducer instead of multiple useState
Turn 3 (user): "the dependency array warning persists after…"
Turn 3 (assistant): "Switch to useCallback for handleSubmit because…"

### 2. …
```

The format is tuned so the LLM can cite specific sessions (e.g.
"according to your Apr 12 session…") and so we stay well under tool-result
token budgets. Cap result body at ~4 KB total; truncate older / lower-rank
sessions first.

**Cost / token notes.** The tool can be invoked multiple times per turn
(VS Code agent loops). We rely on the existing 60 s `runner` cache so the
second call within a tool-loop is free.

### Tier B — `@autoMemory` chat participant

For users who want to call AutoMemory explicitly without enabling the tool
in agent mode. Lighter than the tool — a participant always replies once;
it does not chain.

**`package.json`:**

```json
{
  "contributes": {
    "chatParticipants": [
      {
        "id": "auto-memory.autoMemory",
        "name": "autoMemory",
        "fullName": "AutoMemory",
        "description": "Search and recall past Copilot sessions",
        "isSticky": false,
        "commands": [
          { "name": "find", "description": "Search past sessions for a query" },
          { "name": "last", "description": "Show the most recent session for this repo" },
          { "name": "summary", "description": "Summarise past sessions touching a topic" },
          { "name": "health", "description": "Show recall health for this workspace" }
        ],
        "disambiguation": [
          {
            "category": "recall",
            "description": "The user is asking about something they did before, looking up a past Copilot session, or wants continuity from earlier conversations.",
            "examples": [
              "What did I ask about React hooks last week?",
              "Find the session where we refactored TranscriptReader",
              "Show my last Copilot session in this repo",
              "Summarise what I worked on in repo-A this month"
            ]
          }
        ]
      }
    ]
  }
}
```

**Handler responsibilities:**

| Slash | Behaviour |
|---|---|
| (no command) | Treat the prompt as a recall query: run `session-recall search`, render top 5 as markdown with `stream.reference(fileUri)` per matched transcript file, finish with three `stream.button(...)` buttons (`Open in dashboard`, `Copy summary`, `Insert into chat`) |
| `/find <q>` | Same as default but explicitly search; never delegates to the LLM |
| `/last` | Render the most recent session header + 1-line summary + Resume button (links to the AutoMemory dashboard route `#/find/<id>` via `command:` URI) |
| `/summary <topic>` | Run `search`, then ask the LLM (`request.model`) to synthesise a 4–6 sentence summary across the returned sessions; stream the LLM response, append the references list at the end |
| `/health` | Render `health --json` zones as a one-screen status, with a `Open AutoMemory dashboard` button |

**Output styling:**

- Always include `stream.reference(uri)` for each cited transcript so VS
  Code's "References" tray shows them — this is the dominant native
  affordance for "I used these files".
- Use `stream.anchor(...)` on inline session ids so the user can click
  `#a1b2c3d4` and jump to the dashboard route.
- Buttons invoke our existing commands:
  `command:auto-memory.openSession?<encoded id>`,
  `command:auto-memory.openDashboard`.

**Follow-ups (`followupProvider`):** after a `/find`, suggest:
`{ prompt: "Open the top result", label: "Open #a1b2c3d4 in editor tab" }`
and `{ prompt: "Summarise these in one paragraph", label: "Summarise" }`.

### Tier C — Inline chat support (`request.location`)

When `request.location === vscode.ChatLocation.Editor` (inline chat) or
`Terminal`, the participant shortens its output:

- No buttons (inline UX is cramped).
- One-line summary per session, no excerpts.
- `Esc` is the only dismissal — keep response under ~6 lines.

This is purely a rendering switch in the same handler.

### Tier D — Context contributions to other participants

When the user types in plain chat (no `@autoMemory`), VS Code's tool-calling
flow may still pull in `#recall` if the tool is enabled. Tier A already
handles this — no extra code. We just ensure:

- `tags: ["auto-memory","memory","context"]` so other participants (e.g.
  `@workspace`) can opt-in via `vscode.lm.tools.filter(t => t.tags.includes('memory'))`.
- The tool's `modelDescription` is precise enough that the agent picks it
  only when relevant (avoid spurious calls on every prompt).

## End-to-end flows

### Flow F1 — User asks a continuity question (Tier A)

```
User in Chat view (agent mode, no @-mention):
  "Where did we land on the React hooks refactor?"

VS Code agent → LLM with tool list including #recall
LLM decides to call: recall({ query: "React hooks refactor", repo_only: true, limit: 5 })
Tool runs `session-recall search --json --repo currentRepo -- "React hooks refactor"`
Returns markdown blob (3 sessions, ~2 KB)
LLM uses excerpts as context, replies:
  "Based on your Apr 12 session, you switched to useReducer. The remaining
   bug was the dependency array warning, which you fixed by wrapping
   handleSubmit in useCallback."

User sees the answer + a References tray entry: 'session 0a1b2c3d.jsonl'
```

### Flow F2 — Explicit recall (Tier B)

```
User: "@autoMemory /find typescript decorators"

Participant streams:
  - "Found 4 sessions in the last 90 days:"
  - 4 markdown rows with date / repo / 1-line summary / inline anchor
  - References tray: 4 transcript file URIs
  - Buttons: [Open all in dashboard] [Copy as markdown]

Followups: 'Show only this repo' / 'Summarise these'
```

### Flow F3 — Resuming after a break (Tier B `/last`)

```
User opens VS Code on Monday, types "@autoMemory /last"

Participant streams:
  - Header: "Friday Apr 24 16:51 · repo-A · chat"
  - Summary: "Refactored TranscriptReader to use File.ReadLines"
  - Last 2 user-turns (1-line each)
  - Buttons: [Resume in dashboard] [Open repo-A] [Copy summary]
```

## UX details

### Authoring guidelines for participant responses

- **First line is the answer.** Never start with "I'll search…" — the
  status line goes through `stream.progress(...)`.
- **Cite sessions inline.** Use `stream.anchor(uri, '#a1b2c3d4')` so the
  reader can click to jump to the dashboard.
- **Always finish with references.** Each cited session gets a
  `stream.reference(transcriptUri)` so VS Code's "Used X references" pill
  shows up — that's how users learn which files were read.
- **Keep buttons to ≤ 3.** More buttons fragment attention; users prefer
  follow-up chips.

### Authoring guidelines for tool responses

- **Output is for the LLM, not the user.** No emoji, no greetings, no
  conversational scaffolding.
- **Deterministic structure.** Stable markdown headings so the LLM can
  reliably reference "session 1, 2, 3" from one call to the next.
- **Cap size.** ~4 KB total; truncate the body of older sessions first,
  keep their headers so the LLM can still mention them.
- **Errors as instructions.** If the binary is missing, throw with a
  message like: "AutoMemory binary not installed. Tell the user to run the
  command 'AutoMemory: Install Binary' and then retry."

### Telemetry hooks

Wire `cat.onDidReceiveFeedback` (rename to `autoMemory.onDidReceiveFeedback`)
to count thumbs-up / thumbs-down on each participant turn. For the tool,
count `prepareInvocation` calls vs. successful `invoke` calls. These let us
compute a recall-relevance ratio.

## Backwards compatibility

- The Chat extension surface is part of `vscode.proposed.chatParticipant` /
  `vscode.proposed.lmTools` for some VS Code versions. Use the stable APIs
  available in `vscode.chat.*` and `vscode.lm.*` (≥ 1.95) and gate any
  proposed surface behind a feature check.
- `engines.vscode` should remain `^1.90.0`; for the integration milestone
  bump to whichever version stabilised the chat participant API used here
  (likely 1.97). Bump only in the integration milestone, not earlier.

## Privacy and trust

- The tool only reads local files (`~/.copilot/`,
  `workspaceStorage/.../GitHub.copilot-chat/transcripts/`). Make this
  explicit in `userDescription`: "Reads only your local Copilot history;
  no network calls."
- Session content is sent to the LLM as part of the chat prompt — that's
  unavoidable for the tool to be useful, but it means the tool *transmits*
  past chat content to the model the user picked. Surface this on first
  use via `prepareInvocation` confirmation message:
  > Recall will send up to 5 past session excerpts to the chat model so it
  > can answer with context. Continue?
  After the first "Always allow", VS Code suppresses the dialog.
- Add a setting `auto-memory.chat.tool.enabled` (default `true`) so users
  can globally disable the tool while keeping the participant.

## Settings

| Key | Default | Effect |
|---|---|---|
| `auto-memory.chat.participant.enabled` | `true` | Register `@autoMemory` |
| `auto-memory.chat.tool.enabled` | `true` | Register `#recall` LM tool |
| `auto-memory.chat.recall.maxResults` | `5` | Default `limit` for tool calls |
| `auto-memory.chat.recall.repoOnly` | `true` | Default for `repo_only` |
| `auto-memory.chat.recall.days` | `90` | Default `days` window |

## Milestone fit

Slot into the post-redesign roadmap from
[migration.md](migration.md):

```
M9   Dashboard redesign foundation
M10  Glance + health detail
M11  Find + session detail
M12  Setup + diagnostics
M13  Chat integration  ← this doc
  step-01  Tool: register #recall, stub invoke
  step-02  Tool: real invoke wiring `session-recall search`, format-for-llm
  step-03  Participant: register @autoMemory, default + /find handler
  step-04  Participant: /last, /summary, /health handlers + followup provider
  step-05  Participant: inline-chat short-form rendering
  step-06  Settings + privacy confirmation message + telemetry
  step-07  Tests (handler unit tests, end-to-end smoke via Test Electron)
```

M13 depends only on M11 (search is solid by then) and M12 (settings
pattern). It does *not* require M10 — the dashboard and chat surfaces are
independent.

## What this doc is *not*

- Not a generative-AI feature. The participant uses `request.model` only
  in `/summary`. Everywhere else, AutoMemory remains deterministic search
  over local files.
- Not a replacement for the dashboard. The panel still owns Setup, Health,
  and full transcript reading. The chat surface is a fast-path for
  in-conversation recall.
- Not a memory writer. We never write back into Copilot's chat history;
  the tool is read-only.
