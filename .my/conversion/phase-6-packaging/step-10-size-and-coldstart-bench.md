# Step 10 — Size + cold-start bench

## Goal
Confirm performance budgets are met on each RID.

## Targets
- Cold start `list --limit 5` against warm fixture: **< 50 ms** on Linux.
- Binary size: **< 25 MB** compressed.

## Tooling
- `hyperfine './session-recall list --limit 5'`.
- `du -h` on published binary.

## Done when
- [ ] Targets met on linux-x64.
- [ ] Numbers documented in release notes.
