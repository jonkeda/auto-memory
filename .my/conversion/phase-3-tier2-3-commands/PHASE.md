# Phase 3 — Tier-2 / Tier-3 commands

**Goal:** ship `search`, `show`, and `schema-check`.

## search (Tier 2)

- Positional `query`; flags `--repo`, `--limit`, `--days`, `--json`.
- Sanitize FTS input identically to Python (`test_search_sanitize.py` is the spec):
  - Strip control characters.
  - Escape FTS5 syntax characters that Python escapes.
  - Reject empty/whitespace-only after sanitization → exit 2 with the same stderr.
- Use parameterized SQL **plus** sanitized MATCH expression. No string concat into SQL beyond the MATCH clause.
- Output ordering and snippet rendering must match Python.

## show (Tier 3)

- Positional `session_id` (any prefix length ≥ 4 — verify against Python).
- Flags: `--turns N` (non-negative int; argparse type validator parity), `--full`, `--json`.
- Terminal output sanitization parity (`test_sanitize_terminal.py`):
  - Strip ANSI escape sequences except where `--full` preserves them.
  - Replace control bytes per Python's logic.
- Ambiguous prefix → exit 2 with the same disambiguation hint Python prints.
- Unknown session → exit 2 with same message.

## schema-check (Tier 0)

- `--json` flag only.
- Reuses `AutoMemory.Core/Db/SchemaCheck.cs` from Phase 1.
- Text mode prints a one-line OK/MISMATCH summary; JSON mode emits the full result object.

## Tests

- Port: `test_search_sanitize.py`, `test_show_session.py`, `test_sanitize_terminal.py`, `test_schema_check.py`.
- Add fuzz-style unit tests for the FTS sanitizer using a small character class table (control chars, quotes, parentheses, asterisks).

## Acceptance

- Golden-file diff vs Python passes for `search`, `show`, `schema-check` across fixture DB.
- Exit codes: 0 success, 2 bad input/no match, 3/4/5 from DB layer.
- ANSI handling verified by capturing stdout to a buffer and asserting on bytes (no terminal required).

## Risks

- FTS5 dialect differences between SQLite versions bundled by `Microsoft.Data.Sqlite` vs the system SQLite Python uses → pin to a known-good provider and document in `net/README.md`.
- ANSI regex differences (Python's vs .NET's) — port the regex literally and add a unit test per ANSI category.
