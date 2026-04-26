# Step 09 — Smoke build & test

## Goal
Confirm scaffolding works end-to-end before adding code.

## Commands
```powershell
dotnet build net/AutoMemory.sln -c Release
dotnet test  net/AutoMemory.sln -c Release
```

## Acceptance
- Zero warnings, zero errors.
- xUnit placeholder runs and reports 1 passed test.

## Done when
- [ ] Both commands green locally.
