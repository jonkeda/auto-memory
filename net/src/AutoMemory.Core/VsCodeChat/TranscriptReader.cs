using System.Text.Json;

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
