# Step 02 — DimSchema

## Goal
Port `health/dim_schema.py` → `Health/DimSchema.cs`.

## Notes
- Wraps `Db/SchemaCheck.cs` result.
- Score: 1.0 if valid, 0.0 if mismatch.
- Detail: list of problems.
- Weight: copy from Python verbatim.

## Done when
- [ ] Score & detail match Python on fixture (valid + mismatch).
