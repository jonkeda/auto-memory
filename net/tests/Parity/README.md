# Parity Test Fixtures

This directory contains the deterministic seeder and fixtures for validating .NET/Python output parity.

## seed.py

Generates a reproducible SQLite database with:
- **25 sessions** across 3 repos (`acme/frontend`, `acme/backend`, `acme/mobile`)
- **200 file rows** distributed across all sessions
- **15 checkpoints** (one per session for first 15 sessions)
- **8 sessions with summaries** (intentional gaps to test freshness/coverage scoring)
- **Fixed random seed** (`RANDOM_SEED = 42`) for reproducibility
- **Deterministic timestamps** anchored at `BASE_DATE = 2024-01-01T00:00:00Z`

### Usage

```bash
python seed.py <output_path.sqlite>
```

### Verification

The seeder outputs SHA-256 hash and validates:
- Exactly 25 sessions (8 with summaries, 17 without)
- Exactly 200 file rows
- Exactly 15 checkpoints
- Database size < 5 MB

Multiple runs produce identical SHA-256 hashes, confirming deterministic output.

### Example Output

```
✓ Created parity fixture: fixture.sqlite
  Sessions: 25 (8 with summaries)
  Turns: 99
  Files: 200
  Checkpoints: 15
  Refs: 20
  Size: 0.06 MB
  SHA-256: bb1e11021f72a5e9d3a9d404aa10bb8299258a61d20c8870c39d3eca89053201
```

## Parity Test Cases

(To be added in subsequent steps)

Each test case file will declare:
- Command-line arguments
- Expected exit code
- Expected stdout (or hash for non-deterministic fields)
- Expected stderr regex
- Telemetry tier
