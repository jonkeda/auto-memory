# Step 06 — Wire project references

## Goal
Connect Cli → Core, and Tests → Core.

## Commands
```powershell
dotnet add net/src/AutoMemory.Cli/AutoMemory.Cli.csproj reference net/src/AutoMemory.Core/AutoMemory.Core.csproj
dotnet add net/tests/AutoMemory.Tests/AutoMemory.Tests.csproj reference net/src/AutoMemory.Core/AutoMemory.Core.csproj
```

## Done when
- [ ] `Cli.csproj` and `Tests.csproj` show a `<ProjectReference>` to Core.
- [ ] `dotnet build` resolves cross-project types.
