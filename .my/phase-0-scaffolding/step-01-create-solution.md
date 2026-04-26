# Step 01 — Create solution

## Goal
Create the empty .NET solution file at the root of `net/`.

## Commands
```powershell
dotnet new sln -n AutoMemory -o net
```

## Verification
- `net/AutoMemory.sln` exists.
- `dotnet sln net/AutoMemory.sln list` returns "No projects found".

## Done when
- [ ] `net/AutoMemory.sln` committed.
