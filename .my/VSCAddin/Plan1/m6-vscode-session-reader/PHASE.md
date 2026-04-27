# M6 — VS Code Session-State Reader

## Goal

Read sessions from `~/.copilot/session-state/<uuid>/` (the flat-file format written by VS Code Copilot)
and surface them in the dashboard exactly as if they came from the SQLite `session-store.db`.

The user never has to install the Copilot CLI; `session-recall` works with whatever data is present.

## Steps

| # | File | Title |
|---|---|---|
| 01 | `step-01-vscode-session-model.md`     | Define `VsCodeSession` model + parse `workspace.yaml` |
| 02 | `step-02-events-reader.md`            | `EventsReader`: stream `events.jsonl`, extract tool calls + file refs |
| 03 | `step-03-session-state-store.md`      | `SessionStateStore`: discover + enumerate `session-state/` dirs |
| 04 | `step-04-cli-commands.md`             | Wire `list`, `show`, `search`, `files`, `checkpoints` to flat-file path |
| 05 | `step-05-dashboard-integration.md`    | Extension: auto-detect format, route commands to correct reader |
| 06 | `step-06-tests.md`                    | Unit + integration tests with fixture session-state dirs |

## Data contract

### Input — `~/.copilot/session-state/<uuid>/`

```
<uuid>/
  workspace.yaml            # id, repository, branch, summary, created_at, updated_at
  events.jsonl              # one JSON object per line; see event types below
  vscode.metadata.json      # firstUserMessage, created, modified (epoch ms)
  checkpoints/
    index.md                # markdown table of checkpoints
    <N>-<title>.md          # individual checkpoint files (optional)
  files/                    # files touched (optional, may be empty)
```

### Key `workspace.yaml` fields

```yaml
id: <uuid>
cwd: <absolute path>
git_root: <absolute path>
repository: <owner/repo>            # e.g. jonkeda/Oravey2
host_type: github
branch: main
summary_count: 0
created_at: 2026-04-10T07:32:55.483Z
updated_at: 2026-04-10T07:32:56.382Z
summary: "<first few lines of the session summary>"
```

### Key event types in `events.jsonl`

| Event type | Relevant fields |
|---|---|
| `session.start` | `data.sessionId`, `data.startTime`, `data.selectedModel`, `data.context.{cwd,repository,branch}` |
| `user.message` | `data.content`, `data.interactionId`, `timestamp` |
| `assistant.message` | `data.content`, `timestamp` |
| `tool.execution_start` | `data.toolName`, `data.toolCallId`, `data.arguments`, `timestamp` |
| `tool.execution_complete` | `data.toolCallId`, `data.result`, `timestamp` |
| `session.shutdown` | `timestamp` (marks session end) |

### Output JSON shapes (must match existing dashboard protocol)

**`list --json`**
```json
{
  "sessions": [
    { "id": "<uuid>", "date": "2026-04-10", "repo": "jonkeda/Oravey2", "summary": "..." }
  ],
  "source": "vscode-session-state"
}
```

**`show <id> --json`**
```json
{
  "id": "<uuid>",
  "date": "2026-04-10T07:32:55Z",
  "repo": "jonkeda/Oravey2",
  "branch": "main",
  "model": "claude-haiku-4.5",
  "summary": "...",
  "tool_calls": [{ "name": "submit_town_design", "timestamp": "..." }],
  "source": "vscode-session-state"
}
```

**`search <query> --json`**
```json
{
  "results": [
    { "source": "vscode-session-state", "excerpt": "...", "date": "2026-04-10", "session_id": "<uuid>" }
  ]
}
```

## Done when

- [ ] `session-recall list --json` returns sessions from `session-state/` when no `session-store.db` exists
- [ ] `session-recall show <id> --json` works for VS Code session UUIDs
- [ ] `session-recall search <query> --json` searches `workspace.yaml` summaries + `events.jsonl` user messages
- [ ] `session-recall files --json` enumerates files from `events.jsonl` tool calls where `toolName` references file paths
- [ ] Dashboard shows sessions list and health stub when in VS Code session-state mode
- [ ] All new code covered by tests using fixture session-state directories
