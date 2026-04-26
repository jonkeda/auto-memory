# Step 05 — Wire telemetry into Program

## Goal
Mirror `__main__.py` and call `Telemetry.Record(...)` at the entry boundary for every subcommand.

## Pattern (Program.cs)
```csharp
var sw = Stopwatch.StartNew();
int exit = 1;
try {
    exit = await Dispatch(argv);
} finally {
    Telemetry.Record(
        cmd: cmdName,
        durationMs: (int)sw.ElapsedMilliseconds,
        busyHits: ConnContext.BusyHits,
        attempts: ConnContext.Attempts,
        rowsReturned: ConnContext.RowsReturned,
        exitCode: exit,
        schemaOk: ConnContext.SchemaOk,
        tier: TierMap.TryGetValue(cmdName, out var t) ? t : null,
        queryHash: ConnContext.QueryHash,
        sessionIdPrefix: ConnContext.SessionIdPrefix,
        windowTier: null);
}
return exit;
```

## Done when
- [ ] Every command path records exactly once, even on early exits and exceptions.
