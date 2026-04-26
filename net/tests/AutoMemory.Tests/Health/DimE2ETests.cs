using System;
using System.Collections.Generic;
using System.IO;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimE2ETests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private string CreateDbWithSessionsAndTurns(int sessionCount, int turnsPerSession)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.Delete(path); // GetTempFileName creates the file, we need it not to exist

        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();

            using var createSessionsCmd = conn.CreateCommand();
            createSessionsCmd.CommandText = @"
                CREATE TABLE sessions (
                    id TEXT PRIMARY KEY,
                    repository TEXT,
                    branch TEXT,
                    summary TEXT,
                    created_at TEXT,
                    updated_at TEXT
                )";
            createSessionsCmd.ExecuteNonQuery();

            using var createTurnsCmd = conn.CreateCommand();
            createTurnsCmd.CommandText = @"
                CREATE TABLE turns (
                    id TEXT PRIMARY KEY,
                    session_id TEXT,
                    turn_index INTEGER,
                    request TEXT,
                    response TEXT,
                    created_at TEXT,
                    FOREIGN KEY (session_id) REFERENCES sessions(id)
                )";
            createTurnsCmd.ExecuteNonQuery();

            // Insert sessions
            for (int i = 0; i < sessionCount; i++)
            {
                var sessionId = $"session-{i:D5}";
                using var insertSessionCmd = conn.CreateCommand();
                insertSessionCmd.CommandText = "INSERT INTO sessions (id, repository, branch, summary, created_at) VALUES (@id, 'repo', 'main', 'Test session', @ts)";
                insertSessionCmd.Parameters.AddWithValue("@id", sessionId);
                insertSessionCmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.AddMinutes(-i).ToString("o"));
                insertSessionCmd.ExecuteNonQuery();

                // Insert turns for this session
                for (int j = 0; j < turnsPerSession; j++)
                {
                    using var insertTurnCmd = conn.CreateCommand();
                    insertTurnCmd.CommandText = "INSERT INTO turns (id, session_id, turn_index, created_at) VALUES (@id, @sessionId, @turnIndex, @ts)";
                    insertTurnCmd.Parameters.AddWithValue("@id", $"{sessionId}-turn-{j}");
                    insertTurnCmd.Parameters.AddWithValue("@sessionId", sessionId);
                    insertTurnCmd.Parameters.AddWithValue("@turnIndex", j);
                    insertTurnCmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.AddMinutes(-i).ToString("o"));
                    insertTurnCmd.ExecuteNonQuery();
                }
            }
        }

        SqliteConnection.ClearAllPools();
        return path;
    }

    private string CreateEmptyDb()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.Delete(path);

        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();

            using var createSessionsCmd = conn.CreateCommand();
            createSessionsCmd.CommandText = @"
                CREATE TABLE sessions (
                    id TEXT PRIMARY KEY,
                    repository TEXT,
                    branch TEXT,
                    summary TEXT,
                    created_at TEXT,
                    updated_at TEXT
                )";
            createSessionsCmd.ExecuteNonQuery();

            using var createTurnsCmd = conn.CreateCommand();
            createTurnsCmd.CommandText = @"
                CREATE TABLE turns (
                    id TEXT PRIMARY KEY,
                    session_id TEXT,
                    turn_index INTEGER,
                    request TEXT,
                    response TEXT,
                    created_at TEXT,
                    FOREIGN KEY (session_id) REFERENCES sessions(id)
                )";
            createTurnsCmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();
        return path;
    }

    [Fact]
    public void Green_Zone_With_Sessions_And_Turns()
    {
        // Arrange: DB with sessions and turns
        var path = CreateDbWithSessionsAndTurns(sessionCount: 3, turnsPerSession: 5);
        var dim = new DimE2E();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("E2E Probe", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);
        var detail = result.Detail["detail"] as string;
        Assert.NotNull(detail);
        Assert.Contains("list→show OK", detail);
        Assert.Contains("session-00000".Substring(0, 8), detail); // Most recent session (i=0)
        Assert.Contains("3 turns sampled", detail); // Should sample 3 turns (LIMIT 3)
        Assert.Equal("", result.Detail["hint"]);
    }

    [Fact]
    public void Amber_Zone_No_Sessions()
    {
        // Arrange: Empty DB (no sessions)
        var path = CreateEmptyDb();
        var dim = new DimE2E();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("E2E Probe", result.Name);
        Assert.Equal(5.0, result.Score);
        Assert.Equal("AMBER", result.Detail["zone"]);
        Assert.Equal("No sessions found", result.Detail["detail"]);
        Assert.Equal("Use Copilot CLI first", result.Detail["hint"]);
    }

    [Fact]
    public void Green_Zone_With_Sessions_No_Turns()
    {
        // Arrange: DB with sessions but no turns
        var path = CreateDbWithSessionsAndTurns(sessionCount: 2, turnsPerSession: 0);
        var dim = new DimE2E();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("E2E Probe", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);
        var detail = result.Detail["detail"] as string;
        Assert.NotNull(detail);
        Assert.Contains("list→show OK", detail);
        Assert.Contains("0 turns sampled", detail); // No turns in DB
        Assert.Equal("", result.Detail["hint"]);
    }
}
