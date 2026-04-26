# Phase 8 — Stretch

**Goal:** capture post-v1.0 ideas that build on the .NET port. Nothing here blocks shipping.

## 8.1 NuGet package — `AutoMemory.Core`

- Publish `AutoMemory.Core` to nuget.org so other tools can embed session recall.
- Public API surface: `SessionStore`, `HealthRunner`, `SchemaChecker`, `TelemetryWriter`.
- SemVer commitment starts here; CLI internals stay out of the public API.
- Symbol packages (`.snupkg`) and SourceLink for debugging.

## 8.2 Roslyn analyzer — SQL hardening

- Custom analyzer flags raw SQL string concatenation/interpolation inside `AutoMemory.Core`.
- Allow-list parameterized commands and the FTS5 sanitizer call site only.
- Ships as an `AnalyzerReference` inside `AutoMemory.Core` so consumers inherit the rule.

## 8.3 MCP server in C#

- Reuse `AutoMemory.Core` to expose a Model Context Protocol server.
- Aligns with the main `ROADMAP.md` item: "Optional MCP server wrapper for IDE integration".
- Single binary `session-recall-mcp` published alongside the CLI.

## 8.4 Embedded provider option

- Offer `Microsoft.Data.Sqlite` with `SQLitePCLRaw.bundle_e_sqlcipher` as an opt-in build for users who want encrypted session stores.
- Gate behind a `csproj` property; default build stays `bundle_e_sqlite3`.

## 8.5 Profile-Guided Optimization

- Investigate dynamic PGO and ReadyToRun for cold-start improvements once AOT is settled.
- Target: shave another 20% off `list --limit 5`.

## Acceptance

Each item is independently shippable; track as separate issues post-v1.0.

## Out of scope

- Anything that diverges the .NET CLI surface from Python — those changes go through the main `ROADMAP.md` first.
