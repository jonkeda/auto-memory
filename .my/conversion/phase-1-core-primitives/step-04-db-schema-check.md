# Step 04 — DB Schema Check

## Goal
Port `db/schema_check.py` to `AutoMemory.Core/Db/SchemaCheck.cs`.

## Source Mapping
- **Python**: `src/session_recall/db/schema_check.py`
- **.NET**: `AutoMemory.Core/Db/SchemaCheck.cs`

## Expected Schema

| Table | Columns |
| --- | --- |
| `sessions` | id, repository, branch, summary, created_at, updated_at |
| `turns` | session_id, turn_index, user_message, assistant_response, timestamp |
| `session_files` | session_id, file_path, tool_name, turn_index, first_seen_at |
| `session_refs` | session_id, ref_type, ref_value, turn_index, created_at |
| `checkpoints` | session_id, checkpoint_number, title, overview, created_at |

## Logic
- For each expected table → `PRAGMA table_info({table})`.
- Missing table → problem `MISSING TABLE: {table}`.
- Missing columns → problem `{table}: missing columns {set}`.
- Extra columns are OK (do not flag).
- Column compare: lowercase normalize.

## Output Type
```csharp
public sealed record SchemaCheckResult(bool IsValid, IReadOnlyList<string> Problems);
```

## Exit Code
- Mismatch → exit **5** (raised at command boundary, not in `SchemaCheck.cs`).
- Problem-message text must match Python exactly for golden-file diff.

## Done when
- [ ] Correct schema → empty problems.
- [ ] Missing table/column detected with exact message.
- [ ] Extra columns ignored.
- [ ] Result serializes cleanly to JSON.
