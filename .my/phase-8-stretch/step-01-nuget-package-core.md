# Step 01 — NuGet package: AutoMemory.Core

## Goal
Publish `AutoMemory.Core` to nuget.org so other tools can embed session recall.

## Public API (committed to SemVer)
- `SessionStore`
- `HealthRunner`
- `SchemaChecker`
- `TelemetryWriter`

## Packaging
- `dotnet pack -c Release` with SourceLink + `.snupkg` symbols.
- `<IsPackable>true</IsPackable>` on Core; `false` on Cli/Tests.
- README + license + icon in package.

## Risks
- API lock-in; breaking changes require a major bump.
- Diverging from CLI internals — keep public surface narrow.

## Done when
- [ ] Package published; `dotnet add package AutoMemory.Core` works.
- [ ] SourceLink resolves in a debugger.
