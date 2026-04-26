# Step 09 — Tests: schema-check command

## Goal
Port `tests/test_schema_check.py` command-level cases (separate from Phase 1's library tests) → `AutoMemory.Tests/Commands/SchemaCheckCommandTests.cs`.

## Cases
- OK schema: text "schema OK"; exit 0.
- Mismatch: text mismatch summary; exit 5.
- `--json`: full result object with `is_valid`, `problems[]`.

## Done when
- [ ] All cases pass.
- [ ] Output bytes match Python.
