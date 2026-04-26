# AutoMemory .NET Port

This directory contains the .NET port of the `session-recall` CLI tool.

## Parity Status

**Parity tested against Python 3.13.7 (session-recall v0.1.0)**

This .NET port is feature-equivalent to the Python reference implementation. All commands, options, exit codes, and JSON output formats match byte-for-byte (modulo timestamp/duration fields). See [net/tests/Parity](tests/Parity) for the test harness.

## Architecture

- **AutoMemory.Core**: Core library with database access, configuration, search, health checks, and utilities
- **AutoMemory.Cli**: Command-line interface executable (output: `session-recall`)
- **AutoMemory.Tests**: xUnit test suite

## Build

```bash
dotnet build net/AutoMemory.sln -c Release
```

## Test

```bash
dotnet test net/AutoMemory.sln -c Release
```

## Publish

The CLI is configured for single-file, self-contained, trimmed publishing:

```bash
dotnet publish net/src/AutoMemory.Cli/AutoMemory.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -o ./publish
```

Supported RIDs: `linux-x64`, `win-x64`, `osx-arm64`

### Encrypted Build (SQLCipher)

For users requiring database encryption at rest, an opt-in build with SQLCipher is available:

```bash
dotnet publish net/src/AutoMemory.Cli/AutoMemory.Cli.csproj \
  -c Release \
  -r linux-x64 \
  -p:UseEncryptedSqlite=true \
  --self-contained \
  -o ./publish-encrypted
```

See [docs/encrypted-build.md](docs/encrypted-build.md) for encryption setup and usage.

## AOT Decision (2026-04-26)

**Decision: Stay on single-file trimmed; do not adopt PublishAot.**

### Rationale

1. **Build complexity**: AOT requires platform-specific C++ build tools (Visual Studio C++ workload on Windows, clang on Linux/macOS), adding significant developer and CI overhead.

2. **Cross-compilation limitation**: AOT does not support cross-OS compilation. Building Linux binaries requires a Linux build agent, Windows binaries require Windows, etc. The current single-file approach works across platforms from any host.

3. **Size budget met**: Current single-file trimmed binary for linux-x64 is ~13.4 MB, well under the 25 MB budget.

4. **Cold-start performance**: The session-recall tool primarily interacts with SQLite I/O. Cold-start savings from AOT would be minimal compared to database access time. For a CLI tool run interactively (not in tight loops), the current startup time is acceptable.

5. **Toolchain simplicity**: Staying on single-file trimmed keeps the build process simple (`dotnet publish`) with no external dependencies beyond the .NET SDK.

### Revisit criteria

Reevaluate AOT if:
- Cold-start profiling shows ≥100ms is spent in .NET JIT/startup (vs I/O)
- GitHub Actions adds native AOT build matrix support
- Microsoft.Data.Sqlite publishes trim/AOT compatibility guidance
- User feedback indicates startup time is a pain point

### Current publish settings

See `AutoMemory.Cli.csproj`:

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>partial</TrimMode>
<InvariantGlobalization>true</InvariantGlobalization>
<DebugType>embedded</DebugType>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<TieredPGO>true</TieredPGO>
<PublishReadyToRun>true</PublishReadyToRun>
```

No additional `[DynamicDependency]` annotations are required with `TrimMode=partial`.

## PGO and ReadyToRun Optimization (2026-04-26)

**Decision: Enable TieredPGO and PublishReadyToRun by default.**

### Performance Impact

Benchmarked on Windows x64 with `hyperfine --warmup 2 --runs 20`:
- **Baseline**: 311.3ms mean (single-file trimmed, no PGO/R2R)
- **Optimized**: 163.9ms mean (with PGO/R2R)
- **Improvement**: 47% reduction in cold-start time

### Trade-offs

1. **Binary size**: Increases from ~13.4 MB to ~15.6 MB (+16%) due to ReadyToRun's pre-compiled native code
2. **Still under budget**: Well below the 25 MB target
3. **Build time**: Minimal increase (~1-2 seconds per publish)
4. **Compatibility**: Works with single-file trimmed publishing (unlike AOT which was rejected)

### How it works

- **TieredPGO**: Dynamic Profile-Guided Optimization learns hot paths at runtime and recompiles them
- **PublishReadyToRun**: Pre-compiles common .NET libraries to native code, reducing JIT work at startup

See [hyperfine-results.md](hyperfine-results.md) for full benchmark data.
