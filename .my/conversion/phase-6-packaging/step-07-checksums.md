# Step 07 — Checksums

## Goal
Compute SHA-256 for each released binary and include in release notes.

## Commands
```bash
sha256sum session-recall-linux-x64 session-recall-win-x64.exe session-recall-osx-arm64 \
  > SHA256SUMS.txt
```

## Done when
- [ ] `SHA256SUMS.txt` attached to release.
- [ ] Release notes embed the table.
