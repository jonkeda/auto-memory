using System;
using System.Collections.Generic;
using System.Linq;
using AutoMemory.Core.VsCodeChat;
using AutoMemory.Core.VsCodeSessions;

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

        var allTuples = stateSessions
            .Select(s => (Repo: s.Repository, Summary: s.Summary, UpdatedAt: s.UpdatedAt))
            .Concat(chatSessions
                .Select(s => (Repo: s.Repository, Summary: s.Summary, UpdatedAt: s.UpdatedAt ?? s.CreatedAt)))
            .ToList();

        return CollectFrom(allTuples, stateSessions.Count, chatSessions.Count, currentRepo);
    }

    internal static FlatHealthContext CollectFrom(
        IReadOnlyList<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)> sessions,
        int sessionStateCount,
        int chatCount,
        string? currentRepo)
    {
        var cutoff7d = DateTimeOffset.UtcNow.AddDays(-7);

        return new FlatHealthContext
        {
            TotalSessions        = sessions.Count,
            SessionStateSessions = sessionStateCount,
            ChatSessions         = chatCount,
            RepoSessions         = sessions.Count(s =>
                                       currentRepo is not null &&
                                       string.Equals(s.Repo, currentRepo, StringComparison.OrdinalIgnoreCase)),
            SummaryCount         = sessions.Count(s => !string.IsNullOrWhiteSpace(s.Summary)),
            RecentCount          = sessions.Count(s => s.UpdatedAt >= cutoff7d),
            NewestUpdatedAt      = sessions.Count > 0
                                       ? sessions.Max(s => s.UpdatedAt)
                                       : null,
            CurrentRepo          = currentRepo,
        };
    }
}
