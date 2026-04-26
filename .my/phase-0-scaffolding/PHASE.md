# Phase 0 — Scaffolding

**Goal:** stand up the empty .NET solution under `net/` so subsequent phases have a place to land code.

## Deliverables

- `net/AutoMemory.sln`
- `net/global.json` pinning .NET 9 SDK.
- `net/Directory.Build.props` with:
  - `<Nullable>enable</Nullable>`
  - `<ImplicitUsings>enable</ImplicitUsings>`
  - `<LangVersion>latest</LangVersion>`
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - `<InvariantGlobalization>true</InvariantGlobalization>`
- Projects:
  - `net/src/AutoMemory.Core/AutoMemory.Core.csproj` (classlib)
  - `net/src/AutoMemory.Cli/AutoMemory.Cli.csproj` (console, `<AssemblyName>session-recall</AssemblyName>`)
  - `net/tests/AutoMemory.Tests/AutoMemory.Tests.csproj` (xUnit)
- Single dependency on `Microsoft.Data.Sqlite` in `AutoMemory.Core`.
- `.editorconfig` aligned with C# defaults (4-space indent, file-scoped namespaces).
- CI: extend `.github/workflows/test.yml` with a `dotnet` job (`ubuntu-latest`, `windows-latest`) running `dotnet build` + `dotnet test`.

## Steps

1. `dotnet new sln -n AutoMemory -o net`.
2. `dotnet new classlib -o net/src/AutoMemory.Core`.
3. `dotnet new console -o net/src/AutoMemory.Cli`.
4. `dotnet new xunit -o net/tests/AutoMemory.Tests`.
5. `dotnet sln net/AutoMemory.sln add` for all three projects.
6. Add project reference: `AutoMemory.Cli` → `AutoMemory.Core`; `AutoMemory.Tests` → `AutoMemory.Core`.
7. Add `Microsoft.Data.Sqlite` to `AutoMemory.Core`.
8. Author `Directory.Build.props` and `global.json`.
9. Smoke `dotnet build` and `dotnet test` locally.
10. Add CI job.

## Acceptance

- `dotnet build net/AutoMemory.sln -c Release` succeeds with zero warnings.
- `dotnet test net/AutoMemory.sln` runs the xUnit placeholder and passes.
- CI green on Ubuntu and Windows.
- No NuGet package other than `Microsoft.Data.Sqlite` (and xUnit test deps) appears in `dotnet list package --include-transitive` for `AutoMemory.Core`.

## Out of scope

- Any actual port of Python logic.
- AOT publish (deferred to Phase 6).
- Releasing artifacts.

## Risks

- SDK version drift on contributor machines → mitigated by `global.json`.
- `TreatWarningsAsErrors` blocking analyzer additions later → revisit per-project if it bites.
