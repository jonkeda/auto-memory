# Step 10 — DimE2E

## Goal
Port `health/dim_e2e.py` → `Health/DimE2E.cs`.

## Notes
- Python spawns subprocesses to exercise the CLI. In .NET, **do not** spawn — call `Core` APIs directly to avoid re-entry overhead.
- Score must still match Python's output for equivalent operations.

## Done when
- [ ] No `Process.Start` of `session-recall` from inside this dim.
- [ ] Score parity verified.
