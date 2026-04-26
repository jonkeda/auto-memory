# Step 10 — Golden-file parity

## Goal
End-to-end: invoking the .NET CLI vs Python CLI produces telemetry that diffs only on `duration_ms` and `ts`.

## Harness
- Run a fixed sequence of commands against the same fixture DB with both binaries.
- Read the resulting telemetry files.
- Strip `duration_ms` and `ts` fields.
- Diff remaining content.

## Done when
- [ ] Diff is empty.
- [ ] `jq -r '.cmd' merged.jsonl | sort | uniq -c` works on the merged file.
