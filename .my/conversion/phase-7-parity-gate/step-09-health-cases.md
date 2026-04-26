# Step 09 — health cases

## Goal
Parity cases for `health`.

## Cases
- `health` (text).
- `health --json` (byte-identical).

## Notes
- Text output has many fields — whitelist any rendering nondeterminism (e.g. ordering of dimension list) only if necessary.
- JSON case is the canonical gate.

## Done when
- [ ] Both cases pass.
- [ ] Per-dimension scores agree.
