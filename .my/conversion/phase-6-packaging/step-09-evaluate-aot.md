# Step 09 — Evaluate AOT

## Goal
Try `<PublishAot>true</PublishAot>` and decide whether to ship AOT or stay on single-file trimmed.

## Actions
- Add `[DynamicDependency]` annotations where reflection paths are needed by `Microsoft.Data.Sqlite`.
- Verify `e_sqlite3` native bundle resolves on each RID.
- Measure cold-start delta vs single-file.

## Decision criteria
- Trim/AOT warnings ≤ 5 and all reviewed.
- ≥ 30% cold-start improvement → adopt AOT.
- Otherwise stay on single-file.

## Done when
- [ ] Decision documented in `net/README.md`.
