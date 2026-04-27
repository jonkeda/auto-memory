# Step 02 — Five flat-file health dimensions

## Goal

Create 5 `IFlatHealthDimension` implementations that score the flat-file session data.
These are simpler than the SQLite dimensions — no DB queries, just math on `FlatHealthContext`.

## Location

`net/src/AutoMemory.Core/Health/Flat/`

## Interface

```csharp
namespace AutoMemory.Core.Health.Flat;

public interface IFlatHealthDimension
{
    string Name { get; }
    FlatDimResult Score(FlatHealthContext ctx);
}

public sealed record FlatDimResult(
    string Name,
    double? Score,   // null = CALIBRATING
    string Zone,     // GREEN / AMBER / RED / CALIBRATING
    string Detail,
    string Hint
);
```

## Scoring logic

### `DimFlatFreshness.cs`

Score based on how old the newest session is:

| Age | Score | Zone |
|---|---|---|
| < 1h    | 10.0 | GREEN |
| < 6h    | 9.0  | GREEN |
| < 24h   | 8.0  | GREEN |
| < 3d    | 6.0  | AMBER |
| < 7d    | 4.0  | AMBER |
| ≥ 7d    | 2.0  | RED |
| No data | null | CALIBRATING |

Detail: `"X.Yh ago"` or `"Xd ago"`.

### `DimFlatCorpus.cs`

Score based on total session count:

| Count | Score | Zone |
|---|---|---|
| ≥ 100 | 10.0 | GREEN |
| ≥ 50  | 8.0  | GREEN |
| ≥ 20  | 6.0  | AMBER |
| ≥ 5   | 4.0  | AMBER |
| < 5   | 2.0  | RED |

Detail: `"130 sessions (44 state + 86 chat)"`.

### `DimFlatRepoCoverage.cs`

Score based on sessions matching the current repo:

| Repo sessions | Score | Zone |
|---|---|---|
| ≥ 10 | 10.0 | GREEN |
| ≥ 5  | 8.0  | GREEN |
| ≥ 2  | 6.0  | AMBER |
| 1    | 4.0  | AMBER |
| 0    | 2.0  | RED |
| No repo | null | CALIBRATING |

Detail: `"5 sessions for jonkeda/auto-memory"`.

### `DimFlatSummaryCoverage.cs`

Score based on percentage of sessions with a non-empty summary:

| % | Score | Zone |
|---|---|---|
| ≥ 80% | 10.0 | GREEN |
| ≥ 60% | 8.0  | GREEN |
| ≥ 40% | 6.0  | AMBER |
| ≥ 20% | 4.0  | AMBER |
| < 20% | 2.0  | RED |
| 0 total | null | CALIBRATING |

Detail: `"82% (107/130)"`.

### `DimFlatRecentActivity.cs`

Score based on sessions updated in the last 7 days:

| Recent | Score | Zone |
|---|---|---|
| ≥ 20 | 10.0 | GREEN |
| ≥ 10 | 8.0  | GREEN |
| ≥ 5  | 6.0  | AMBER |
| ≥ 1  | 4.0  | AMBER |
| 0    | 2.0  | RED |

Detail: `"12 sessions (last 7d)"`.

## Overall score

Arithmetic mean of non-null dimension scores.

## Tests

`AutoMemory.Tests/Health/FlatDimensionTests.cs` (10 tests)

One test per boundary for each dimension:
- `DimFlatFreshness_OldSession_ReturnsRed`
- `DimFlatFreshness_RecentSession_ReturnsGreen`
- `DimFlatCorpus_100Sessions_Returns10`
- `DimFlatCorpus_NoSessions_Returns2`
- `DimFlatRepoCoverage_NoCurrentRepo_ReturnsCalibrating`
- `DimFlatRepoCoverage_5Matches_ReturnsGreen`
- `DimFlatSummaryCoverage_AllHaveSummary_Returns10`
- `DimFlatSummaryCoverage_NoneSummary_Returns2`
- `DimFlatRecentActivity_ManyRecent_ReturnsGreen`
- `DimFlatRecentActivity_NoneRecent_ReturnsRed`
