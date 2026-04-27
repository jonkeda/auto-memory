using AutoMemory.Core.VsCodeChat;

namespace AutoMemory.Tests.VsCodeChat;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class ChatSessionStoreTests
{
    private static string FixtureRoot => Path.Combine(
        AppContext.BaseDirectory,
        "fixtures", "chat-transcripts");

    private static ChatSessionStore CreateStore()
    {
        var ws1 = new ChatWorkspace
        {
            Hash = "ws-hash-0001",
            WorkspacePath = "e:/repos/Owner/RepoA",
            TranscriptDir = Path.Combine(FixtureRoot, "ws-hash-0001", "GitHub.copilot-chat", "transcripts")
        };

        var ws2 = new ChatWorkspace
        {
            Hash = "ws-hash-0002",
            WorkspacePath = "e:/repos/Owner/RepoB",
            TranscriptDir = Path.Combine(FixtureRoot, "ws-hash-0002", "GitHub.copilot-chat", "transcripts")
        };

        return ChatSessionStore.FromWorkspaces(new[] { ws1, ws2 });
    }

    [Fact]
    public void Count_ReturnsCorrectTotal()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var count = store.Count();

        // Assert
        Assert.Equal(3, count); // session-0001, session-0002, session-0003
    }

    [Fact]
    public void List_ReturnsAllSessions_WhenNoFilter()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var sessions = store.List(limit: 100);

        // Assert
        Assert.Equal(3, sessions.Count);
        Assert.Contains(sessions, s => s.Id == "session-0001");
        Assert.Contains(sessions, s => s.Id == "session-0002");
        Assert.Contains(sessions, s => s.Id == "session-0003");
    }

    [Fact]
    public void List_LimitIsRespected()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var sessions = store.List(limit: 2);

        // Assert
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void List_OrderedByCreatedAtDesc()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var sessions = store.List(limit: 100);

        // Assert
        Assert.Equal(3, sessions.Count);
        // session-0002 (2026-04-25T14:30) should be first
        Assert.Equal("session-0002", sessions[0].Id);
        // session-0001 (2026-04-25T10:00) should be second
        Assert.Equal("session-0001", sessions[1].Id);
        // session-0003 (2026-01-01T08:00) should be last
        Assert.Equal("session-0003", sessions[2].Id);
    }

    [Fact]
    public void List_DaysFilter_ExcludesOldSessions()
    {
        // Arrange
        var store = CreateStore();

        // Act - only sessions from last 30 days (excludes session-0003 from 2026-01-01)
        var sessions = store.List(limit: 100, days: 30);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, s => s.Id == "session-0001");
        Assert.Contains(sessions, s => s.Id == "session-0002");
        Assert.DoesNotContain(sessions, s => s.Id == "session-0003");
    }

    [Fact]
    public void List_RepoFilter_OnlyMatchingSessions()
    {
        // Arrange
        var store = CreateStore();

        // Act - filter by RepoA
        var sessions = store.List(limit: 100, repo: "RepoA");

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, s => Assert.Equal("RepoA", s.Repository));
    }

    [Fact]
    public void Show_ByFullId_ReturnsSession()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var session = store.Show("session-0001");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("session-0001", session.Id);
        Assert.Equal(3, session.TurnCount);
        Assert.Equal(2, session.ToolCount);
        Assert.Equal("Read the README file and summarize it", session.Summary);
    }

    [Fact]
    public void Show_ByShortPrefix_ReturnsSession()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var session = store.Show("session-000");

        // Assert
        Assert.NotNull(session);
        // Should match the first one found (implementation detail: depends on enumeration order)
        Assert.StartsWith("session-000", session.Id);
    }

    [Fact]
    public void Show_UnknownId_ReturnsNull()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var session = store.Show("nonexistent-session");

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void Search_ByKeywordInSummary_ReturnsMatch()
    {
        // Arrange
        var store = CreateStore();

        // Act - search for "README" which appears in session-0001's summary
        var sessions = store.Search("README", limit: 10);

        // Assert
        Assert.Single(sessions);
        Assert.Equal("session-0001", sessions[0].Id);
    }

    [Fact]
    public void Search_ByKeywordInWorkspacePath_ReturnsMatch()
    {
        // Arrange
        var store = CreateStore();

        // Act - search for "RepoB" which appears in workspace path
        var sessions = store.Search("RepoB", limit: 10);

        // Assert
        Assert.Single(sessions);
        Assert.Equal("session-0003", sessions[0].Id);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        // Arrange
        var store = CreateStore();

        // Act - search with different casing
        var sessions = store.Search("readme", limit: 10);

        // Assert
        Assert.Single(sessions);
        Assert.Equal("session-0001", sessions[0].Id);
    }

    [Fact]
    public void IsAvailable_ReturnsTrueWhenWorkspacesExist()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var available = store.IsAvailable();

        // Assert
        Assert.True(available);
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenNoWorkspaces()
    {
        // Arrange
        var store = ChatSessionStore.FromWorkspaces(Array.Empty<ChatWorkspace>());

        // Act
        var available = store.IsAvailable();

        // Assert
        Assert.False(available);
    }
}
