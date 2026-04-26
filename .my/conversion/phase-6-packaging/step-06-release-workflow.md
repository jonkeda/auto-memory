# Step 06 — Release workflow

## Goal
GitHub Actions workflow triggered on tag `v*`.

## Jobs (one per RID)
- Checkout, setup-dotnet (matching `global.json`).
- `dotnet publish` for the RID.
- Strip on Linux.
- Upload artifact.
- Create release; attach `session-recall-<rid>` binaries.

## Done when
- [ ] Tagging `v0.4.2` produces a release with three binaries.
- [ ] Workflow run is reproducible (same SHA → same hash, modulo embedded timestamps).
