# Step 02 — Roslyn SQL analyzer

## Goal
Custom analyzer that flags raw SQL string concatenation/interpolation inside `AutoMemory.Core`.

## Scope
- Detect `string +` and `$"…"` interpolation flowing into `SqliteCommand.CommandText`.
- Allow-list: parameterized commands and the FTS5 sanitizer call site.
- Ship as `AnalyzerReference` so consumers inherit the rule via the NuGet package.

## Risks
- False positives on harmless concatenation.
- Build-time impact for consumers.

## Done when
- [ ] Analyzer produces an error on a known-bad sample and zero diagnostics on the production code.
- [ ] Embedded in the NuGet package; consumer projects inherit it.
