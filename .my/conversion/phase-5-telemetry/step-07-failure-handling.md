# Step 07 — Failure handling

## Goal
Telemetry must never crash the CLI; surface errors only when explicitly debugging.

## Rules
- Wrap entire `Record(...)` body in try/catch.
- On exception: if `SESSION_RECALL_TELEMETRY_DEBUG=1`, write a one-liner to stderr; else swallow.
- Auto-create parent directory (best-effort).
- File-share: `FileShare.ReadWrite` to allow concurrent readers.

## Done when
- [ ] Disk-full simulation does not surface an exception.
- [ ] Permission-denied test passes (Linux fixture: read-only telemetry dir).
- [ ] Debug env var produces stderr line when set.
