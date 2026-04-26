# Step 04 — files command

## Goal
Implement `session-recall files`.

## Source Mapping
- **Python**: `src/session_recall/commands/files.py`
- **.NET**: `AutoMemory.Cli/Commands/FilesCommand.cs`

## Flags
- `--repo`, `--limit`, `--days` (no default per Python — verify!), `--json`.

## Behavior
- Run `FileQueries.SelectRecentFiles`.
- Group/render per Python's column ordering.
- Empty results behavior identical to Python.

## Done when
- [ ] `--json` parity test passes.
- [ ] Text output column widths match Python.
- [ ] `--days` default matches Python (no implicit 30).
