# Step 03 — list command

## Goal
Implement `session-recall list` in `AutoMemory.Cli/Commands/ListCommand.cs`.

## Source Mapping
- **Python**: `src/session_recall/commands/list_sessions.py`
- **.NET**: `AutoMemory.Cli/Commands/ListCommand.cs`

## Flags
- `--repo` (string, optional; defaults to detect via `DetectRepo`).
- `--limit` (int, default per Python).
- `--days` (int, default **30**).
- `--json` (bool).

## Behavior
- Connect read-only via Phase 1 layer.
- Run `SessionQueries.SelectRecentSessions`.
- Render via `FormatOutput.Output(rows, jsonMode)`.
- Empty result: same banner/blank as Python.
- Exit codes: 0 success; 2 bad args; 3/4 from db layer.

## Done when
- [ ] Text output byte-equal to Python on fixture DB.
- [ ] `--json` byte-equal.
- [ ] Default `--days 30` confirmed by test.
