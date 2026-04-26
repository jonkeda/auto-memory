# Step 07 — Tests: show session

## Goal
Port `tests/test_show_session.py` → `AutoMemory.Tests/Commands/ShowCommandTests.cs`.

## Cases
- Exact id, valid prefix (≥4), ambiguous prefix, unknown id.
- `--turns 0`, `--turns N`, negative `--turns` rejected.
- `--full` retains ANSI.
- `--json` emits expected shape.

## Done when
- [ ] All Python cases ported.
- [ ] Stdout captured as bytes for ANSI assertions (no terminal needed).
