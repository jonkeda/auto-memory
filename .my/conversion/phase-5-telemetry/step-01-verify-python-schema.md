# Step 01 — Verify Python schema

## Goal
Lock the canonical telemetry schema before refining the .NET writer.

## Actions
- Read `src/session_recall/util/telemetry.py` and enumerate every field actually written.
- Cross-check `__main__.py` for the `Telemetry.record(...)` call sites (which fields are passed for each command).
- Confirmed required fields: `ts`, `cmd`, `duration_ms`, `busy_hits`, `attempts`, `rows_returned`, `exit_code`, `schema_ok`.
- Confirmed optional (omit when null): `tier`, `query_hash`, `session_id_prefix`, `window_tier`.

## Done when
- [ ] A canonical schema doc lives at `net/docs/telemetry-schema.md` (or in this step file) and matches Python output byte-for-byte for sample inputs.
