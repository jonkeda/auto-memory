# Step 04 — show command

## Goal
Implement `session-recall show <session_id>`.

## Source Mapping
- **Python**: `src/session_recall/commands/show_session.py`
- **.NET**: `AutoMemory.Cli/Commands/ShowCommand.cs`

## Args
- Positional `session_id` — accepts any prefix length **≥ 4** (verify against Python).
- Flags: `--turns N` (non-negative int), `--full`, `--json`.

## Behavior
- Resolve prefix → unique session id.
  - Ambiguous → exit **2** with same disambiguation hint.
  - Unknown → exit **2** with same "no such session" message.
- Apply ANSI sanitization unless `--full`.
- Limit output to last `--turns` turns.
- Telemetry: tier=3, `session_id_prefix` (first 8 chars).

## Done when
- [ ] Prefix matching parity verified.
- [ ] `--turns` validator rejects negatives identically.
- [ ] ANSI handling parity vs Python on captured byte buffers.
