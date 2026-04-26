# Step 08 — show cases

## Goal
Parity cases for `show`.

## Cases
- `show <full_id>`.
- `show <prefix>` (≥ 4 chars).
- `show <ambiguous_prefix>` → exit 2 same hint.
- `show <unknown>` → exit 2 same message.
- `show <id> --turns 3`.
- `show <id> --turns -1` → exit 2 same usage.
- `show <id> --full`.
- `show <id> --json`.

## Done when
- [ ] All cases pass; ANSI handling parity verified on byte buffers.
