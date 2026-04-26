# Step 13 — Tests: scoring

## Goal
Port `tests/test_health_scoring.py` → `AutoMemory.Tests/Health/ScoringTests.cs`.

## Cases
- Weights sum to 1.0 ± epsilon.
- Aggregate score for fixture matches Python.
- Rounding matches Python (`MidpointRounding.ToEven`).
- Per-dimension scores byte-identical.

## Done when
- [ ] All cases pass on a deterministic seeded DB.
