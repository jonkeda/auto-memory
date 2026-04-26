# Step 13 — CI parity job

## Goal
Add a `parity` job to GitHub Actions that depends on both `pytest` and `dotnet test`.

## Job sketch
```yaml
parity:
  needs: [pytest, dotnet]
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-python@v5
    - uses: actions/setup-dotnet@v4
      with: { global-json-file: net/global.json }
    - run: pip install -e .
    - run: dotnet publish net/src/AutoMemory.Cli -c Release -r linux-x64
    - run: python net/tests/Parity/seed.py /tmp/fixture.sqlite
    - run: dotnet test net/tests/AutoMemory.Tests --filter Category=Parity
```

## Done when
- [ ] Parity job green on `main`.
- [ ] Parity failures block PR merges.
