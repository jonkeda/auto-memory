# Step 02 — `TranscriptReader`: stream transcript JSONL

## Goal

Read a single `<session-id>.jsonl` transcript file and extract structured data:
- Session metadata (`session.start` event)
- User messages (`user.message` events)
- Tool calls (`tool.execution_start` events)
- Last event timestamp (for `updated_at`)

Parsing must be **streaming** (line-by-line) so multi-MB files don't load fully into memory.

## Location

`net/src/AutoMemory.Core/VsCodeChat/TranscriptReader.cs`

## Types

### `ChatEvent.cs`

```csharp
namespace AutoMemory.Core.VsCodeChat;

public sealed record ChatEvent
{
    public required string          Type      { get; init; }
    public required string          Id        { get; init; }
    public          DateTimeOffset  Timestamp { get; init; }
    public          string?         ParentId  { get; init; }
    public          JsonElement     Data      { get; init; }
}
```

### `TranscriptReader.cs`

```csharp
namespace AutoMemory.Core.VsCodeChat;

public static class TranscriptReader
{
    /// <summary>
    /// Stream all events from a transcript JSONL file. Skips malformed lines.
    /// </summary>
    public static IEnumerable<ChatEvent> Read(string transcriptPath)
    {
        foreach (var line in File.ReadLines(transcriptPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            ChatEvent? ev = null;
            try { ev = ParseLine(line); }
            catch { /* skip malformed */ }
            if (ev is not null) yield return ev;
        }
    }

    /// <summary>Extract only metadata without reading the full file.</summary>
    public static ChatSessionMeta ReadMeta(string transcriptPath)
    {
        string? sessionId = null;
        DateTimeOffset createdAt = DateTimeOffset.MinValue;
        DateTimeOffset? updatedAt = null;
        string? firstUserMessage = null;
        int turnCount = 0;
        int toolCount = 0;

        foreach (var ev in Read(transcriptPath))
        {
            updatedAt = ev.Timestamp; // last one wins

            switch (ev.Type)
            {
                case "session.start":
                    if (ev.Data.TryGetProperty("sessionId", out var sid))
                        sessionId = sid.GetString();
                    if (ev.Data.TryGetProperty("startTime", out var st) &&
                        DateTimeOffset.TryParse(st.GetString(), out var parsed))
                        createdAt = parsed;
                    else
                        createdAt = ev.Timestamp;
                    break;

                case "user.message":
                    turnCount++;
                    if (firstUserMessage is null &&
                        ev.Data.TryGetProperty("content", out var content))
                    {
                        var raw = content.GetString() ?? "";
                        firstUserMessage = raw.Length > 200 ? raw[..200] : raw;
                    }
                    break;

                case "tool.execution_start":
                    toolCount++;
                    break;
            }
        }

        return new ChatSessionMeta(
            SessionId: sessionId ?? Path.GetFileNameWithoutExtension(transcriptPath),
            CreatedAt: createdAt,
            UpdatedAt: updatedAt,
            Summary: firstUserMessage,
            TurnCount: turnCount,
            ToolCount: toolCount);
    }

    private static ChatEvent ParseLine(string line)
    {
        var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        var type = root.GetProperty("type").GetString() ?? "";
        var id   = root.GetProperty("id").GetString() ?? "";
        var ts   = DateTimeOffset.MinValue;
        if (root.TryGetProperty("timestamp", out var tsEl) &&
            DateTimeOffset.TryParse(tsEl.GetString(), out var parsed))
            ts = parsed;

        string? parentId = null;
        if (root.TryGetProperty("parentId", out var pid) && pid.ValueKind != JsonValueKind.Null)
            parentId = pid.GetString();

        var data = root.TryGetProperty("data", out var d) ? d.Clone() : default;

        return new ChatEvent { Type = type, Id = id, Timestamp = ts, ParentId = parentId, Data = data };
    }
}
```

### `ChatSessionMeta.cs`

```csharp
namespace AutoMemory.Core.VsCodeChat;

public sealed record ChatSessionMeta(
    string SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? Summary,
    int TurnCount,
    int ToolCount
);
```

## Tests

`AutoMemory.Tests/VsCodeChat/TranscriptReaderTests.cs`

Use a small fixture file at `net/tests/fixtures/chat-transcripts/abc12345-0001/transcript.jsonl`:

```jsonl
{"type":"session.start","data":{"sessionId":"abc12345-0001-0000-0000-000000000000","version":1,"producer":"copilot-agent","copilotVersion":"0.46.x","vscodeVersion":"1.118.x","startTime":"2026-04-20T10:00:00.000Z"},"id":"event-001","timestamp":"2026-04-20T10:00:00.000Z","parentId":null}
{"type":"user.message","data":{"content":"How do I add a NuGet package?","attachments":[]},"id":"event-002","timestamp":"2026-04-20T10:00:05.000Z","parentId":"event-001"}
{"type":"assistant.turn_start","data":{"turnId":"0"},"id":"event-003","timestamp":"2026-04-20T10:00:05.001Z","parentId":"event-002"}
{"type":"tool.execution_start","data":{"toolCallId":"tc-001","toolName":"semantic_search","arguments":{"query":"NuGet"}},"id":"event-004","timestamp":"2026-04-20T10:00:06.000Z","parentId":"event-003"}
{"type":"tool.execution_complete","data":{"toolCallId":"tc-001","success":true},"id":"event-005","timestamp":"2026-04-20T10:00:06.500Z","parentId":"event-004"}
{"type":"assistant.message","data":{"messageId":"msg-001","content":"Use `dotnet add package`.","toolRequests":[],"reasoningText":""},"id":"event-006","timestamp":"2026-04-20T10:00:08.000Z","parentId":"event-005"}
{"type":"assistant.turn_end","data":{"turnId":"0"},"id":"event-007","timestamp":"2026-04-20T10:00:08.001Z","parentId":"event-006"}
{"type":"user.message","data":{"content":"What about removing one?","attachments":[]},"id":"event-008","timestamp":"2026-04-20T10:01:00.000Z","parentId":"event-007"}
```

Test cases:
- `Read_AllEvents_Parsed` — 8 events returned, none skipped
- `ReadMeta_SessionId_MatchesStartEvent`
- `ReadMeta_CreatedAt_MatchesStartTime`
- `ReadMeta_UpdatedAt_IsLastEventTimestamp`
- `ReadMeta_Summary_IsFirstUserMessageTruncated`
- `ReadMeta_TurnCount_Is2` (two `user.message` events)
- `ReadMeta_ToolCount_Is1` (one `tool.execution_start`)
- `Read_MalformedLine_IsSkipped` (inject a bad line into fixture, verify no exception)
