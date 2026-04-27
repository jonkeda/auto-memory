# Step 03 — `HealthCommand` flat-file fallback

## Goal

When `DatabaseNotFoundException` is thrown, run `FlatHealthCommand.Run()` instead of
emitting the stub `{"overall_score":null,"dimensions":[]}` payload.

## Changes to `HealthCommand.cs`

Replace the stub response with a real flat-file health computation:

```csharp
catch (DatabaseNotFoundException)
{
    if (args.GetFlag("json"))
    {
        return FlatHealthCommand.Run(args);  // real scores from flat-file backends
    }
    throw;
}
```

## New file: `FlatHealthCommand.cs`

`net/src/AutoMemory.Cli/Commands/FlatHealthCommand.cs`

```csharp
namespace AutoMemory.Cli.Commands;

internal static class FlatHealthCommand
{
    public static int Run(ParsedArgs args)
    {
        var repo = GitUtil.DetectRepo(Directory.GetCurrentDirectory());
        var ctx  = FlatHealthContext.Collect(repo);

        IFlatHealthDimension[] dims =
        [
            new DimFlatFreshness(),
            new DimFlatCorpus(),
            new DimFlatRepoCoverage(),
            new DimFlatSummaryCoverage(),
            new DimFlatRecentActivity(),
        ];

        var results = dims.Select(d => d.Score(ctx)).ToArray();

        var scoredResults = results.Where(r => r.Score.HasValue).ToArray();
        double? overall = scoredResults.Length > 0
            ? scoredResults.Average(r => r.Score!.Value)
            : null;

        var hints = results.Where(r => !string.IsNullOrEmpty(r.Hint))
                           .Select(r => r.Hint)
                           .Distinct()
                           .Take(3)
                           .ToArray();

        var output = new
        {
            overall_score = overall.HasValue ? Math.Round(overall.Value, 1) : (double?)null,
            dims = results.Select(r => new
            {
                name   = r.Name,
                score  = r.Score.HasValue ? Math.Round(r.Score.Value, 1) : (double?)null,
                zone   = r.Zone,
                detail = r.Detail,
                hint   = r.Hint,
            }).ToArray(),
            top_hints = hints,
            source    = "vscode-flat",
            backends  = new
            {
                session_state = ctx.SessionStateSessions,
                vscode_chat   = ctx.ChatSessions,
            },
        };

        Console.WriteLine(JsonSerializer.Serialize(output,
            new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
```

## Changes to `panel.js`

In `renderHealth`, detect `source === 'vscode-flat'` and show a banner:

```js
function renderHealth(data) {
  // ...existing rendering...
  if (data.source === 'vscode-flat') {
    // Show a note that this is flat-file mode
    const banner = `<p class="note">Flat-file mode — ${data.backends?.session_state ?? 0} Copilot CLI + ${data.backends?.vscode_chat ?? 0} Chat sessions</p>`;
    // Prepend to the health card
  }
}
```

Also: when `renderHealth` is called with `overall_score !== null`, **do NOT call `renderNoDb`**
— the health card itself provides the context.

## Changes to `messages.ts`

`postHealth` currently calls `setLastHealthData` — no change needed. `renderHealth` is
called by the message handler — no change needed.

However, `renderSessions` is currently called separately. Add logic: if
`source === 'vscode-flat'` on the health response, the session list panel should also
attempt to load sessions from the flat-file backends.

## Smoke tests

```powershell
# Force no-DB mode
$env:SESSION_RECALL_DB = "C:\nope\nope.db"
.\session-recall.exe health --json
# Expect: overall_score is a number (not null), 5 dims, source="vscode-flat"

.\session-recall.exe list --json --limit 5
# Expect: sessions from session-state + chat, source="vscode-mixed" or "vscode-chat"

Remove-Item env:SESSION_RECALL_DB
```
