# Step 06 — Tests: search sanitize

## Goal
Port `tests/test_search_sanitize.py` → `AutoMemory.Tests/Search/FtsSanitizerTests.cs`.

## Cases
- Each character-class table entry from Python becomes a `[Theory]` row.
- Empty / whitespace-only inputs → expects rejection.
- Unicode preservation.
- FTS5 syntax escape verification.

## Done when
- [ ] All Python cases mirrored.
- [ ] Property-style fuzz over `[\x00-\x1f]`, quotes, parentheses, asterisks.
