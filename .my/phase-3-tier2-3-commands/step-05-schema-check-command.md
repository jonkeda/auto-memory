# Step 05 — schema-check command

## Goal
Implement `session-recall schema-check` (Tier 0).

## Source Mapping
- **Python**: `src/session_recall/commands/schema_check_cmd.py`
- **.NET**: `AutoMemory.Cli/Commands/SchemaCheckCommand.cs`

## Args
- `--json` only.

## Behavior
- Reuses `Db/SchemaCheck.cs` (Phase 1).
- Text mode: one-line OK or MISMATCH summary.
- JSON mode: full result object (`is_valid`, `problems`).
- Mismatch → exit **5**.

## Done when
- [ ] Text + JSON parity vs Python.
- [ ] Mismatch returns exit 5; OK returns 0.
