using System;
using AutoMemory.Core.Queries;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Queries;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class SessionQueriesTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public SessionQueriesTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateSchema();
        InsertTestData();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private void CreateSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE sessions (
                id TEXT PRIMARY KEY, cwd TEXT, repository TEXT, branch TEXT,
                summary TEXT, created_at TEXT, updated_at TEXT, host_type TEXT);
            
            CREATE TABLE turns (
                id INTEGER PRIMARY KEY, session_id TEXT, turn_index INTEGER,
                user_message TEXT, assistant_response TEXT, timestamp TEXT);
            
            CREATE TABLE session_files (
                id INTEGER PRIMARY KEY, session_id TEXT, file_path TEXT,
                tool_name TEXT, turn_index INTEGER, first_seen_at TEXT);
            """;
        cmd.ExecuteNonQuery();
    }

    private void InsertTestData()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sessions VALUES ('s1', '/tmp', 'owner/repo', 'main', 'Recent session', datetime('now'), datetime('now'), 'local');
            INSERT INTO sessions VALUES ('s2', '/tmp', 'owner/repo', 'main', 'Yesterday session', datetime('now', '-1 day'), datetime('now'), 'local');
            INSERT INTO sessions VALUES ('s3', '/tmp', 'other/repo', 'dev', 'Other repo', datetime('now'), datetime('now'), 'local');
            INSERT INTO sessions VALUES ('s4', '/tmp', 'owner/repo', 'main', 'Old session', datetime('now', '-100 days'), datetime('now'), 'local');
            
            INSERT INTO turns VALUES (1, 's1', 0, 'hello', 'hi', datetime('now'));
            INSERT INTO turns VALUES (2, 's1', 1, 'q2', 'a2', datetime('now'));
            INSERT INTO turns VALUES (3, 's2', 0, 'test', 'response', datetime('now', '-1 day'));
            
            INSERT INTO session_files VALUES (1, 's1', '/tmp/file1.md', 'create_file', 0, datetime('now'));
            INSERT INTO session_files VALUES (2, 's1', '/tmp/file2.md', 'edit_file', 1, datetime('now'));
            INSERT INTO session_files VALUES (3, 's2', '/tmp/file3.md', 'read_file', 0, datetime('now', '-1 day'));
            """;
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void SelectRecentSessions_WithRepo_FiltersCorrectly()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, "owner/repo", 10, 30);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
            var repo = reader.GetString(reader.GetOrdinal("repository"));
            Assert.Equal("owner/repo", repo);
        }

        Assert.Equal(2, count); // s1 and s2, not s4 (too old)
    }

    [Fact]
    public void SelectRecentSessions_AllRepos_ReturnsMultipleRepos()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, null, 10, 30);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        var repos = new System.Collections.Generic.HashSet<string>();
        while (reader.Read())
        {
            count++;
            repos.Add(reader.GetString(reader.GetOrdinal("repository")));
        }

        Assert.Equal(3, count); // s1, s2, s3
        Assert.Contains("owner/repo", repos);
        Assert.Contains("other/repo", repos);
    }

    [Fact]
    public void SelectRecentSessions_IncludesTurnCount()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, "owner/repo", 10, 30);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var turnsCount = reader.GetInt32(reader.GetOrdinal("turns_count"));
        Assert.Equal(2, turnsCount); // s1 has 2 turns
    }

    [Fact]
    public void SelectRecentSessions_IncludesFileCount()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, "owner/repo", 10, 30);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var filesCount = reader.GetInt32(reader.GetOrdinal("files_count"));
        Assert.Equal(2, filesCount); // s1 has 2 files
    }

    [Fact]
    public void SelectRecentSessions_RespectsLimit()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, null, 1, 30);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void SelectRecentSessions_RespectsDaysFilter()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, "owner/repo", 10, 0);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        // Only s1 should match (created today), not s2 (yesterday)
        Assert.Equal(1, count);
    }

    [Fact]
    public void SelectRecentSessions_OrdersByCreatedAtDesc()
    {
        using var cmd = SessionQueries.SelectRecentSessions(_connection, "owner/repo", 10, 30);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var firstId = reader.GetString(reader.GetOrdinal("id"));
        Assert.Equal("s1", firstId); // s1 is more recent than s2

        Assert.True(reader.Read());
        var secondId = reader.GetString(reader.GetOrdinal("id"));
        Assert.Equal("s2", secondId);
    }
}
