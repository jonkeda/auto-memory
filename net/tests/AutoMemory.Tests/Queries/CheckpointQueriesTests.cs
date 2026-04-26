using System;
using AutoMemory.Core.Queries;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Queries;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class CheckpointQueriesTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CheckpointQueriesTests()
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
            
            CREATE TABLE checkpoints (
                id INTEGER PRIMARY KEY, session_id TEXT, checkpoint_number INTEGER,
                title TEXT, overview TEXT, created_at TEXT);
            """;
        cmd.ExecuteNonQuery();
    }

    private void InsertTestData()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sessions VALUES ('s1', '/tmp', 'owner/repo', 'main', 'Session 1', datetime('now'), datetime('now'), 'local');
            INSERT INTO sessions VALUES ('s2', '/tmp', 'other/repo', 'dev', 'Session 2', datetime('now'), datetime('now'), 'local');
            
            INSERT INTO checkpoints VALUES (1, 's1', 1, 'Checkpoint 1', 'First checkpoint', datetime('now'));
            INSERT INTO checkpoints VALUES (2, 's1', 2, 'Checkpoint 2', 'Second checkpoint', datetime('now', '-1 day'));
            INSERT INTO checkpoints VALUES (3, 's2', 1, 'Other checkpoint', 'From other repo', datetime('now'));
            INSERT INTO checkpoints VALUES (4, 's1', 3, 'Old checkpoint', 'Too old', datetime('now', '-15 days'));
            """;
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void SelectRecentCheckpoints_WithRepo_FiltersCorrectly()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, "owner/repo", 10, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
            var summary = reader.GetString(reader.GetOrdinal("session_summary"));
            Assert.Equal("Session 1", summary);
        }

        Assert.Equal(3, count); // cp1, cp2, cp4 from owner/repo
    }

    [Fact]
    public void SelectRecentCheckpoints_AllRepos_ReturnsMultipleRepos()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, null, 10, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(4, count); // All checkpoints
    }

    [Fact]
    public void SelectRecentCheckpoints_RespectsLimit()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, null, 2, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void SelectRecentCheckpoints_RespectsDaysFilter()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, null, 10, 7);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        // Should get cp1 (today), cp2 (-1 day), cp3 (today), but not cp4 (-15 days)
        Assert.Equal(3, count);
    }

    [Fact]
    public void SelectRecentCheckpoints_NullDays_NoDateFilter()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, null, 10, null);
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        Assert.Equal(4, count); // All checkpoints including old ones
    }

    [Fact]
    public void SelectRecentCheckpoints_OrdersByCreatedAtDesc()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, "owner/repo", 10, null);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var firstTitle = reader.GetString(reader.GetOrdinal("title"));
        Assert.Equal("Checkpoint 1", firstTitle); // Most recent

        Assert.True(reader.Read());
        var secondTitle = reader.GetString(reader.GetOrdinal("title"));
        Assert.Equal("Checkpoint 2", secondTitle);

        Assert.True(reader.Read());
        var thirdTitle = reader.GetString(reader.GetOrdinal("title"));
        Assert.Equal("Old checkpoint", thirdTitle); // Oldest
    }

    [Fact]
    public void SelectRecentCheckpoints_IncludesCheckpointNumber()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, "owner/repo", 1, null);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var checkpointNumber = reader.GetInt32(reader.GetOrdinal("checkpoint_number"));
        Assert.Equal(1, checkpointNumber);
    }

    [Fact]
    public void SelectRecentCheckpoints_IncludesOverview()
    {
        using var cmd = CheckpointQueries.SelectRecentCheckpoints(_connection, "owner/repo", 1, null);
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        var overview = reader.GetString(reader.GetOrdinal("overview"));
        Assert.Equal("First checkpoint", overview);
    }
}
