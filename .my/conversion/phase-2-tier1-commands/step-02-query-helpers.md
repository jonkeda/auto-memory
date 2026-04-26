# Step 02 — Query helpers

## Goal
Centralize parameterized SQL for tier-1 commands in `AutoMemory.Core/Queries/`.

## Files
- `SessionQueries.cs` — `SelectRecentSessions(repo, limit, days)`.
- `FileQueries.cs` — `SelectRecentFiles(repo, limit, days)`.
- `CheckpointQueries.cs` — `SelectRecentCheckpoints(repo, limit, days)`.

## Rules
- All SQL strings are constants; values bound via `SqliteParameter`.
- No `string.Format` / interpolation into SQL.
- Date math: compute cutoff `DateTimeOffset.UtcNow.AddDays(-days)` and bind as ISO-8601 string (matches Python's `datetime.now(timezone.utc)`).

## Done when
- [ ] All three queries unit-tested against an in-memory DB.
- [ ] Roslyn / code review confirms no string-concat into SQL.
