# Step 03 — DB Connect

## Goal
Port `db/connect.py` to `AutoMemory.Core/Db/Connect.cs` with exact retry, exit code, and PRAGMA behavior.

## Source Mapping
- **Python**: `src/session_recall/db/connect.py`
- **.NET**: `AutoMemory.Core/Db/Connect.cs`

## Implementation Notes

### Connection
- `Microsoft.Data.Sqlite`, conn string `Data Source={path};Mode=ReadOnly`.
- After open: `PRAGMA busy_timeout = 500;`, `PRAGMA query_only = ON;`.

### Missing DB
- `File.Exists` check before opening.
- Stderr: `error: database not found: {path}`. Exit **4**.

### Busy / Locked Retry
- Delays `[50, 150, 450]` ms.
- On `SqliteException` whose message contains `"locked"` or `"busy"` (Ordinal IgnoreCase), sleep `delay * Random(0.8, 1.2)` ms and retry.
- Non-busy exceptions rethrow.
- After exhausting retries: stderr `error: database is locked — another session-recall process may be running`. Exit **3**.

### Robustness
- Guard with both `SqliteErrorCode == 5` AND substring (per PHASE.md risk).
- Force `CultureInfo.InvariantCulture` for any string ops.

## Done when
- [ ] Missing DB → exit 4.
- [ ] Locked DB → retries, exit 3.
- [ ] Read-only enforced (writes throw).
- [ ] PRAGMAs verified post-open.
