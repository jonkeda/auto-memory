# Step 02 — Create AutoMemory.Core classlib

## Goal
Create the core library project that will host db, health, util.

## Commands
```powershell
dotnet new classlib -o net/src/AutoMemory.Core
```

## Edits
- Remove the auto-generated `Class1.cs`.
- Csproj will be tightened by `Directory.Build.props` (Step 08).

## Done when
- [ ] `net/src/AutoMemory.Core/AutoMemory.Core.csproj` exists.
- [ ] No placeholder `Class1.cs`.
