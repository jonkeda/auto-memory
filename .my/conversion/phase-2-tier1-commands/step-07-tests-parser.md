# Step 07 — Tests: parser

## Goal
Port `tests/test_parser.py` → `AutoMemory.Tests/Cli/ArgParserTests.cs`.

## Cases
- `--limit 5` and `--limit=5` parse equivalently.
- Negative `--limit` → usage error.
- Unknown flag → exit 2.
- Boolean flag `--json` toggles correctly.
- Positional + flag mixing.
- Missing required positional → usage error.
- `-h` / `--help` short-circuits to help text.

## Done when
- [ ] All cases pass.
- [ ] No DB or filesystem dependency in this test class.
