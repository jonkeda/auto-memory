# Step 08 — Author Directory.Build.props + global.json

## Goal
Pin SDK and apply solution-wide compile flags.

## Files

`net/global.json`:
```json
{ "sdk": { "version": "9.0.100", "rollForward": "latestFeature" } }
```

`net/Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <InvariantGlobalization>true</InvariantGlobalization>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

## Done when
- [ ] Both files committed.
- [ ] `dotnet build` still succeeds with zero warnings under `TreatWarningsAsErrors`.
