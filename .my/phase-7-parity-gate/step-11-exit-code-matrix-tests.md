# Step 11 — Exit code matrix tests

## Goal
Cross-cutting check that exit codes match Python for the full matrix.

## Matrix
| Code | Trigger |
| --- | --- |
| 0 | success |
| 1 | generic error |
| 2 | bad args / unknown subcommand |
| 3 | DB locked (writer holds `BEGIN IMMEDIATE`) |
| 4 | DB not found (path doesn't exist) |
| 5 | schema mismatch |

## Implementation
- One xUnit `[Theory]` row per code.
- Each row spawns both CLIs against a curated fixture and asserts exit codes equal.

## Done when
- [ ] All 6 codes verified.
