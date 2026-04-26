# Step 14 — v1 tag checklist

## Pre-tag gate
- [ ] All Phase 0–6 steps completed.
- [ ] All parity case files passing.
- [ ] Exit-code matrix passing.
- [ ] Telemetry golden-file diff empty (modulo whitelisted fields).
- [ ] Cold-start + size budgets met (Phase 6 step 10).
- [ ] CI green: pytest + dotnet test + parity on Ubuntu and Windows.
- [ ] `net/README.md` updated with "Parity tested against Python {version}".
- [ ] Release notes drafted with SHA-256 table.

## Tag command
```bash
git tag -a v1.0.0 -m "First feature-equivalent .NET port"
git push origin v1.0.0
```

## Done when
- [ ] All checklist items above checked.
- [ ] Release published with three platform binaries.
