# Step 08 — Tests: round-trip

## Goal
Write 100 records from .NET, read back via Python's reader, compare against Python-written records.

## Cases
- 100 random commands across the tier map.
- Every optional field exercised at least 5 times.
- Encoding: UTF-8 with non-ASCII in `query_hash` inputs (hash itself remains ASCII).

## Done when
- [ ] Python reader accepts every .NET record.
- [ ] Diff vs Python-written file empty except for `duration_ms` and `ts`.
