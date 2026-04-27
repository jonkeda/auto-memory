using System;
using System.Collections.Generic;
using AutoMemory.Core.Health;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public class FlatHealthContextTests
{
    [Fact]
    public void Collect_TotalCount_IsSumOfBothBackends()
    {
        // Arrange: 3 session-state + 2 chat = 5 total
        var sessions = new List<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)>
        {
            ("owner/repo1", "Summary 1", DateTimeOffset.UtcNow.AddDays(-1)),
            ("owner/repo2", "Summary 2", DateTimeOffset.UtcNow.AddDays(-2)),
            ("owner/repo3", "Summary 3", DateTimeOffset.UtcNow.AddDays(-3)),
            ("owner/repo4", "Summary 4", DateTimeOffset.UtcNow.AddDays(-4)),
            ("owner/repo5", "Summary 5", DateTimeOffset.UtcNow.AddDays(-5)),
        };

        // Act
        var ctx = FlatHealthContext.CollectFrom(sessions, sessionStateCount: 3, chatCount: 2, currentRepo: null);

        // Assert
        Assert.Equal(5, ctx.TotalSessions);
        Assert.Equal(3, ctx.SessionStateSessions);
        Assert.Equal(2, ctx.ChatSessions);
    }

    [Fact]
    public void Collect_RecentCount_OnlyLast7Days()
    {
        // Arrange: 3 sessions within 7 days, 2 old sessions
        var sessions = new List<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)>
        {
            ("owner/repo1", "Recent 1", DateTimeOffset.UtcNow.AddDays(-1)),
            ("owner/repo2", "Recent 2", DateTimeOffset.UtcNow.AddDays(-3)),
            ("owner/repo3", "Recent 3", DateTimeOffset.UtcNow.AddDays(-6)),
            ("owner/repo4", "Old 1",    DateTimeOffset.UtcNow.AddDays(-10)),
            ("owner/repo5", "Old 2",    DateTimeOffset.UtcNow.AddDays(-30)),
        };

        // Act
        var ctx = FlatHealthContext.CollectFrom(sessions, sessionStateCount: 3, chatCount: 2, currentRepo: null);

        // Assert
        Assert.Equal(3, ctx.RecentCount);
        Assert.Equal(5, ctx.TotalSessions);
    }

    [Fact]
    public void Collect_SummaryCount_OnlyNonEmpty()
    {
        // Arrange: 3 with summary, 2 without (null or empty)
        var sessions = new List<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)>
        {
            ("owner/repo1", "Summary 1",  DateTimeOffset.UtcNow.AddDays(-1)),
            ("owner/repo2", "Summary 2",  DateTimeOffset.UtcNow.AddDays(-2)),
            ("owner/repo3", "Summary 3",  DateTimeOffset.UtcNow.AddDays(-3)),
            ("owner/repo4", null,         DateTimeOffset.UtcNow.AddDays(-4)),
            ("owner/repo5", "",           DateTimeOffset.UtcNow.AddDays(-5)),
        };

        // Act
        var ctx = FlatHealthContext.CollectFrom(sessions, sessionStateCount: 3, chatCount: 2, currentRepo: null);

        // Assert
        Assert.Equal(3, ctx.SummaryCount);
        Assert.Equal(5, ctx.TotalSessions);
    }

    [Fact]
    public void Collect_RepoSessions_MatchesCurrentRepo()
    {
        // Arrange: 2 sessions matching "owner/myrepo", 3 others
        var sessions = new List<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)>
        {
            ("owner/myrepo", "Summary 1", DateTimeOffset.UtcNow.AddDays(-1)),
            ("owner/other",  "Summary 2", DateTimeOffset.UtcNow.AddDays(-2)),
            ("owner/myrepo", "Summary 3", DateTimeOffset.UtcNow.AddDays(-3)),
            ("owner/xyz",    "Summary 4", DateTimeOffset.UtcNow.AddDays(-4)),
            (null,           "Summary 5", DateTimeOffset.UtcNow.AddDays(-5)),
        };

        // Act
        var ctx = FlatHealthContext.CollectFrom(sessions, sessionStateCount: 3, chatCount: 2, currentRepo: "owner/myrepo");

        // Assert
        Assert.Equal(2, ctx.RepoSessions);
        Assert.Equal(5, ctx.TotalSessions);
        Assert.Equal("owner/myrepo", ctx.CurrentRepo);
    }

    [Fact]
    public void Collect_NewestUpdatedAt_ReturnsMax()
    {
        // Arrange
        var newest = DateTimeOffset.UtcNow.AddDays(-1);
        var sessions = new List<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)>
        {
            ("owner/repo1", "S1", DateTimeOffset.UtcNow.AddDays(-5)),
            ("owner/repo2", "S2", newest),
            ("owner/repo3", "S3", DateTimeOffset.UtcNow.AddDays(-10)),
        };

        // Act
        var ctx = FlatHealthContext.CollectFrom(sessions, sessionStateCount: 2, chatCount: 1, currentRepo: null);

        // Assert
        Assert.Equal(newest, ctx.NewestUpdatedAt);
    }

    [Fact]
    public void Collect_EmptySessions_NewestUpdatedAtIsNull()
    {
        // Arrange
        var sessions = new List<(string? Repo, string? Summary, DateTimeOffset UpdatedAt)>();

        // Act
        var ctx = FlatHealthContext.CollectFrom(sessions, sessionStateCount: 0, chatCount: 0, currentRepo: null);

        // Assert
        Assert.Null(ctx.NewestUpdatedAt);
        Assert.Equal(0, ctx.TotalSessions);
    }
}
