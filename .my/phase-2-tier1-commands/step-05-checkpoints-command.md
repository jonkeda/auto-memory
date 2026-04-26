# Step 05 — checkpoints command

## Goal
Implement `session-recall checkpoints`.

## Source Mapping
- **Python**: `src/session_recall/commands/checkpoints.py`
- **.NET**: `AutoMemory.Cli/Commands/CheckpointsCommand.cs`

## Flags
Same set as `files`: `--repo`, `--limit`, `--days`, `--json`.

## Behavior
- Run `CheckpointQueries.SelectRecentCheckpoints`.
- Render checkpoint number + title + overview snippet identically to Python.

## Done when
- [ ] `--json` parity verified.
- [ ] Text output matches Python.
- [ ] `--days` default matches Python.
