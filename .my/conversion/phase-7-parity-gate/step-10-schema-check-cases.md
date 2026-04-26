# Step 10 — schema-check cases

## Goal
Parity cases for `schema-check`.

## Cases
- OK fixture: `schema-check`, `schema-check --json`.
- Mismatch fixture (drop a column): `schema-check` → exit 5; `--json` shape parity.

## Done when
- [ ] Both fixtures and both modes parity-pass.
