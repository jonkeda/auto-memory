# Step 03 — `ChatSessionStore`: discover all workspaces, list/show/search

## Goal

Implement `ChatSessionStore` that combines `WorkspaceStorageLocator` + `TranscriptReader`
to provide the same interface as `SessionStateStore` (M6):

- `IsAvailable()` — returns true if any workspaceStorage with transcripts is found
- `Count()` — total session count across all workspaces
- `List(limit, days, repo)` — sessions sorted by `createdAt` desc
- `Show(idPrefix)` — load single session with full turn list
- `Search(query, limit)` — keyword match in summary / workspace path

## Location

`net/src/AutoMemory.Core/VsCodeChat/ChatSessionStore.cs`

## Implementation

```csharp
namespace AutoMemory.Core.VsCodeChat;

public sealed class ChatSessionStore
{
    private readonly IReadOnlyList<ChatWorkspace> _workspaces;

    private ChatSessionStore(IReadOnlyList<ChatWorkspace> workspaces)
        => _workspaces = workspaces;

    public static ChatSessionStore Default()
    {
        var ws = WorkspaceStorageLocator.FindWorkspaces().ToList();
        return new ChatSessionStore(ws);
    }

    public bool IsAvailable() => _workspaces.Count > 0;

    public int Count()
    {
        int n = 0;
        foreach (var ws in _workspaces)
            n += Directory.GetFiles(ws.TranscriptDir, "*.jsonl").Length;
        return n;
    }

    public IReadOnlyList<ChatSession> List(int limit = 10, int? days = null, string? repo = null)
    {
        var cutoff = days.HasValue
            ? DateTimeOffset.UtcNow.AddDays(-days.Value)
            : DateTimeOffset.MinValue;

        var sessions = new List<ChatSession>();

        foreach (var ws in _workspaces)
        {
            foreach (var file in Directory.EnumerateFiles(ws.TranscriptDir, "*.jsonl"))
            {
                var s = TryLoad(file, ws);
                if (s is null) continue;
                if (s.CreatedAt < cutoff) continue;
                if (repo is not null && !string.Equals(s.Repository, repo, StringComparison.OrdinalIgnoreCase)) continue;
                sessions.Add(s);
            }
        }

        return sessions
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToList();
    }

    public ChatSession? Show(string idPrefix)
    {
        foreach (var ws in _workspaces)
        {
            foreach (var file in Directory.EnumerateFiles(ws.TranscriptDir, "*.jsonl"))
            {
                var stem = Path.GetFileNameWithoutExtension(file);
                if (stem.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase))
                    return TryLoad(file, ws);
            }
        }
        return null;
    }

    public IReadOnlyList<ChatSession> Search(string query, int limit = 10)
    {
        var q = query.ToLowerInvariant();
        var results = new List<ChatSession>();

        foreach (var ws in _workspaces)
        {
            foreach (var file in Directory.EnumerateFiles(ws.TranscriptDir, "*.jsonl"))
            {
                var s = TryLoad(file, ws);
                if (s is null) continue;
                if ((s.Summary?.ToLowerInvariant().Contains(q) ?? false) ||
                    (s.WorkspacePath?.ToLowerInvariant().Contains(q) ?? false) ||
                    (s.Repository?.ToLowerInvariant().Contains(q) ?? false))
                    results.Add(s);
            }
        }

        return results
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToList();
    }

    private static ChatSession? TryLoad(string transcriptPath, ChatWorkspace ws)
    {
        try
        {
            var meta = TranscriptReader.ReadMeta(transcriptPath);
            var repo = DeriveRepo(ws.WorkspacePath);
            return new ChatSession
            {
                Id             = meta.SessionId,
                WorkspaceHash  = ws.Hash,
                WorkspacePath  = ws.WorkspacePath,
                Repository     = repo,
                CreatedAt      = meta.CreatedAt,
                UpdatedAt      = meta.UpdatedAt,
                Summary        = meta.Summary,
                TurnCount      = meta.TurnCount,
                ToolCount      = meta.ToolCount,
                TranscriptPath = transcriptPath,
            };
        }
        catch { return null; }
    }

    /// <summary>
    /// Best-effort: extract "owner/repo" from a local workspace path by looking
    /// at the folder structure. Heuristic: last two path segments that look like
    /// a git remote (no spaces, owner/repo pattern). Falls back to last folder name.
    /// </summary>
    private static string? DeriveRepo(string? workspacePath)
    {
        if (workspacePath is null) return null;
        // Try to read .git/config for remote.origin.url
        var gitConfig = Path.Combine(workspacePath, ".git", "config");
        if (File.Exists(gitConfig))
        {
            foreach (var line in File.ReadLines(gitConfig))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("url = ", StringComparison.OrdinalIgnoreCase))
                {
                    var url = trimmed.Substring("url = ".Length).Trim();
                    // git@github.com:owner/repo.git  or  https://github.com/owner/repo.git
                    var match = System.Text.RegularExpressions.Regex.Match(url, @"[:/]([^/]+/[^/]+?)(?:\.git)?$");
                    if (match.Success) return match.Groups[1].Value;
                }
            }
        }
        // Fallback: last folder name
        return Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar));
    }
}
```

## Tests

`AutoMemory.Tests/VsCodeChat/ChatSessionStoreTests.cs`

Use fixture directory `net/tests/fixtures/chat-transcripts/` with two fake workspace hashes:

```
fixtures/
  chat-transcripts/
    ws-hash-0001/
      workspace.json            { "workspace": "file:///e%3A/repos/Owner/RepoA" }
      GitHub.copilot-chat/
        transcripts/
          session-0001.jsonl    (3 user messages, 2 tool calls)
          session-0002.jsonl    (1 user message, 0 tool calls)
    ws-hash-0002/
      workspace.json            { "workspace": "file:///e%3A/repos/Owner/RepoB" }
      GitHub.copilot-chat/
        transcripts/
          session-0003.jsonl    (5 user messages, 10 tool calls)
```

A custom `ChatSessionStore` factory that accepts an explicit list of `ChatWorkspace` objects
(inject the fixture paths instead of calling `WorkspaceStorageLocator`).

Test cases (10):
- `List_ReturnsAllSessions_WhenNoFilter`
- `List_LimitIsRespected`
- `List_RepoFilter_OnlyMatchingSessions`
- `List_DaysFilter_ExcludesOldSessions`
- `List_OrderedByCreatedAtDesc`
- `Show_ByFullId_ReturnsSession`
- `Show_ByShortPrefix_ReturnsSession`
- `Show_UnknownId_ReturnsNull`
- `Search_ByKeywordInSummary_ReturnsMatch`
- `Count_ReturnsCorrectTotal`
