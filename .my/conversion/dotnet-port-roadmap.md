# .NET Port Roadmap — `auto-memory` → `net/`

Goal: produce a feature-equivalent .NET port of the Python `session_recall` CLI under `net/`, preserving the zero-dependency philosophy, read-only SQLite access, and identical CLI surface and exit codes.

## Guiding principles

- **Behavior parity first.** Same subcommands, same flags, same stdout/stderr, same exit codes, same JSON shapes. The Python suite is the spec.
- **Minimal dependencies.** Use BCL only where possible. `Microsoft.Data.Sqlite` is the single allowed runtime dependency (it ships ootb with .NET and is the closest analog to Python's `sqlite3`). No MediatR / Serilog / etc.
- **Single-file, self-contained publish.** Distribute as a trimmed, AOT-friendly executable so users get the same "no install pain" feel as `pip install`.
- **Read-only, schema-checked.** Mirror `PRAGMA query_only = ON`, `mode=ro` URI, busy-timeout + retry semantics exactly.
- **Cross-platform.** Linux (WSL2) is the primary target — same DB path resolution as Python (`~/.copilot/session-store.db`).

---

## Target layout — `net/`

```
net/
  AutoMemory.sln
  Directory.Build.props                # LangVersion=latest, Nullable=enable, TreatWarningsAsErrors
  global.json                           # pin .NET 9 SDK
  src/
    AutoMemory.Cli/                     # entrypoint — maps to src/session_recall/__main__.py
      Program.cs
      Commands/                         # one class per subcommand (List, Files, Checkpoints, Show, Search, Health, SchemaCheck)
      AutoMemory.Cli.csproj             # <OutputType>Exe</OutputType>, PublishAot, AssemblyName=session-recall
    AutoMemory.Core/                    # library — db, health, util
      Config.cs
      Types.cs
      Db/
        Connect.cs                      # read-only open + retry
        SchemaCheck.cs
      Health/
        Scoring.cs
        Dim*.cs                         # one per dim_*.py
      Util/
        DetectRepo.cs
        FormatOutput.cs
        Telemetry.cs
      AutoMemory.Core.csproj
  tests/
    AutoMemory.Tests/                   # xUnit, mirrors src/session_recall/tests/
      AutoMemory.Tests.csproj
  README.md                             # net-specific build/run notes
  .editorconfig
```

Naming: keep CLI binary name `session-recall` so existing `deploy/install.md` instructions still work after `dotnet publish`.

---

## Phases

### Phase 0 — Scaffolding

- [ ] `dotnet new sln`, `classlib`, `console`, `xunit` projects under `net/`.
- [ ] `global.json` pinning .NET 9; `Directory.Build.props` enabling `nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `LangVersion=latest`.
- [ ] Add single `Microsoft.Data.Sqlite` PackageReference to `AutoMemory.Core`.
- [ ] CI: GitHub Actions matrix (`ubuntu-latest`, `windows-latest`) running `dotnet test` alongside existing pytest job.

### Phase 1 — Core primitives (port `db/`, `config.py`, `util/`)

- [ ] `Config.cs` — env vars `SESSION_RECALL_DB`, `SESSION_RECALL_TELEMETRY`, retry delays, expected schema version. Match defaults exactly.
- [ ] `Db/Connect.cs` — open with `Mode=ReadOnly`, `Cache=Shared` off, set `PRAGMA busy_timeout=500` + `PRAGMA query_only=ON`, retry with delays `[50,150,450]ms` ±20% jitter on `SQLITE_BUSY`/locked.
- [ ] `Db/SchemaCheck.cs` — port table/column/version checks, exit code 5 on mismatch.
- [ ] `Util/DetectRepo.cs`, `FormatOutput.cs`, `Telemetry.cs` (append JSONL to telemetry path).
- [ ] Tests: `test_connect.py`, `test_schema_check.py`, `test_telemetry.py` ported to xUnit.

### Phase 2 — Tier-1 commands (cheap scans)

- [ ] `list` — `--repo`, `--limit`, `--days`, `--json`.
- [ ] `files` — same flags.
- [ ] `checkpoints` — same flags.
- [ ] System.CommandLine **not** required — a small hand-rolled `ArgParser` keeps zero deps. Reuse across subcommands.
- [ ] Tests ported: `test_list_sessions.py`, `test_days_filter.py`, `test_parser.py`.

### Phase 3 — Tier-2/3 commands

- [ ] `search` — FTS query with sanitization (`test_search_sanitize.py`), `--repo`, `--limit`, `--days`, `--json`.
- [ ] `show` — `session_id` positional, `--turns` (non-negative int), `--full`, `--json`. Port terminal sanitization (`test_sanitize_terminal.py`, `test_show_session.py`).
- [ ] `schema-check` — Tier 0 ops command.

### Phase 4 — Health (9 dimensions)

- [ ] Port each `dim_*.py` 1:1 to `Health/Dim*.cs`.
- [ ] `Health/Scoring.cs` — same weights, same thresholds, same output schema.
- [ ] Tests: `test_health_scoring.py`, `test_dim_disclosure.py`.

### Phase 5 — Telemetry parity

- [ ] Match telemetry JSONL field names exactly (`cmd`, `duration_ms`, `exit_code`, `tier`, `query_hash`, `session_id_prefix`, `window_tier`).
- [ ] `query_hash` must use the same hashing algorithm/truncation as Python.
- [ ] Tier map identical to `TIER_MAP` in `__main__.py`.

### Phase 6 — Packaging & distribution

- [ ] `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true`.
- [ ] Investigate `PublishAot=true` (requires Sqlite native interop — verify on Linux/Windows).
- [ ] Output: a single `session-recall` binary placed alongside Python `pip` install path in `deploy/install.md`.
- [ ] Bench cold-start: target `< 50ms` for `list --limit 5` to beat Python interpreter startup.

### Phase 7 — Parity gate (release criteria)

- [ ] **Golden-output tests**: run Python and .NET CLIs against a fixture DB; diff stdout (text + `--json`) for every subcommand. Must be byte-identical for JSON, semantically identical for text.
- [ ] All 90+ pytest cases have an xUnit equivalent and pass.
- [ ] Exit codes match for: success (0), generic error (1), DB locked (3), DB not found (4), schema mismatch (5).
- [ ] CI green on Ubuntu + Windows.

### Phase 8 — Stretch

- [ ] NuGet package for `AutoMemory.Core` so other tools can embed recall.
- [ ] Roslyn analyzer that flags raw SQL string concatenation in `Core` (defense-in-depth for the FTS sanitizer).
- [ ] Optional MCP server in C# reusing `AutoMemory.Core` (aligns with main `ROADMAP.md`).

---

## Risks & open questions

- **AOT + Microsoft.Data.Sqlite**: trim warnings around SQLitePCLRaw bundle providers — may need `e_sqlite3` bundle and manual rd.xml. Validate early in Phase 0.
- **Telemetry hash compatibility**: confirm Python's hash function (likely SHA-256 truncated) is reproducible byte-for-byte in .NET.
- **Argparse vs hand-rolled parser**: argparse error message wording differs. If parity tests assert on stderr text, accept divergence here and document it.
- **Path expansion**: Python's `Path.home()` vs .NET `Environment.GetFolderPath(UserProfile)` — verify identical resolution under WSL2.
- **Locale / number formatting**: force `CultureInfo.InvariantCulture` everywhere to match Python defaults.

---

## Out of scope (for the port)

- Rewriting the `deploy/install.md` UX flow.
- Replacing the Python version — the .NET port is **additive**, lives in `net/`, and is selected by users who prefer a single static binary.
- Adding features not present in the Python CLI (any new feature lands in Python first, then ports).

---

## Definition of Done

A user on Ubuntu/WSL2 can:

1. `cd net && dotnet publish -c Release` → get a single `session-recall` binary.
2. Run every subcommand documented in `README.md` and get the same output as the Python CLI.
3. Pass `session-recall health` with the same score as the Python implementation against the same DB.
