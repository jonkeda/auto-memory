# Step 05 — Tests

## Goal

Complete unit and integration tests for M8 flat-file health.

## Test files

### `FlatHealthContextTests.cs` (4 tests)

`AutoMemory.Tests/Health/FlatHealthContextTests.cs`

Use fixture data from M6 (`session-state`) and M7 (`chat-transcripts`) fixtures.
Inject a custom `ChatSessionStore` factory (add an overload to `FlatHealthContext.Collect`)
that accepts pre-built session lists instead of scanning the real file system.

- `Collect_TotalCount_IsSumOfBothBackends`
- `Collect_RecentCount_OnlyLast7Days`
- `Collect_SummaryCount_OnlyNonEmpty`
- `Collect_RepoSessions_MatchesCurrentRepo`

### `FlatDimensionTests.cs` (10 tests)

`AutoMemory.Tests/Health/FlatDimensionTests.cs`

Pure unit tests — inject a `FlatHealthContext` with known values.

- `DimFlatFreshness_NewestIn1h_ReturnsScore10_Green`
- `DimFlatFreshness_NewestIn8d_ReturnsScore2_Red`
- `DimFlatCorpus_130Sessions_ReturnsGreen`
- `DimFlatCorpus_0Sessions_ReturnsRed`
- `DimFlatRepoCoverage_NullRepo_ReturnsCalibrating`
- `DimFlatRepoCoverage_10Matches_ReturnsGreen`
- `DimFlatSummaryCoverage_AllHave_Returns10`
- `DimFlatSummaryCoverage_None_Returns2`
- `DimFlatRecentActivity_20Recent_Returns10`
- `DimFlatRecentActivity_0Recent_Returns2`

### `FlatHealthCommandTests.cs` (3 tests)

`AutoMemory.Tests/Health/FlatHealthCommandTests.cs`

- `Run_OutputsValidJson`
- `Run_OverallScore_IsNonNull`
- `Run_Source_IsVscodFlat`

(Use `StringWriter` to capture `Console.Out`; inject mock `FlatHealthContext`.)

## Total: 17 new tests

## Pass criteria

- All 17 new M8 tests pass
- All 21 M6 tests still pass
- All 26 M7 tests still pass
- Total test suite: 64+ tests pass

## Integration smoke test

```powershell
$env:SESSION_RECALL_DB = "C:\nope\nope.db"
.\session-recall.exe health --json | ConvertFrom-Json | Select-Object overall_score, source
# Expected: overall_score = <number>, source = "vscode-flat"

.\session-recall.exe health --json | ConvertFrom-Json | Select-Object -ExpandProperty dims | Format-Table name, score, zone
# Expected: 5 rows, all non-null scores

Remove-Item env:SESSION_RECALL_DB
```
