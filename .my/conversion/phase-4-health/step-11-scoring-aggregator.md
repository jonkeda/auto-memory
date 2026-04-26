# Step 11 — Scoring aggregator

## Goal
Port `health/scoring.py` → `Health/Scoring.cs`.

## Notes
- Iterate registered `IHealthDimension` instances in fixed order.
- Weighted sum using `decimal` for precision; convert to `double` for output.
- `Math.Round(x, 2, MidpointRounding.ToEven)` to match Python `round(x, 2)`.
- JSON shape: top-level fields + nested `dimensions` array. Field order matches Python.

## Done when
- [ ] Output JSON byte-equal to Python on fixture DB.
- [ ] Total score within ±1e-9 of Python's value before rounding.
