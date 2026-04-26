# Step 06 — JSON output formatter

## Goal
Single shared JSON formatter ensuring stable field ordering and snake_case naming.

## Notes
- Use `System.Text.Json` with `JsonNamingPolicy.SnakeCaseLower` (.NET 9+).
- Explicit field ordering via record property declaration order.
- No trailing `.0` on integers (matches `json.dumps`).
- UTC ISO-8601 timestamps with `Z` suffix.

## Done when
- [ ] Round-trip test: serialized output diff'd byte-for-byte against Python `json.dumps` of equivalent dict.
- [ ] Field ordering deterministic across runs.
