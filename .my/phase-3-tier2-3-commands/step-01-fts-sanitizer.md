# Step 01 — FTS sanitizer

## Goal
Port the FTS5 query sanitizer used by `search`. Behavior must match `tests/test_search_sanitize.py`.

## Source Mapping
- **Python**: `src/session_recall/commands/search.py` (sanitize helper)
- **.NET**: `AutoMemory.Core/Search/FtsSanitizer.cs`

## Rules
- Strip control characters (`\x00–\x1f` except none — verify Python).
- Escape FTS5 syntax characters Python escapes (typically `"`).
- Reject empty / whitespace-only after sanitization → caller exits **2** with same stderr text.
- Output goes inside a parameterized `MATCH ?` placeholder (no string concat into the SQL itself).

## Done when
- [ ] Each case in `test_search_sanitize.py` has a matching xUnit assertion.
- [ ] Empty/whitespace input rejected with same stderr message.
