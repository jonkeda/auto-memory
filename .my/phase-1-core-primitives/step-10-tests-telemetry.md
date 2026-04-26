# Step 10 — Tests: Telemetry

## Goal
Port `tests/test_telemetry.py` → `AutoMemory.Tests/Util/TelemetryTests.cs`.

## Test Cases

1. **Record_WithTier** — `Record("list", duration:42, tier:1)` → entry has `cmd:"list"`, `tier:1`, no `query_hash`.
2. **QueryHash_Deterministic** — same input → same 8-char hash; `"HELLO  WORLD"` == `"hello world"`.
3. **Record_WithQueryHash** — search records persist `query_hash`.
4. **Record_WithSessionPrefix** — show records persist `session_id_prefix`.
5. **OptionalFields_Omitted** — entry without tier omits the key entirely (not `"tier": null`).
6. **RingBuffer_500** — record 600 entries, file retains 500; oldest 100 dropped.
7. **NoRawQueryStored** — feed sensitive query, verify file text contains neither the query nor key tokens (privacy).

## Fixtures
- Per-test temp telemetry path; reset `Telemetry.Init(null)` after.

## Done when
- [ ] All 7 cases pass.
- [ ] No flakiness on parallel xUnit runs (isolate file paths per test).
