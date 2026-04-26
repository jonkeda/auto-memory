# Step 05 — Publish osx-arm64 (best-effort)

## Commands
```bash
dotnet publish net/src/AutoMemory.Cli -c Release -r osx-arm64 \
  --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Notes
- macOS notarization out of scope for v1.
- Ship unsigned with documented Gatekeeper override.

## Done when
- [ ] Binary runs on Apple Silicon test runner.
- [ ] Smoke test passes.
