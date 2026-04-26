# Step 10 — Add CI dotnet job

## Goal
Extend `.github/workflows/test.yml` (or add a sibling workflow) with a `dotnet` job that builds and tests on Ubuntu and Windows.

## Workflow snippet
```yaml
dotnet:
  strategy:
    matrix:
      os: [ubuntu-latest, windows-latest]
  runs-on: ${{ matrix.os }}
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        global-json-file: net/global.json
    - run: dotnet build net/AutoMemory.sln -c Release
    - run: dotnet test  net/AutoMemory.sln -c Release --no-build
```

## Done when
- [ ] CI run is green on both OSes.
- [ ] Existing pytest job still runs unchanged.
