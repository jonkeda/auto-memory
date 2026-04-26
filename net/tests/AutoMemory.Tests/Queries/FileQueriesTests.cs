using System;
using AutoMemory.Core.Queries;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Queries;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class FileQueriesTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public FileQueriesTests()
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
            INSERT INTO sessions VALUES ('s1', '/tmp', 'owner/repo', 'main', 'Session 1', datetime('now'), datetime('now'), 'local');
            INSERT INTO sessions VALUES ('s2', '/tmp', 'other/repo', 'dev', 'Session 2', datetime('now'), datetime('now'), 'local');
            
            INSERT INTO session_files VALUES (1, 's1', '/tmp/file1.md', 'create_file', 0, datetime('now'));
            INSERT INTO session_files VALUES (2, 's1', '/tmp/file2.md', 'edit_file', 1, datetime('now', '-1 day'));
            INSERT INTO session_files VALUES (3, 's2', '/tmp/file3.md', 'read_file', 0, datetime('now'));
            INSERT INTO session_files VALUES (4, 's1', '/tmp/file4.md', 'create_file', 2, datetime('now', '-10 days'));
            """;
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void SelectRecentFiles_WithRepo_FiltersCorrectly()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, "owner/repo", 10, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
            var summary = reader.GetString(reader.GetOrdinal("summary"));
            Assert.Equal("Session 1", summary);
        }

        Assert.Equal(3, count); // file1, file2, file4 from owner/repo
    }

    [Fact]
    public void SelectRecentFiles_AllRepos_ReturnsMultipleRepos()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, null, 10, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(4, count); // All files
    }

    [Fact]
    public void SelectRecentFiles_RespectsLimit()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, null, 2, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void SelectRecentFiles_RespectsDaysFilter()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, null, 10, 5);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        // Should get file1 (today), file2 (-1 day), file3 (today), but not file4 (-10 days)
        Assert.Equal(3, count);
    }

    [Fact]
    public void SelectRecentFiles_NullDays_NoDateFilter()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, null, 10, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(4, count); // All files including old ones
    }

    [Fact]
    public void SelectRecentFiles_OrdersByFirstSeenAtDesc()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, "owner/repo", 10, null);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var firstFile = reader.GetString(reader.GetOrdinal("file_path"));
        Assert.Equal("/tmp/file1.md", firstFile); // Most recent

        Assert.True(reader.Read());
        var secondFile = reader.GetString(reader.GetOrdinal("file_path"));
        Assert.Equal("/tmp/file2.md", secondFile);

        Assert.True(reader.Read());
        var thirdFile = reader.GetString(reader.GetOrdinal("file_path"));
        Assert.Equal("/tmp/file4.md", thirdFile); // Oldest
    }

    [Fact]
    public void SelectRecentFiles_IncludesToolName()
    {
        using var cmd = FileQueries.SelectRecentFiles(_connection, "owner/repo", 1, null);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var toolName = reader.GetString(reader.GetOrdinal("tool_name"));
        Assert.Equal("create_file", toolName);
    }
}
