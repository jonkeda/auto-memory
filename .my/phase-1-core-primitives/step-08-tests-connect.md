# Step 08 — Tests: Connect

## Goal
Port `tests/test_connect.py` → `AutoMemory.Tests/Db/ConnectTests.cs`.

## Test Cases

1. **Connect_Success** — temp DB with simple schema, open, run `SELECT 1;`.
2. **Connect_MissingDb** — bogus path. Asserts exit code **4** and stderr `error: database not found:`.
3. **Connect_Readonly** — open, attempt INSERT, assert `SqliteException`.
4. **Connect_BusyRetry** — second connection holds `BEGIN IMMEDIATE`; assert reader retries with the `[50,150,450]` schedule and exits **3**.

## Fixtures
- `TempDbFixture` (IDisposable) — creates a temp file + minimal schema, deletes on dispose.
- Always close all `SqliteConnection`s before deleting on Windows.

## Done when
- [ ] All 4 cases pass on Windows + Linux.
- [ ] No leftover temp files.
- [ ] No flaky timing dependencies (use a controllable clock/sleep abstraction if needed).
