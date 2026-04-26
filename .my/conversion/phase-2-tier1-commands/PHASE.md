# Phase 2 — Tier-1 commands

**Goal:** ship `list`, `files`, `checkpoints` with full flag and output parity.

## Scope

Three "cheap scan" commands sharing a common shape:

- `list` — recent sessions
- `files` — recently touched files
- `checkpoints` — recent checkpoints

Common flags: `--repo`, `--limit`, `--days`, `--json`.

## Deliverables

- `AutoMemory.Cli/Commands/ListCommand.cs`
- `AutoMemory.Cli/Commands/FilesCommand.cs`
- `AutoMemory.Cli/Commands/CheckpointsCommand.cs`
- `AutoMemory.Cli/ArgParser.cs` — hand-rolled parser to keep the dep graph clean. Supports:
  - `--name=value` and `--name value`
  - `--flag` (boolean)
  - positional args
  - `-h` / `--help` per subcommand
- `AutoMemory.Core/Queries/SessionQueries.cs`, `FileQueries.cs`, `CheckpointQueries.cs` — parameterized SQL only.

## Argparse parity notes

- `--limit` parses as int; negative → error message identical to Python where feasible.
- `--days` defaults differ slightly across commands in Python (`list` uses 30, others none) — preserve exactly.
- Unknown flags → exit 2 with `usage:` line.

## Output parity

- Default text formatter must match Python column order, separator, truncation, and timestamp formatting (UTC ISO-8601 with `Z` suffix; verify against `util/format_output.py`).
- `--json` produces an array of objects with the same field names and ordering Python uses.
- Empty result: print nothing to stdout (or the same banner Python prints — verify), exit 0.

## Tests

- Port `test_list_sessions.py`, `test_days_filter.py`, `test_parser.py`.
- Snapshot tests for `--json` output across each command using a fixture DB.
- Argument parser unit tests independent of DB.

## Acceptance

- For a fixture DB, the following all hold:
  - `session-recall list --json` byte-identical to Python.
  - `session-recall files --repo <r> --days 7 --limit 5` byte-identical text.
  - `session-recall checkpoints --json` byte-identical to Python.
- Exit codes: 0 on success, 2 on bad args, 3/4 on DB issues (delegated to Phase 1 layer).

## Risks

- Hand-rolled parser drift vs `argparse` — accept minor stderr wording differences if they don't affect machine-readable output. Document in `net/README.md`.
- Date math edge cases (`--days`): use `DateTimeOffset.UtcNow` and align rounding with Python (`datetime.now(timezone.utc)`).
