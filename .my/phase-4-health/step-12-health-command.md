# Step 12 — health command

## Goal
Implement `session-recall health`.

## Source Mapping
- **Python**: `src/session_recall/commands/health.py`
- **.NET**: `AutoMemory.Cli/Commands/HealthCommand.cs`

## Args
- `--json` (and any flag Python exposes — verify).

## Behavior
- Connect read-only.
- Run all dims via `Scoring.Run(conn)`.
- Render text or JSON.
- Telemetry: tier=0.

## Done when
- [ ] Text + JSON parity vs Python.
- [ ] Runtime ≤ 1.2× Python on a 50 MB fixture.
