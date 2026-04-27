using System;
using System.IO;
using System.Linq;
using AutoMemory.Core.VsCodeSessions;
using Xunit;

namespace AutoMemory.Tests.VsCodeSessions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class SessionStateStoreTests
{
    private static string FixtureRoot()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "session-state");
        if (!Directory.Exists(basePath))
            throw new DirectoryNotFoundException($"Fixture root not found: {basePath}");
        return basePath;
    }

    [Fact]
    public void List_ReturnsBothSessions()
    {
        var store = new SessionStateStore(FixtureRoot());
        var sessions = store.List(limit: 10, days: 3650);
        
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void List_FilterByRepo()
    {
        var store = new SessionStateStore(FixtureRoot());
        var sessions = store.List(repo: "owner/my-project", days: 3650);
        
        Assert.Single(sessions);
        Assert.Equal("owner/my-project", sessions[0].Repository);
    }

    [Fact]
    public void List_NewestFirst()
    {
        var store = new SessionStateStore(FixtureRoot());
        var sessions = store.List(days: 3650);
        
        // Session 1 created on 2026-01-15, session 2 on 2026-01-10
        Assert.True(sessions[0].CreatedAt >= sessions[1].CreatedAt);
        Assert.Equal("a1b2c3d4-0000-0000-0000-000000000001", sessions[0].Id);
    }

    [Fact]
    public void Show_ByPrefix_Found()
    {
        var store = new SessionStateStore(FixtureRoot());
        var s = store.Show("a1b2c3d4");
        
        Assert.NotNull(s);
        Assert.StartsWith("a1b2c3d4", s.Id);
    }

    [Fact]
    public void Show_FullId_Found()
    {
        var store = new SessionStateStore(FixtureRoot());
        var s = store.Show("a1b2c3d4-0000-0000-0000-000000000001");
        
        Assert.NotNull(s);
        Assert.Equal("a1b2c3d4-0000-0000-0000-000000000001", s.Id);
    }

    [Fact]
    public void Show_NonExistent_ReturnsNull()
    {
        var store = new SessionStateStore(FixtureRoot());
        var s = store.Show("nonexistent-id");
        
        Assert.Null(s);
    }

    [Fact]
    public void Search_MatchesSummary()
    {
        var store = new SessionStateStore(FixtureRoot());
        var results = store.Search("login");
        
        Assert.Contains(results, r => r.SessionId.StartsWith("a1b2c3d4-0000-0000-0000-000000000001"));
        Assert.Contains(results, r => r.Source == "summary");
    }

    [Fact]
    public void Search_MatchesUserMessage()
    {
        var store = new SessionStateStore(FixtureRoot());
        var results = store.Search("implement");
        
        // "Please implement the login page" is in the user message
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Search_NoMatches_ReturnsEmpty()
    {
        var store = new SessionStateStore(FixtureRoot());
        var results = store.Search("zyxwvutsrqponmlk");
        
        Assert.Empty(results);
    }

    [Fact]
    public void Files_ReturnsTouchedFiles()
    {
        var store = new SessionStateStore(FixtureRoot());
        var files = store.Files(limit: 20, days: 3650);
        
        Assert.NotEmpty(files);
        Assert.Contains(files, f => f.FilePath.Contains("Login.tsx"));
    }

    [Fact]
    public void IsAvailable_WithFixtures_ReturnsTrue()
    {
        var store = new SessionStateStore(FixtureRoot());
        
        Assert.True(store.IsAvailable());
    }

    [Fact]
    public void IsAvailable_NonExistentDir_ReturnsFalse()
    {
        var store = new SessionStateStore(Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}"));
        
        Assert.False(store.IsAvailable());
    }
}
