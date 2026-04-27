# RCA-001 — Dashboard sections stuck loading / "database not found"

**Date:** 2026-04-26  
**Severity:** P2 — extension unusable on first install  
**Status:** Fixed

---

## Symptom

After installing the VSIX and opening the Auto Memory dashboard, the **HEALTH** and **RECENT SESSIONS** sections displayed an error:

```
Command failed: …\bin\win32-x64\session-recall.exe health --json
error: database not found: C:\Users\<user>\.copilot\session-store.db
error: database not found: C:\Users\<user>\.copilot\session-store.db
```

The error string appeared doubled because `runner.ts` concatenates `err.message` and `stderr`, and the CLI prints the same message to both.

---

## Root Cause

When no session database exists yet (`~/.copilot/session-store.db` is absent), the CLI throws `DatabaseNotFoundException`, which `Program.cs` catches and converts to **exit code 4** after printing the error to stderr.

`runner.ts` rejects any non-zero exit, so the `postHealth` / `postSessions` promises rejected.  
Before the previous fix these rejections were silently swallowed (`void postHealth(p)`).  
After the previous fix they surfaced correctly as error text — but the real fix must be further upstream: the CLI should not treat a missing database as a fatal error when JSON output is requested.

### Contributing factors

1. **CLI design**: `DatabaseNotFoundException` always causes exit code 4 regardless of `--json` mode. A brand-new user with no database sees a fatal error instead of empty data.
2. **`runner.ts` doubled error text**: `err.message` already contains the stderr string when `execFile` invokes the callback with a non-zero exit; concatenating `stderr` again duplicates it.
3. **`BinaryManager.resolve()` (previous bug, already fixed)**: returned the bundled binary path without checking that the file existed on disk.

---

## Fix

### 1. `HealthCommand.cs` — return empty JSON on missing DB in `--json` mode

When `--json` is passed and `DatabaseNotFoundException` is thrown, emit a minimal valid health JSON and exit 0 instead of propagating the exception.

### 2. `ListCommand.cs` — return empty JSON on missing DB in `--json` mode

Same pattern: emit `{"sessions": [], "warning": "no database found"}` and exit 0.

### 3. `runner.ts` — deduplicate error text

Don't append `stderr` when it is already included in `err.message`.

---

## Verification

After the fix:

- `session-recall health --json` on a machine with no DB → exits 0, prints `{"overall_score": null, "dimensions": [], "warning": "no database found"}`
- `session-recall list --json --limit 10` on a machine with no DB → exits 0, prints `{"sessions": [], "warning": "no database found"}`
- Dashboard HEALTH section shows a "no database found" notice, not a spinner
- Dashboard RECENT SESSIONS section shows empty list, not a spinner
