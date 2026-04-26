# Step 04 — DimFreshness

## Goal
Port `health/dim_freshness.py` → `Health/DimFreshness.cs`.

## Notes
- Time-since-last-session math; align with Python `datetime.now(timezone.utc)`.
- Bucket thresholds verbatim.

## Done when
- [ ] Boundary cases (0, threshold-1, threshold, threshold+1 seconds) match Python.
