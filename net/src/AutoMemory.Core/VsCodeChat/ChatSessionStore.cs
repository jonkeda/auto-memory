namespace AutoMemory.Core.VsCodeChat;

/// <summary>
/// Store for discovering and accessing VS Code Copilot Chat sessions
/// across all workspace storage folders.
/// </summary>
public sealed class ChatSessionStore
{
    private readonly IReadOnlyList<ChatWorkspace> _workspaces;

    private ChatSessionStore(IReadOnlyList<ChatWorkspace> workspaces)
        => _workspaces = workspaces;

    /// <summary>
    /// Creates a store that scans the default VS Code workspace storage locations.
    /// </summary>
    public static ChatSessionStore Default()
    {
        var ws = WorkspaceStorageLocator.FindWorkspaces().ToList();
        return new ChatSessionStore(ws);
    }

    /// <summary>
    /// Creates a store from an explicit list of workspaces (for testing).
    /// </summary>
    internal static ChatSessionStore FromWorkspaces(IReadOnlyList<ChatWorkspace> workspaces)
        => new(workspaces);

    /// <summary>
    /// Returns true if any workspace with transcripts is found.
    /// </summary>
    public bool IsAvailable() => _workspaces.Count > 0;

    /// <summary>
    /// Returns the total session count across all workspaces.
    /// </summary>
    public int Count()
    {
        int n = 0;
        foreach (var ws in _workspaces)
            n += Directory.GetFiles(ws.TranscriptDir, "*.jsonl").Length;
        return n;
    }

    /// <summary>
    /// Lists sessions sorted by creation date (newest first).
    /// </summary>
    /// <param name="limit">Maximum number of sessions to return.</param>
    /// <param name="days">Optional filter to include only sessions created in the last N days.</param>
    /// <param name="repo">Optional filter to include only sessions from a specific repository.</param>
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

    /// <summary>
    /// Shows a single session by ID prefix.
    /// </summary>
    /// <param name="idPrefix">Session ID or prefix to match against filename stems.</param>
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

    /// <summary>
    /// Searches sessions by keyword in summary, workspace path, or repository.
    /// </summary>
    /// <param name="query">Search keyword (case-insensitive).</param>
    /// <param name="limit">Maximum number of results to return.</param>
    public IReadOnlyList<ChatSession> Search(string query, int limit = 10)
    {
        var results = new List<ChatSession>();

        foreach (var ws in _workspaces)
        {
            foreach (var file in Directory.EnumerateFiles(ws.TranscriptDir, "*.jsonl"))
            {
                var s = TryLoad(file, ws);
                if (s is null) continue;
                if ((s.Summary?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.WorkspacePath?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Repository?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
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
