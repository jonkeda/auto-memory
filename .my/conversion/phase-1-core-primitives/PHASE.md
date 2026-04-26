# Phase 1 — Core primitives

**Goal:** port `config.py`, `db/connect.py`, `db/schema_check.py`, and `util/` with behavior parity and a passing test suite.

## Source → target mapping

| Python | .NET |
| --- | --- |
| `src/session_recall/config.py` | `AutoMemory.Core/Config.cs` |
| `src/session_recall/types.py` | `AutoMemory.Core/Types.cs` |
| `src/session_recall/db/connect.py` | `AutoMemory.Core/Db/Connect.cs` |
| `src/session_recall/db/schema_check.py` | `AutoMemory.Core/Db/SchemaCheck.cs` |
| `src/session_recall/util/detect_repo.py` | `AutoMemory.Core/Util/DetectRepo.cs` |
| `src/session_recall/util/format_output.py` | `AutoMemory.Core/Util/FormatOutput.cs` |
| `src/session_recall/util/telemetry.py` | `AutoMemory.Core/Util/Telemetry.cs` |
| `tests/test_connect.py` | `AutoMemory.Tests/Db/ConnectTests.cs` |
| `tests/test_schema_check.py` | `AutoMemory.Tests/Db/SchemaCheckTests.cs` |
| `tests/test_telemetry.py` | `AutoMemory.Tests/Util/TelemetryTests.cs` |

## Config parity

- `SESSION_RECALL_DB` env override; default `~/.copilot/session-store.db` resolved via `Environment.GetFolderPath(SpecialFolder.UserProfile)`.
- `SESSION_RECALL_TELEMETRY` env override; default `~/.copilot/scripts/.session-recall-stats.json`.
- `RetryDelaysMs = [50, 150, 450]`.
- `MaxRetries = RetryDelaysMs.Length`.
- `ExpectedSchemaVersion = 1`.

## Connect parity

- Open via connection string `Data Source={path};Mode=ReadOnly` (`Microsoft.Data.Sqlite`).
- If file does not exist → write `error: database not found: {path}` to stderr, exit code **4**.
- Apply `PRAGMA busy_timeout = 500;` and `PRAGMA query_only = ON;` immediately after open.
- On `SqliteException` whose message contains `locked` or `busy`: sleep `delay * Random(0.8, 1.2)` ms (delay ∈ `[50,150,450]`), retry.
- On non-busy `SqliteException`: rethrow.
- After exhausting retries: stderr `error: database is locked — another session-recall process may be running`, exit code **3**.
- Use `CultureInfo.InvariantCulture` and `StringComparison.OrdinalIgnoreCase` for the locked/busy substring check.

## Schema check parity

- Validate presence of expected tables/columns and `user_version` (or whatever the Python file checks).
- On mismatch: stderr message identical to Python, exit code **5**.
- Return a typed `SchemaCheckResult` so the `schema-check` command (Phase 3) can render JSON.

## Telemetry parity

- JSONL append, one record per invocation.
- Fields: `cmd`, `duration_ms`, `exit_code`, `tier`, `query_hash`, `session_id_prefix`, `window_tier`, plus any timestamp the Python writer emits.
- `query_hash`: replicate Python's hash exactly (verify algorithm in `util/telemetry.py` — likely SHA-256 truncated to N hex chars).
- Best-effort writer: never throw out of `Record(...)`. Telemetry failures must not affect the user.
- Create parent directory if missing.

## Tests

- xUnit + temp-file fixtures (`Path.GetTempFileName()` for DB paths, dispose with `IAsyncLifetime`).
- Cover: missing DB, locked DB (simulate by opening a writer with `BEGIN IMMEDIATE`), schema mismatch, telemetry round-trip, query-hash determinism.
- Add a small `TestDb` helper that builds an in-memory schema matching the real `session-store.db`.

## Acceptance

- All ported tests pass on Ubuntu + Windows CI.
- Behavior table for exit codes (3, 4, 5) matches Python on a curated fixture set.
- Telemetry JSONL produced by .NET is byte-identical to Python's for the same inputs (golden-file test).

## Risks

- `Microsoft.Data.Sqlite` does not surface `SQLITE_BUSY` as a discrete code reliably across versions — guard by both `SqliteErrorCode == 5` *and* message substring.
- Path resolution differs under WSL2 if `HOME` is unset — fall back to `Environment.GetEnvironmentVariable("HOME")` before `SpecialFolder.UserProfile`.
