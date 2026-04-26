# Phase 5 — Telemetry parity

**Goal:** make .NET telemetry JSONL byte-identical to Python's so a single analytics pipeline reads both.

## Scope

- `Util/Telemetry.cs` (refined from Phase 1).
- Wire telemetry into every subcommand at the `Program.cs` boundary, mirroring `__main__.py` exactly.

## Field schema (from `__main__.py`)

```
cmd                : string  — subcommand name
duration_ms        : int     — wall time, monotonic
exit_code          : int     — final process exit code
tier               : int|null — TIER_MAP value
query_hash         : string|null — only for `search`
session_id_prefix  : string|null — only for `show`, first 8 chars
window_tier        : int|null — Phase-4-reserved (always null today)
```

Plus whatever timestamp/version fields `util/telemetry.py` emits — verify and replicate.

## Tier map

```csharp
static readonly IReadOnlyDictionary<string, int> TierMap = new Dictionary<string, int>
{
    ["list"] = 1, ["files"] = 1, ["checkpoints"] = 1,
    ["search"] = 2,
    ["show"] = 3,
    ["health"] = 0, ["schema-check"] = 0,
    ["calibrate"] = 0,
};
```

## Query hash

- Read Python implementation in `util/telemetry.py`.
- Reproduce algorithm exactly (likely `hashlib.sha256(query.encode("utf-8")).hexdigest()[:N]`).
- Unit test: 50-row table of `(input, expected_hash)` shared between Python and .NET test suites (drop into `tests/fixtures/query_hashes.csv`).

## JSON serialization

- Use `System.Text.Json` with:
  - `JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.Never }`
  - Property naming policy: snake_case (custom, since BCL has no built-in snake_case prior to .NET 8 — .NET 9 has `JsonNamingPolicy.SnakeCaseLower`).
  - Numbers serialized without trailing `.0` for ints — matches Python `json.dumps`.
- Append with `O_APPEND` semantics: open with `FileMode.Append, FileAccess.Write, FileShare.ReadWrite`.

## Failure handling

- Any exception inside `Record(...)` is swallowed and logged to stderr **only** when `SESSION_RECALL_TELEMETRY_DEBUG=1`.
- Telemetry path directory created if missing (best-effort).

## Tests

- Round-trip: write 100 records from .NET, read back, compare with Python-written records for the same inputs.
- Concurrency: 8 parallel writers append without corrupting any line (each line is valid JSON).
- Disk full / permission denied → no exception thrown to caller.

## Acceptance

- Golden-file equality: `python -m session_recall list ...` and `session-recall list ...` produce telemetry lines that diff only on `duration_ms` and timestamp.
- A unified jq query works against the merged file: `jq -r '.cmd' merged.jsonl | sort | uniq -c`.

## Risks

- `JsonNamingPolicy.SnakeCaseLower` availability — fall back to a custom policy if needed.
- File-locking semantics differ on Windows; use `FileShare.ReadWrite` and document that concurrent writers on Windows may interleave bytes within a line — mitigated by writing one full record per `WriteAllText`-style append with a single syscall.
