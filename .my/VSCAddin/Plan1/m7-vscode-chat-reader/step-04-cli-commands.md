# Step 04 — CLI commands: wire Chat as third fallback

## Goal

Add `VscChatListCommand`, `VscChatShowCommand`, `VscChatSearchCommand` and wire them
as the third fallback in `Program.cs` after `VsCodeListCommand` (session-state).

## Fallback chain in `Program.cs`

```
list / show / search called
  ├─ Try SQLite (ListCommand / ShowCommand / SearchCommand)
  │    DatabaseNotFoundException ↓
  ├─ Try session-state (VsCodeListCommand / VsCodeShowCommand / VsCodeSearchCommand)
  │    Returns [] (or no match) ↓
  └─ Try Chat transcripts (VscChatListCommand / VscChatShowCommand / VscChatSearchCommand)
```

**Note:** session-state uses `DatabaseNotFoundException` to trigger. Chat falls through
when session-state returns an empty `sessions` array OR when the `session-state` folder
is absent. Simplest approach: **always run both** and merge results, sorted by date desc.

## New files

### `net/src/AutoMemory.Cli/Commands/VscChatListCommand.cs`

```csharp
namespace AutoMemory.Cli.Commands;

internal static class VscChatListCommand
{
    public static int Run(ParsedArgs args)
    {
        var store = ChatSessionStore.Default();
        var limit = args.GetInt("limit", 10);
        var days  = args.GetInt("days", null);
        var repo  = args.GetString("repo", null);

        var sessions = store.List(limit, days, repo);
        var total    = store.Count();

        var result = new
        {
            sessions = sessions.Select(s => new
            {
                id        = s.Id[..8],
                date      = s.CreatedAt.ToString("yyyy-MM-dd"),
                repo      = s.Repository,
                workspace = s.WorkspacePath,
                summary   = s.Summary,
                turns     = s.TurnCount,
                tools     = s.ToolCount,
            }).ToArray(),
            source = "vscode-chat",
            total  = total,
        };

        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
```

### `net/src/AutoMemory.Cli/Commands/VscChatShowCommand.cs`

```csharp
internal static class VscChatShowCommand
{
    public static int Run(ParsedArgs args)
    {
        var id    = args.GetPositional(0) ?? "";
        var store = ChatSessionStore.Default();
        var s     = store.Show(id);

        if (s is null)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { error = $"Session {id} not found" }));
            return 1;
        }

        // Stream the first 50 user messages for detail view
        var turns = TranscriptReader.Read(s.TranscriptPath)
            .Where(e => e.Type == "user.message")
            .Take(50)
            .Select(e => new
            {
                role    = "user",
                ts      = e.Timestamp.ToString("o"),
                content = e.Data.TryGetProperty("content", out var c) ? c.GetString() : null,
            })
            .ToList();

        var result = new
        {
            id        = s.Id,
            date      = s.CreatedAt.ToString("yyyy-MM-dd"),
            repo      = s.Repository,
            workspace = s.WorkspacePath,
            summary   = s.Summary,
            turns     = turns,
            source    = "vscode-chat",
        };

        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
```

### `net/src/AutoMemory.Cli/Commands/VscChatSearchCommand.cs`

```csharp
internal static class VscChatSearchCommand
{
    public static int Run(ParsedArgs args)
    {
        var query = args.GetPositional(0) ?? args.GetString("query", "");
        var limit = args.GetInt("limit", 10);
        var store = ChatSessionStore.Default();
        var hits  = store.Search(query!, limit);

        var result = new
        {
            query,
            sessions = hits.Select(s => new
            {
                id      = s.Id[..8],
                date    = s.CreatedAt.ToString("yyyy-MM-dd"),
                repo    = s.Repository,
                summary = s.Summary,
                turns   = s.TurnCount,
            }).ToArray(),
            source = "vscode-chat",
        };

        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
```

## Changes to `Program.cs`

Modify `RunList`, `RunShow`, `RunSearch` to merge session-state + chat results:

```csharp
static int RunList(string[] commandArgs)
{
    // ... parser setup ...
    try
    {
        return ListCommand.Run(parsedArgs);        // SQLite
    }
    catch (DatabaseNotFoundException)
    {
        // Run both flat-file backends and merge
        return MergedListCommand.Run(parsedArgs);
    }
}
```

Or add a `MergedListCommand` that calls both `VsCodeListCommand` and `VscChatListCommand`
and outputs a combined sorted array:

```json
{
  "sessions": [...],
  "source": "vscode-mixed",
  "backends": { "session_state": 44, "vscode_chat": 86 }
}
```

## Tests

`AutoMemory.Tests/VsCodeChat/VscChatCommandTests.cs`

- `VscChatListCommand_OutputsJsonWithSourceField`
- `VscChatListCommand_LimitIsRespected`
- `VscChatShowCommand_KnownId_OutputsSession`
- `VscChatShowCommand_UnknownId_Returns1`
- `VscChatSearchCommand_KeywordMatch_ReturnsResults`
