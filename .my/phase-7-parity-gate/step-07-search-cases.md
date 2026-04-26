# Step 07 — search cases

## Goal
Parity cases for `search`, including sanitization rejections.

## Cases
- `search hello`.
- `search "hello world" --json`.
- `search "" ` → exit 2 with same stderr.
- `search "needle" --repo <r> --days 30`.

## Done when
- [ ] All cases pass; FTS5 result ordering matches Python.
