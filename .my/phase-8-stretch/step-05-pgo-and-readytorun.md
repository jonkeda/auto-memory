# Step 05 — PGO and ReadyToRun

## Goal
Once AOT decision is settled (Phase 6 Step 09), explore Dynamic PGO and ReadyToRun for further cold-start gains.

## Target
- Additional ≥ 20% reduction in `list --limit 5` cold-start vs the chosen Phase-6 baseline.

## Actions
- Enable `<TieredPGO>true</TieredPGO>` and `<PublishReadyToRun>true</PublishReadyToRun>` (where compatible with chosen publish profile).
- Re-bench with `hyperfine`.

## Risks
- May conflict with AOT; only relevant if Phase 6 chose single-file trimmed.
- Platform variance.

## Done when
- [ ] Bench numbers documented and either adopted by default or rationale captured.
