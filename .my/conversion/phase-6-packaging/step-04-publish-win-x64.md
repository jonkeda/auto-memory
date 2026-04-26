# Step 04 — Publish win-x64

## Commands
```powershell
dotnet publish net/src/AutoMemory.Cli -c Release -r win-x64 `
  --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Notes
- AV false-positive risk on unsigned single-file exe — document mitigation; sign if a cert is available.

## Done when
- [ ] `session-recall.exe` runs on a clean Windows VM with no .NET runtime.
- [ ] Smoke test passes against fixture DB.
