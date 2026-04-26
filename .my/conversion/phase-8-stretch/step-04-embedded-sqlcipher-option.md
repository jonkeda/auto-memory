# Step 04 — Embedded SQLCipher option

## Goal
Opt-in build with `SQLitePCLRaw.bundle_e_sqlcipher` for users wanting encrypted session stores.

## Mechanism
- MSBuild property `UseEncryptedSqlite=true` swaps the bundled provider.
- Default build remains `bundle_e_sqlite3` (no behavior change).

## Risks
- Binary bloat / dual-build maintenance.
- Key-management UX out of scope (document only).

## Done when
- [ ] Both default and encrypted builds publish + smoke-test.
- [ ] Documentation covers password-supply mechanism (env var).
