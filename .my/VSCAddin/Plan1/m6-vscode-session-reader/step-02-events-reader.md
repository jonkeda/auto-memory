# Step 02 — `EventsReader`: stream `events.jsonl`

## Goal

Read `events.jsonl` and extract structured information:
- All user messages (for search)
- All tool call names + timestamps (for `show`)
- File paths touched via tool arguments (for `files`)

Uses `System.Text.Json` (already in the .NET runtime — no new dependency).

## New file: `net/src/AutoMemory.Core/VsCodeSessions/SessionEvent.cs`

```csharp
using System;
using System.Text.Json;

namespace AutoMemory.Core.VsCodeSessions;

public sealed record SessionEvent
{
    public required string   Type      { get; init; }
    public required string   Id        { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string?   ParentId  { get; init; }
    public JsonElement Data     { get; init; }  // raw; callers extract what they need
}
```

## New file: `net/src/AutoMemory.Core/VsCodeSessions/EventsReader.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoMemory.Core.VsCodeSessions;

/// <summary>
/// Streams events from events.jsonl without loading the whole file into memory.
/// </summary>
public static class EventsReader
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Enumerate all events in the file. Skips malformed lines.</summary>
    public static IEnumerable<SessionEvent> Read(string sessionDir)
    {
        var path = Path.Combine(sessionDir, "events.jsonl");
        if (!File.Exists(path)) yield break;

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            SessionEvent? ev = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                ev = new SessionEvent
                {
                    Type      = root.GetProperty("type").GetString() ?? "",
                    Id        = root.GetProperty("id").GetString() ?? "",
                    Timestamp = root.GetProperty("timestamp").GetDateTimeOffset(),
                    ParentId  = root.TryGetProperty("parentId", out var p) ? p.GetString() : null,
                    Data      = root.TryGetProperty("data", out var d)
                                    ? JsonDocument.Parse(d.GetRawText()).RootElement
                                    : default,
                };
            }
            catch { /* skip malformed lines */ }
            if (ev is not null) yield return ev;
        }
    }

    /// <summary>All user message contents in order.</summary>
    public static IEnumerable<string> UserMessages(string sessionDir)
    {
        foreach (var ev in Read(sessionDir))
        {
            if (ev.Type != "user.message") continue;
            if (ev.Data.TryGetProperty("content", out var c))
                yield return c.GetString() ?? "";
        }
    }

    /// <summary>All tool call names + timestamps.</summary>
    public static IEnumerable<(string Name, DateTimeOffset Timestamp)> ToolCalls(string sessionDir)
    {
        foreach (var ev in Read(sessionDir))
        {
            if (ev.Type != "tool.execution_start") continue;
            if (ev.Data.TryGetProperty("toolName", out var n))
                yield return (n.GetString() ?? "", ev.Timestamp);
        }
    }

    /// <summary>
    /// Heuristically extract file paths from tool arguments.
    /// Looks for string values in the arguments object that look like file paths.
    /// </summary>
    public static IEnumerable<string> TouchedFiles(string sessionDir)
    {
        foreach (var ev in Read(sessionDir))
        {
            if (ev.Type != "tool.execution_start") continue;
            if (!ev.Data.TryGetProperty("arguments", out var args)) continue;
            foreach (var path in ExtractFilePaths(args))
                yield return path;
        }
    }

    private static IEnumerable<string> ExtractFilePaths(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString() ?? "";
            // Heuristic: contains a path separator and a known extension
            if ((s.Contains('/') || s.Contains('\\')) && Path.HasExtension(s))
                yield return s;
        }
        else if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in el.EnumerateObject())
                foreach (var p in ExtractFilePaths(prop.Value))
                    yield return p;
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
                foreach (var p in ExtractFilePaths(item))
                    yield return p;
        }
    }
}
```

## Done when

- [ ] `EventsReader.Read(dir)` returns all events; skips malformed lines without throwing
- [ ] `UserMessages(dir)` returns content of all `user.message` events in order
- [ ] `ToolCalls(dir)` returns `(name, timestamp)` for each `tool.execution_start`
- [ ] `TouchedFiles(dir)` extracts file-path-like strings from tool arguments
- [ ] Works on a real `events.jsonl` from `~/.copilot/session-state/`
