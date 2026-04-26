# Step 01 — Port Config

## Goal
Port `config.py` to `AutoMemory.Core/Config.cs` with exact constant parity and environment override support.

## Source Mapping
- **Python**: `src/session_recall/config.py`
- **.NET**: `AutoMemory.Core/Config.cs`

## Implementation Notes

### Constants
- `DbPath`: from `SESSION_RECALL_DB`, fallback `~/.copilot/session-store.db`
  - `Environment.GetEnvironmentVariable("SESSION_RECALL_DB")`
  - Fallback: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".copilot", "session-store.db")`
- `TelemetryPath`: from `SESSION_RECALL_TELEMETRY`, fallback `~/.copilot/scripts/.session-recall-stats.json`
- `RetryDelaysMs`: `new[] { 50, 150, 450 }`
- `MaxRetries`: `RetryDelaysMs.Length` (3)
- `ExpectedSchemaVersion`: `1`

### Path Resolution
- WSL2 edge case: prefer `HOME` env var if `SpecialFolder.UserProfile` is unset.
- Use `Path.GetFullPath()` to normalize.

## Done when
- [ ] All constants defined as static public.
- [ ] Env overrides verified by unit test.
- [ ] Fallback paths resolve on Windows + Unix.
- [ ] No deps beyond `System.IO`.
