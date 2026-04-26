# Step 01 — Fixture DB seeder

## Goal
Deterministic Python script that builds the parity-test SQLite fixture.

## Path
`net/tests/Parity/seed.py`

## Contents
- 25 sessions across 3 repos.
- 200 file rows.
- 15 checkpoints.
- 8 sessions with summaries; intentional gaps to exercise freshness/coverage scoring.
- Fixed seed for `random.seed(...)`; deterministic timestamps anchored at a constant `BASE_DATE`.

## Done when
- [ ] `python seed.py path/to/fixture.sqlite` is reproducible (same SHA-256 for the file across runs).
- [ ] Fixture < 5 MB.
