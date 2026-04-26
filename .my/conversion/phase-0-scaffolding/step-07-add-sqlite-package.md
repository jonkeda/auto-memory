# Step 07 — Add Microsoft.Data.Sqlite

## Goal
Single runtime dependency for the Core library.

## Commands
```powershell
dotnet add net/src/AutoMemory.Core/AutoMemory.Core.csproj package Microsoft.Data.Sqlite
```

## Verification
- `dotnet list net/src/AutoMemory.Core/AutoMemory.Core.csproj package` shows `Microsoft.Data.Sqlite`.
- `--include-transitive` listing reveals only `SQLitePCLRaw.bundle_e_sqlite3` family — no MediatR/Serilog/etc.

## Done when
- [ ] Core compiles with `using Microsoft.Data.Sqlite;`.
