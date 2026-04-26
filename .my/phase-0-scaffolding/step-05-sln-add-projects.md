# Step 05 — Add projects to solution

## Goal
Wire all three projects into `AutoMemory.sln`.

## Commands
```powershell
dotnet sln net/AutoMemory.sln add `
  net/src/AutoMemory.Core/AutoMemory.Core.csproj `
  net/src/AutoMemory.Cli/AutoMemory.Cli.csproj `
  net/tests/AutoMemory.Tests/AutoMemory.Tests.csproj
```

## Done when
- [ ] `dotnet sln net/AutoMemory.sln list` shows three projects.
- [ ] `dotnet build net/AutoMemory.sln` succeeds.
