# Step 09 — Tests: SchemaCheck

## Goal
Port `tests/test_schema_check.py` → `AutoMemory.Tests/Db/SchemaCheckTests.cs`.

## Test Cases

1. **CorrectSchema_NoProblems** — full expected schema → `IsValid == true`.
2. **MissingColumn** — drop `summary` from `sessions` → exactly one problem mentioning `summary`.
3. **MissingTable** — drop `checkpoints` → problem text `MISSING TABLE: checkpoints`.
4. **ExtraColumns_Ok** — extra columns do not flag.

## Helper
`CreateTempDb(IDictionary<string, IEnumerable<string>> tables)` builds a SQLite file matching the supplied shape.

## Done when
- [ ] All 4 cases pass.
- [ ] Error strings match Python byte-for-byte.
- [ ] Cleanup leaves no artifacts.
