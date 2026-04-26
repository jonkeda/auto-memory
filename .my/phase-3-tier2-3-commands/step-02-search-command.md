# Step 02 — search command

## Goal
Implement `session-recall search`.

## Source Mapping
- **Python**: `src/session_recall/commands/search.py`
- **.NET**: `AutoMemory.Cli/Commands/SearchCommand.cs`

## Args
- Positional `query`.
- Flags: `--repo`, `--limit`, `--days`, `--json`.

## Behavior
- Sanitize via Step 01.
- Run parameterized FTS5 `MATCH ?` query.
- Render snippet identical to Python (length, ellipsis, highlight markers if any).
- Telemetry: record tier=2 + `query_hash` of normalized input.

## Done when
- [ ] Output parity (text + json) verified on fixture DB.
- [ ] Sanitization rejection path returns exit 2 with same stderr.
