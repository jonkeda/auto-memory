# Phase 6 — Packaging & distribution

**Goal:** ship a single self-contained `session-recall` binary and document install.

## Targets

- `linux-x64` (primary — WSL2 + native Linux).
- `win-x64` (secondary).
- `osx-arm64` (best-effort).

## Publish profile

`net/src/AutoMemory.Cli/AutoMemory.Cli.csproj`:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <AssemblyName>session-recall</AssemblyName>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  <InvariantGlobalization>true</InvariantGlobalization>
  <DebugType>embedded</DebugType>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

Optional: try `<PublishAot>true</PublishAot>` once `Microsoft.Data.Sqlite` AOT compatibility is verified. If AOT trim warnings exceed budget, stay on single-file trimmed.

## Build matrix

GitHub Actions release workflow, triggered on tag `v*`:

- `dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained`
- Same for `win-x64`, `osx-arm64`.
- Strip on Linux (`strip session-recall`).
- Upload artifacts; create GitHub release with binaries attached.
- Compute SHA-256 for each binary and include in release notes.

## Install docs

Update `deploy/install.md` with a ".NET binary" tab:

```bash
curl -L -o /usr/local/bin/session-recall \
  https://github.com/dezgit2025/auto-memory/releases/latest/download/session-recall-linux-x64
chmod +x /usr/local/bin/session-recall
session-recall health
```

`pip install` route remains the default — the .NET binary is for users who want zero Python toolchain.

## Performance budget

- Cold start `session-recall list --limit 5` against a warm fixture DB: **< 50 ms** on Linux.
- Binary size: **< 25 MB** compressed single-file.

## Tests

- Smoke test in CI after publish: run the produced binary against a fixture DB and diff output vs Python.
- Verify `--version` flag (added during this phase) reports the same version string as `pyproject.toml`.

## Acceptance

- Tagged release ships three binaries with checksums.
- `session-recall health` on a fresh Ubuntu container with no .NET runtime installed succeeds.
- Binary size and cold-start budgets met.

## Risks

- Trim warnings from `Microsoft.Data.Sqlite` reflection paths — keep `TrimMode=partial` and add `[DynamicDependency]` annotations as needed.
- AOT + SQLite native interop: verify `e_sqlite3` bundles correctly per RID.
- Antivirus false positives on Windows single-file exe — mitigate by signing if a cert is available, otherwise document.
