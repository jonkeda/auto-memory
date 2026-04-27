# Step 03 — `SessionStateStore`: enumerate and query the flat-file store

## Goal

A single class that acts as the query layer over all `~/.copilot/session-state/<uuid>/` folders,
exposing the same logical operations as the SQLite path (`list`, `show`, `search`, `files`, `checkpoints`).

## New file: `net/src/AutoMemory.Core/VsCodeSessions/SessionStateStore.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoMemory.Core.VsCodeSessions;

/// <summary>
/// Reads Copilot VS Code session-state flat files from ~/.copilot/session-state/.
/// </summary>
public sealed class SessionStateStore
{
    private readonly string _root;

    public SessionStateStore(string root) => _root = root;

    /// <summary>Default root: ~/.copilot/session-state</summary>
    public static SessionStateStore Default() =>
        new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".copilot", "session-state"));

    /// <summary>True if the root directory exists and contains at least one session.</summary>
    public bool IsAvailable() =>
        Directory.Exists(_root) && Directory.GetDirectories(_root).Length > 0;

    /// <summary>Load all sessions, newest first, up to <paramref name="limit"/>.</summary>
    public IReadOnlyList<VsCodeSession> List(int limit = 10, int days = 30, string? repo = null)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
        return Directory
            .GetDirectories(_root)
            .Select(dir => TryParse(dir))
            .OfType<VsCodeSession>()
            .Where(s => s.CreatedAt >= cutoff)
            .Where(s => repo == null || string.Equals(s.Repository, repo, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToList();
    }

    /// <summary>Load a single session by full UUID or 8-char prefix.</summary>
    public VsCodeSession? Show(string id)
    {
        var dirs = Directory.GetDirectories(_root);
        var dir = dirs.FirstOrDefault(d =>
        {
            var name = Path.GetFileName(d);
            return name == id || name.StartsWith(id, StringComparison.OrdinalIgnoreCase);
        });
        return dir is null ? null : TryParse(dir);
    }

    /// <summary>Full-text search across summary + first user message of each session.</summary>
    public IReadOnlyList<SearchResult> Search(string query, int limit = 20)
    {
        var results = new List<SearchResult>();
        var pattern = new Regex(Regex.Escape(query), RegexOptions.IgnoreCase);

        foreach (var dir in Directory.GetDirectories(_root))
        {
            var session = TryParse(dir);
            if (session is null) continue;

            // Search summary
            if (session.Summary is not null && pattern.IsMatch(session.Summary))
            {
                results.Add(new SearchResult
                {
                    SessionId = session.Id,
                    Date      = session.CreatedAt.ToString("yyyy-MM-dd"),
                    Excerpt   = Truncate(session.Summary, 120),
                    Source    = "summary",
                });
                continue;
            }

            // Search first user message
            var firstMsg = EventsReader.UserMessages(dir).FirstOrDefault();
            if (firstMsg is not null && pattern.IsMatch(firstMsg))
            {
                results.Add(new SearchResult
                {
                    SessionId = session.Id,
                    Date      = session.CreatedAt.ToString("yyyy-MM-dd"),
                    Excerpt   = Truncate(firstMsg, 120),
                    Source    = "user.message",
                });
            }
        }

        return results
            .OrderByDescending(r => r.Date)
            .Take(limit)
            .ToList();
    }

    /// <summary>Recent files touched across all sessions (from tool arguments).</summary>
    public IReadOnlyList<FileResult> Files(int limit = 20, int days = 30)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
        var results = new List<FileResult>();

        foreach (var dir in Directory.GetDirectories(_root))
        {
            var session = TryParse(dir);
            if (session is null || session.CreatedAt < cutoff) continue;

            foreach (var path in EventsReader.TouchedFiles(dir).Distinct())
            {
                results.Add(new FileResult
                {
                    FilePath  = path,
                    SessionId = session.Id,
                    Date      = session.CreatedAt.ToString("yyyy-MM-dd"),
                });
            }
        }

        return results
            .OrderByDescending(r => r.Date)
            .Take(limit)
            .ToList();
    }

    private VsCodeSession? TryParse(string dir)
    {
        try { return WorkspaceYamlParser.Parse(dir); }
        catch { return null; }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

public sealed record SearchResult
{
    public required string SessionId { get; init; }
    public required string Date      { get; init; }
    public required string Excerpt   { get; init; }
    public required string Source    { get; init; }
}

public sealed record FileResult
{
    public required string FilePath  { get; init; }
    public required string SessionId { get; init; }
    public required string Date      { get; init; }
}
```

## Done when

- [ ] `List(limit:10, days:30)` returns the 10 most recent sessions
- [ ] `List(repo:"jonkeda/Oravey2")` filters by repository
- [ ] `Show("005540f5")` resolves by 8-char prefix
- [ ] `Search("RPG")` returns sessions whose summary or first user message contains the term
- [ ] `Files(limit:20)` returns file paths extracted from tool arguments
- [ ] `TryParse` silently skips directories without a valid `workspace.yaml`
