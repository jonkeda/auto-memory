# Step 03 — Telemetry writer refine

## Goal
Refine the Phase-1 stub writer to fully match Python: ring buffer, all required + optional fields, append-safe IO.

## Actions
- Switch storage to single JSON object `{ "entries": [...] }` if Python uses that — else keep JSONL. **Verify** in `util/telemetry.py`.
- Implement ring trimming (cap **500**).
- Use `FileMode.Append` style writes for JSONL; full read-modify-write for object form.

## Done when
- [ ] Output binary file matches Python's after the same sequence of `Record` calls.
- [ ] Trimming works at the 500 boundary.
