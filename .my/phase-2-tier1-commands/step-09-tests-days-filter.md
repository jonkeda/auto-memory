# Step 09 — Tests: days filter

## Goal
Port `tests/test_days_filter.py` to verify date-cutoff math is identical across `list`, `files`, `checkpoints`.

## Cases
- Sessions older than `--days N` excluded.
- Boundary: row exactly `N*86400` seconds old → match Python (inclusive vs exclusive).
- `--days 0` semantics match Python.
- Default `--days` differs across commands (`list`=30, others may differ) — assert per command.

## Done when
- [ ] All cases pass.
- [ ] Boundary behavior documented in code comment if Python is exclusive.
