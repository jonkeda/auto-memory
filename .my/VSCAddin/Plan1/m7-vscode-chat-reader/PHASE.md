# M7 — VS Code Copilot Chat Reader

## Goal

Read sessions from VS Code Copilot Chat extension storage
(`%APPDATA%\Code - Insiders\User\workspaceStorage\<hash>\GitHub.copilot-chat\transcripts\<session-id>.jsonl`)
and surface them in the dashboard alongside the existing Copilot CLI session-state sessions.

**These are the "local VSC" sessions the user sees in the VS Code chat panel.**
They are entirely different from the `~/.copilot/session-state/` sessions (which are Copilot CLI).

### Why two backends?

| Source | Location | Format | Product |
|---|---|---|---|
| Copilot CLI | `~/.copilot/session-store.db` | SQLite | `gh copilot` CLI |
| Session-State | `~/.copilot/session-state/<uuid>/` | Flat-file YAML+JSONL | VS Code extension embedded CLI |
| **Chat (new)** | `workspaceStorage/<hash>/GitHub.copilot-chat/transcripts/<id>.jsonl` | JSONL | VS Code Copilot Chat panel |

### Coexistence with VS Code Copilot Chat

- auto-memory reads the transcript files **read-only**; it never writes or modifies them.
- VS Code continues to manage its own storage; no interference.
- The VS Code extension (auto-memory VSIX) has access to the same `%APPDATA%` paths.
- On stable VS Code: `%APPDATA%\Code\User\workspaceStorage\...` (no " - Insiders" suffix).
- Both stable and Insiders paths are scanned.

## Steps

| # | File | Title |
|---|---|---|
| 01 | `step-01-chat-session-model.md`    | Define `ChatSession` model + enumerate workspaceStorage |
| 02 | `step-02-transcript-reader.md`     | `TranscriptReader`: stream transcript JSONL, extract events |
| 03 | `step-03-chat-session-store.md`    | `ChatSessionStore`: discover all workspaces, list/show/search |
| 04 | `step-04-cli-commands.md`          | Wire `list`, `show`, `search` to Chat fallback |
| 05 | `step-05-dashboard-integration.md` | Show Chat sessions in dashboard (source badge + count) |
| 06 | `step-06-tests.md`                 | Unit + integration tests with fixture transcript dirs |

## Data Contract

### Storage root discovery (Windows)

```
%APPDATA%\Code - Insiders\User\workspaceStorage\<hash>\GitHub.copilot-chat\transcripts\
%APPDATA%\Code\User\workspaceStorage\<hash>\GitHub.copilot-chat\transcripts\
```

For each hash:
- `<hash>\workspace.json` — `{"workspace": "file:///e%3A/repos/..."}` — URL-decode to get local path
- `<hash>\GitHub.copilot-chat\transcripts\<session-id>.jsonl` — one file per chat session

### Transcript JSONL event types

```json
// session.start — always first line
{"type":"session.start","data":{"sessionId":"...","version":1,"producer":"copilot-agent",
  "copilotVersion":"0.46.x","vscodeVersion":"1.118.x","startTime":"2026-04-25T18:50:04.999Z"},
 "id":"...","timestamp":"2026-04-25T18:50:04.999Z","parentId":null}

// user.message — user's typed prompt
{"type":"user.message","data":{"content":"...text...","attachments":[]},"id":"...","timestamp":"...","parentId":"..."}

// assistant.message — Copilot response
{"type":"assistant.message","data":{"messageId":"...","content":"...","toolRequests":[
  {"toolCallId":"...","name":"read_file","arguments":"...json string...","type":"function"}
],"reasoningText":"..."},"id":"...","timestamp":"...","parentId":"..."}

// tool.execution_start
{"type":"tool.execution_start","data":{"toolCallId":"...","toolName":"read_file","arguments":{...}},"id":"...","timestamp":"...","parentId":"..."}

// tool.execution_complete
{"type":"tool.execution_complete","data":{"toolCallId":"...","success":true},"id":"...","timestamp":"...","parentId":"..."}

// assistant.turn_start / assistant.turn_end — bracket each assistant turn
{"type":"assistant.turn_start","data":{"turnId":"0"},"id":"...","timestamp":"...","parentId":"..."}
{"type":"assistant.turn_end","data":{"turnId":"0"},"id":"...","timestamp":"...","parentId":"..."}
```

### Extracted fields per session

| Field | Source |
|---|---|
| `id` | filename stem (the `<session-id>` UUID) |
| `workspace_path` | URL-decoded `workspace.json["workspace"]` |
| `repository` | derive from workspace path (git remote lookup or simple folder heuristic) |
| `created_at` | `session.start` → `data.startTime` |
| `updated_at` | last event `timestamp` in the file |
| `summary` | first `user.message` → `data.content` (truncated to 200 chars) |
| `turn_count` | count of `user.message` events |
| `tool_count` | count of `tool.execution_start` events |

### JSON output shape (list command)

```json
{
  "sessions": [
    {
      "id": "b6ac1569-fa25-42ae-b70f-621f392716a9",
      "date": "2026-04-25",
      "repo": "jonkeda/auto-memory",
      "workspace": "e:\\repos\\Private\\auto-memoryNet",
      "summary": "create a new milestone...",
      "turns": 12,
      "tools": 48
    }
  ],
  "source": "vscode-chat",
  "total": 86
}
```

## CLI Dispatcher Changes

Extend `Program.cs` fallback chain:

```
DatabaseNotFoundException
  → VsCodeListCommand (session-state, 44 sessions)
  → if also empty → VscChatListCommand (workspaceStorage, 86 sessions)
```

Or merge both into a unified list (combined, sorted by date desc).

## Scale (user's machine)

- 86 sessions across 14 workspaces
- Largest workspace: 20 sessions (ClayBouwMobile, AiGlasses, unknown)
- Average transcript size: ~1 MB (largest: 11.7 MB for current session)
- Scan is fast (enumerate directories, read first lines of each JSONL for metadata)
