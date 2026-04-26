# Step 08 — Tests: list_sessions

## Goal
Port `tests/test_list_sessions.py` → `AutoMemory.Tests/Commands/ListCommandTests.cs`.

## Setup
- Seed in-memory or temp DB with 3 repos × 5 sessions, varied dates.
- Run `ListCommand` via internal entrypoint capturing stdout/stderr.

## Cases
- Default invocation returns most-recent first.
- `--repo X` filters.
- `--limit N` truncates.
- `--json` produces well-formed array.
- Empty DB → no rows / banner identical to Python.

## Done when
- [ ] All cases pass.
- [ ] Snapshot of `--json` matches a fixture file.
