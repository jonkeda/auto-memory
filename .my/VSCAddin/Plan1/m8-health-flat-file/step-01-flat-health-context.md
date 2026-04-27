# Step 01 — `FlatHealthContext`: aggregate stats from both flat-file stores

## Goal

Create `FlatHealthContext` in `AutoMemory.Core` that reads from both:
- `SessionStateStore` (M6) — `~/.copilot/session-state/`
- `ChatSessionStore` (M7) — `workspaceStorage/*/GitHub.copilot-chat/transcripts/`

And computes the aggregate statistics needed by the flat-file health dimensions.

## Location

`net/src/AutoMemory.Core/Health/FlatHealthContext.cs`

## Implementation

```csharp
namespace AutoMemory.Core.Health;

/// <summary>
/// Aggregated statistics from all flat-file session backends.
/// Computed once; used by all flat-file health dimensions.
/// </summary>
public sealed class FlatHealthContext
{
    public int    TotalSessions       { get; init; }
    public int    SessionStateSessions { get; init; }
    public int    ChatSessions         { get; init; }
    public int    RepoSessions         { get; init; }   // matching current repo
    public int    SummaryCount         { get; init; }   // sessions with non-empty summary
    public int    RecentCount          { get; init; }   // updated in last 7 days
    public DateTimeOffset? NewestUpdatedAt { get; init; }
    public string? CurrentRepo         { get; init; }

    public static FlatHealthContext Collect(string? currentRepo = null)
    {
        var stateStore = SessionStateStore.Default();
        var chatStore  = ChatSessionStore.Default();

        // Load all sessions without limit (days=null, repo=null)
        var stateSessions = stateStore.IsAvailable()
            ? stateStore.List(limit: int.MaxValue)
            : [];

        var chatSessions = chatStore.IsAvailable()
            ? chatStore.List(limit: int.MaxValue)
            : [];

        var allSessions = stateSessions
            .Select(s => (Id: s.Id, Repo: s.Repository, Summary: s.Summary, UpdatedAt: s.UpdatedAt ?? s.CreatedAt))
            .Concat(chatSessions
                .Select(s => (Id: s.Id, Repo: s.Repository, Summary: s.Summary, UpdatedAt: s.UpdatedAt ?? s.CreatedAt)))
            .ToList();

        var cutoff7d = DateTimeOffset.UtcNow.AddDays(-7);

        return new FlatHealthContext
        {
            TotalSessions        = allSessions.Count,
            SessionStateSessions = stateSessions.Count,
            ChatSessions         = chatSessions.Count,
            RepoSessions         = allSessions.Count(s =>
                                       currentRepo is not null &&
                                       string.Equals(s.Repo, currentRepo, StringComparison.OrdinalIgnoreCase)),
            SummaryCount         = allSessions.Count(s => !string.IsNullOrWhiteSpace(s.Summary)),
            RecentCount          = allSessions.Count(s => s.UpdatedAt >= cutoff7d),
            NewestUpdatedAt      = allSessions.Count > 0
                                       ? allSessions.Max(s => s.UpdatedAt)
                                       : null,
            CurrentRepo          = currentRepo,
        };
    }
}
```

## Tests

`AutoMemory.Tests/Health/FlatHealthContextTests.cs`

Use fixture sessions from M6 (`net/tests/fixtures/session-state/`) and M7 (`net/tests/fixtures/chat-transcripts/`).

- `Collect_WithFixtures_ReturnsCorrectTotalCount`
- `Collect_RecentCount_OnlyCountsLast7Days` (need at least one old fixture session)
- `Collect_SummaryCount_OnlyNonEmptySummaries`
- `Collect_RepoSessions_MatchesFilter`
