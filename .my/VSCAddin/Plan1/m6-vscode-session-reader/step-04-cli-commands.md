# Step 04 — Wire CLI commands to the flat-file reader

## Goal

Make `list`, `show`, `search`, `files`, and `checkpoints` fall through to
`SessionStateStore` when `session-store.db` is absent and the `session-state/`
folder is present. No change to the commands when SQLite is available.

## Pattern — "dual reader" dispatch

Add a helper to `Program.cs` (or a shared `Dispatch.cs`) that wraps each command:

```csharp
// In Program.cs — replace per-command top-level function bodies

static int RunList(string[] args)
{
    var parsed = new ParsedArgs(["list", ..args]);
    try
    {
        using var conn = Connect.ConnectReadOnly(Config.DbPath);
        return ListCommand.RunCore(parsed, conn);
    }
    catch (DatabaseNotFoundException)
    {
        return VsCodeListCommand.Run(parsed);   // flat-file fallback
    }
}
```

Repeat for `RunShow`, `RunSearch`, `RunFiles`, `RunCheckpoints`.

`RunHealth` stays SQLite-only for now (health metrics don't apply to flat files);
it returns the VS Code detection stub from `StorageDetect.NoDatabaseJson`.

## New file: `net/src/AutoMemory.Cli/Commands/VsCodeListCommand.cs`

```csharp
using System;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeSessions;

namespace AutoMemory.Cli.Commands;

public static class VsCodeListCommand
{
    public static int Run(ParsedArgs args)
    {
        var store  = SessionStateStore.Default();
        var repo   = args.GetOption("repo");
        var limit  = args.GetInt("limit") ?? 10;
        var days   = args.GetInt("days") ?? 30;
        var json   = args.GetFlag("json");

        var sessions = store.List(limit, days, repo);

        if (json)
        {
            var items = sessions.Select(s => new
            {
                id      = s.Id,
                date    = s.CreatedAt.ToString("yyyy-MM-dd"),
                repo    = s.Repository,
                summary = Truncate(s.Summary, 100),
            });
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                sessions = items,
                source   = "vscode-session-state",
            }));
        }
        else
        {
            foreach (var s in sessions)
                Console.WriteLine($"{s.CreatedAt:yyyy-MM-dd}  {s.Id[..8]}  {s.Repository ?? "-"}  {Truncate(s.Summary, 60)}");
        }
        return 0;
    }

    private static string? Truncate(string? s, int max) =>
        s is null ? null : s.Length <= max ? s : s[..max] + "…";
}
```

## New files to create (same pattern)

| File | Command | Key output field |
|---|---|---|
| `VsCodeShowCommand.cs`        | `show <id>`      | Full session detail + tool calls |
| `VsCodeSearchCommand.cs`      | `search <query>` | `results[]` with excerpt + date |
| `VsCodeFilesCommand.cs`       | `files`          | `files[]` with path + session_id |
| `VsCodeCheckpointsCommand.cs` | `checkpoints`    | Read `checkpoints/index.md`, emit rows |

Each follows the same `Run(ParsedArgs)` → JSON / plain text pattern.

## Edits to `Program.cs`

Replace the five `using var conn = Connect.ConnectReadOnly(...)` top-level run functions
with the dual-reader try/catch pattern shown above.

## Done when

- [ ] `session-recall list --json` returns VS Code sessions when no SQLite DB present
- [ ] `session-recall list --json --limit 5` respects limit
- [ ] `session-recall list --json --repo jonkeda/Oravey2` filters by repo
- [ ] `session-recall show <8-char-prefix> --json` returns session detail
- [ ] `session-recall search "RPG" --json` returns matching sessions
- [ ] `session-recall files --json` returns file paths from tool arguments
- [ ] `session-recall checkpoints --json` returns checkpoint table rows
- [ ] All existing SQLite-backed tests still pass (no regression)
