# Step 07 — Util Telemetry (initial)

## Goal
Initial port of `util/telemetry.py` → `AutoMemory.Core/Util/Telemetry.cs`. Phase 5 refines wiring + golden-file parity.

## Source Mapping
- **Python**: `src/session_recall/util/telemetry.py`
- **.NET**: `AutoMemory.Core/Util/Telemetry.cs`

## API
- `Telemetry.Init(string? path)` — sets target path; null/empty disables.
- `Telemetry.QueryHash(string query) -> string` — 8-char SHA-256 of normalized query.
- `Telemetry.Record(...)` — append entry, trim ring buffer.

## Query Hash
- Normalize: lowercase, collapse whitespace runs to single space, trim.
- SHA-256 → first 8 hex chars.
- Deterministic; case- & whitespace-insensitive.

## Entry Schema (JSONL ring-buffered file)
Required: `ts`, `cmd`, `duration_ms`, `busy_hits`, `attempts`, `rows_returned`, `exit_code`, `schema_ok`.
Optional (omit when null): `tier`, `query_hash`, `session_id_prefix`, `window_tier`.

## Storage
- File format: `{ "entries": [ ... ] }`.
- Ring buffer cap: **500** entries.
- Trim from head when overflow.

## Robustness
- All exceptions inside `Record` swallowed (best-effort).
- Auto-create parent directory.
- Timestamp UTC ISO-8601: `yyyy-MM-ddTHH:mm:ssZ`.

## Done when
- [ ] `QueryHash` deterministic + normalization verified.
- [ ] Optional fields omitted (not `null` in JSON).
- [ ] Ring buffer trims at 500.
- [ ] Telemetry never throws to caller.
