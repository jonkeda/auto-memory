# Step 11 — CI smoke test

## Goal
After publish, run the produced binary against a fixture DB and diff vs Python.

## Pipeline step
1. `dotnet publish` on the runner.
2. Run `./session-recall list --limit 5 --json` against `tests/fixtures/session-store.db`.
3. Run `python -m session_recall list --limit 5 --json` on the same fixture.
4. Diff stdout. Fail on diff.

## Done when
- [ ] Smoke job is green on Ubuntu in the release workflow.
- [ ] Diff failures block the release tag.
