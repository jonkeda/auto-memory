# Phase 4 — Health (9 dimensions)

**Goal:** port the entire `health/` package with identical scoring.

## Source → target mapping

| Python | .NET |
| --- | --- |
| `health/scoring.py` | `Health/Scoring.cs` |
| `health/dim_concurrency.py` | `Health/DimConcurrency.cs` |
| `health/dim_corpus.py` | `Health/DimCorpus.cs` |
| `health/dim_disclosure.py` | `Health/DimDisclosure.cs` |
| `health/dim_e2e.py` | `Health/DimE2E.cs` |
| `health/dim_freshness.py` | `Health/DimFreshness.cs` |
| `health/dim_latency.py` | `Health/DimLatency.cs` |
| `health/dim_repo_coverage.py` | `Health/DimRepoCoverage.cs` |
| `health/dim_schema.py` | `Health/DimSchema.cs` |
| `health/dim_summary_coverage.py` | `Health/DimSummaryCoverage.cs` |
| `commands/health.py` | `AutoMemory.Cli/Commands/HealthCommand.cs` |

## Design

- Define an `IHealthDimension` interface:
  ```csharp
  public interface IHealthDimension
  {
      string Name { get; }
      double Weight { get; }
      DimensionResult Run(SqliteConnection conn, HealthContext ctx);
  }
  ```
- `Scoring.cs` aggregates results, computes the weighted score, and produces the JSON object Python emits.
- One class per dimension; pure functions where Python uses pure functions.

## Parity requirements

- Weights, thresholds, bucket boundaries, and label strings must match Python verbatim.
- Score rounding must match Python (`round(x, 2)` — use `Math.Round(x, 2, MidpointRounding.ToEven)`).
- JSON field names, ordering, and nested shape identical.

## Tests

- Port `test_health_scoring.py`, `test_dim_disclosure.py`.
- Add a fixture DB seeded with deterministic data; assert per-dimension scores byte-identical to Python on the same fixture.
- Add property-style tests: weights sum to 1.0 ± epsilon.

## Acceptance

- `session-recall health` and `session-recall health --json` produce identical output to Python on a shared fixture.
- All 9 dimensions individually unit-tested.
- Performance: full health run on a 50MB fixture DB completes in `< 500ms` (Python baseline + 20%).

## Risks

- Floating-point divergence between Python and .NET on aggregations — use `decimal` for the final weighted sum, then convert to `double` for output, matching Python's two-decimal rounding.
- E2E dim relies on running other commands as subprocesses in Python; in .NET, refactor to call `Core` APIs directly to avoid spawning the same process.
