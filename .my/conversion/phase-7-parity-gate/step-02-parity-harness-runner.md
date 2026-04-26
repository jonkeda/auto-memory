# Step 02 — Parity harness runner

## Goal
Runner that executes a case file against both Python and .NET CLIs and diffs the output.

## Path
`net/tests/Parity/Runner.cs` (xUnit `[Theory]`-driven from case files).

## Per-case logic
1. Spawn `python -m session_recall <argv>` capturing stdout/stderr/exitcode.
2. Spawn `./session-recall <argv>` likewise.
3. Apply whitelist redactions (timestamps, durations).
4. Assert byte-identical stdout (or hash equal).
5. Assert stderr matches whitelisted regex.
6. Assert exit codes equal.

## Done when
- [ ] Runner can execute a single case file end-to-end.
- [ ] Failure messages show diff context.
