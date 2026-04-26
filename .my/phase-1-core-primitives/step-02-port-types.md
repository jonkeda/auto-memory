# Step 02 — Port Types

## Goal
Port `types.py` TypedDicts to `AutoMemory.Core/Types.cs` as C# `record` types.

## Source Mapping
- **Python**: `src/session_recall/types.py`
- **.NET**: `AutoMemory.Core/Types.cs`

## Records

- `SessionRecord` — id, repository, branch, summary, created_at, updated_at, turns_count, files_count
- `TurnRecord` — turn_index, user_message, assistant_response, timestamp
- `FileRecord` — file_path, tool_name, turn_index
- `CheckpointRecord` — checkpoint_number, title, overview, created_at
- `HealthDimResult` — name, score, detail
- `TelemetryEntry` — command, timestamp, duration_ms, ok

## Notes
- Use `record` with `init` properties.
- JSON property names must match Python field casing exactly (snake_case) — annotate with `[JsonPropertyName]` or apply a naming policy.
- No validation logic; pure data carriers.

## Done when
- [ ] All records compile.
- [ ] Round-trips through `System.Text.Json` produce snake_case field names matching Python.
