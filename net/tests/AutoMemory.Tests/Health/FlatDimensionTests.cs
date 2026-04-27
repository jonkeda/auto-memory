using System;
using AutoMemory.Core.Health;
using AutoMemory.Core.Health.Flat;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class FlatDimensionTests
{
    [Fact]
    public void DimFlatFreshness_OldSession_ReturnsRed()
    {
        var ctx = new FlatHealthContext
        {
            NewestUpdatedAt = DateTimeOffset.UtcNow.AddDays(-8),
            TotalSessions = 10
        };

        var dim = new DimFlatFreshness();
        var result = dim.Score(ctx);

        Assert.Equal("Freshness", result.Name);
        Assert.Equal(2.0, result.Score);
        Assert.Equal("RED", result.Zone);
        Assert.Contains("ago", result.Detail);
    }

    [Fact]
    public void DimFlatFreshness_RecentSession_ReturnsGreen()
    {
        var ctx = new FlatHealthContext
        {
            NewestUpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            TotalSessions = 10
        };

        var dim = new DimFlatFreshness();
        var result = dim.Score(ctx);

        Assert.Equal("Freshness", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Zone);
        Assert.Contains("h ago", result.Detail);
    }

    [Fact]
    public void DimFlatCorpus_100Sessions_Returns10()
    {
        var ctx = new FlatHealthContext
        {
            TotalSessions = 100,
            SessionStateSessions = 40,
            ChatSessions = 60
        };

        var dim = new DimFlatCorpus();
        var result = dim.Score(ctx);

        Assert.Equal("Corpus Size", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Zone);
        Assert.Contains("100 sessions", result.Detail);
        Assert.Contains("40 state", result.Detail);
        Assert.Contains("60 chat", result.Detail);
    }

    [Fact]
    public void DimFlatCorpus_NoSessions_Returns2()
    {
        var ctx = new FlatHealthContext
        {
            TotalSessions = 2,
            SessionStateSessions = 1,
            ChatSessions = 1
        };

        var dim = new DimFlatCorpus();
        var result = dim.Score(ctx);

        Assert.Equal("Corpus Size", result.Name);
        Assert.Equal(2.0, result.Score);
        Assert.Equal("RED", result.Zone);
    }

    [Fact]
    public void DimFlatRepoCoverage_NoCurrentRepo_ReturnsCalibrating()
    {
        var ctx = new FlatHealthContext
        {
            CurrentRepo = null,
            TotalSessions = 10
        };

        var dim = new DimFlatRepoCoverage();
        var result = dim.Score(ctx);

        Assert.Equal("Repo Coverage", result.Name);
        Assert.Null(result.Score);
        Assert.Equal("CALIBRATING", result.Zone);
        Assert.Contains("no current repo", result.Detail);
    }

    [Fact]
    public void DimFlatRepoCoverage_5Matches_ReturnsGreen()
    {
        var ctx = new FlatHealthContext
        {
            CurrentRepo = "jonkeda/auto-memory",
            RepoSessions = 5,
            TotalSessions = 10
        };

        var dim = new DimFlatRepoCoverage();
        var result = dim.Score(ctx);

        Assert.Equal("Repo Coverage", result.Name);
        Assert.Equal(8.0, result.Score);
        Assert.Equal("GREEN", result.Zone);
        Assert.Contains("5 sessions", result.Detail);
        Assert.Contains("jonkeda/auto-memory", result.Detail);
    }

    [Fact]
    public void DimFlatSummaryCoverage_AllHaveSummary_Returns10()
    {
        var ctx = new FlatHealthContext
        {
            TotalSessions = 100,
            SummaryCount = 100
        };

        var dim = new DimFlatSummaryCoverage();
        var result = dim.Score(ctx);

        Assert.Equal("Summary Coverage", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Zone);
        Assert.Contains("100%", result.Detail);
        Assert.Contains("(100/100)", result.Detail);
    }

    [Fact]
    public void DimFlatSummaryCoverage_NoneSummary_Returns2()
    {
        var ctx = new FlatHealthContext
        {
            TotalSessions = 100,
            SummaryCount = 10
        };

        var dim = new DimFlatSummaryCoverage();
        var result = dim.Score(ctx);

        Assert.Equal("Summary Coverage", result.Name);
        Assert.Equal(2.0, result.Score);
        Assert.Equal("RED", result.Zone);
        Assert.Contains("10%", result.Detail);
    }

    [Fact]
    public void DimFlatRecentActivity_ManyRecent_ReturnsGreen()
    {
        var ctx = new FlatHealthContext
        {
            RecentCount = 25,
            TotalSessions = 100
        };

        var dim = new DimFlatRecentActivity();
        var result = dim.Score(ctx);

        Assert.Equal("Recent Activity", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Zone);
        Assert.Contains("25 sessions", result.Detail);
        Assert.Contains("last 7d", result.Detail);
    }

    [Fact]
    public void DimFlatRecentActivity_NoneRecent_ReturnsRed()
    {
        var ctx = new FlatHealthContext
        {
            RecentCount = 0,
            TotalSessions = 100
        };

        var dim = new DimFlatRecentActivity();
        var result = dim.Score(ctx);

        Assert.Equal("Recent Activity", result.Name);
        Assert.Equal(2.0, result.Score);
        Assert.Equal("RED", result.Zone);
        Assert.Contains("0 sessions", result.Detail);
    }
}
