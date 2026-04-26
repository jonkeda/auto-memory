# Step 09 — Tests: concurrency

## Goal
Verify 8 parallel writers append without corrupting any line / entry.

## Cases
- JSONL form: each line valid JSON after run.
- Object form: ring buffer maintains invariants under contention (no duplicate or lost entries beyond the documented "best-effort" behavior).
- Windows + Linux runs.

## Done when
- [ ] No corrupt lines.
- [ ] No exception surfaced to callers.
