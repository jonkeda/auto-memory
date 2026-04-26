# Step 03 — Publish linux-x64

## Commands
```bash
dotnet publish net/src/AutoMemory.Cli -c Release -r linux-x64 \
  --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
strip ./bin/Release/net9.0/linux-x64/publish/session-recall
```

## Done when
- [ ] Binary < 25 MB compressed.
- [ ] `./session-recall list --limit 5` cold-start < 50 ms on warm fixture.
