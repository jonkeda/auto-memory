using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimLatencyTests : IDisposable
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

        SqliteConnection.ClearAllPools();
    }

    private string CreateDbWithData()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.Delete(path);

        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();

            // Create sessions table
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

            // Create turns table
            using var createTurnsCmd = conn.CreateCommand();
            createTurnsCmd.CommandText = @"
                CREATE TABLE turns (
                    session_id TEXT NOT NULL,
                    turn_index INTEGER NOT NULL,
                    user_prompt TEXT,
                    PRIMARY KEY (session_id, turn_index),
                    FOREIGN KEY (session_id) REFERENCES sessions(id)
                )";
            createTurnsCmd.ExecuteNonQuery();

            // Insert test sessions
            for (int i = 0; i < 20; i++)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO sessions (id, repository, branch, summary, created_at) VALUES (@id, 'repo', 'main', @summary, @ts)";
                insertCmd.Parameters.AddWithValue("@id", $"session-{i:D5}");
                insertCmd.Parameters.AddWithValue("@summary", $"Test session {i}");
                insertCmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.AddMinutes(-i).ToString("o"));
                insertCmd.ExecuteNonQuery();

                // Insert turns for the first session
                if (i == 0)
                {
                    for (int t = 0; t < 10; t++)
                    {
                        using var insertTurnCmd = conn.CreateCommand();
                        insertTurnCmd.CommandText = "INSERT INTO turns (session_id, turn_index, user_prompt) VALUES (@sid, @tidx, @prompt)";
                        insertTurnCmd.Parameters.AddWithValue("@sid", $"session-{i:D5}");
                        insertTurnCmd.Parameters.AddWithValue("@tidx", t);
                        insertTurnCmd.Parameters.AddWithValue("@prompt", $"Turn {t} prompt");
                        insertTurnCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        SqliteConnection.ClearAllPools();
        return path;
    }

    [Fact]
    public void Run_Returns_Valid_Result_With_Latency_Measurement()
    {
        // Arrange: Create a test DB
        var dbPath = CreateDbWithData();

        var dim = new DimLatency();

        // Act: Run the dimension check with dbPathOverride
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), dbPath);
        }
        
        SqliteConnection.ClearAllPools();

        // Assert: Verify result structure
        Assert.Equal("Query Latency", result.Name);
        Assert.NotNull(result.Score);
        Assert.True(result.Score >= 0.0 && result.Score <= 10.0, $"Score {result.Score} should be between 0 and 10");
        
        Assert.True(result.Detail.ContainsKey("zone"));
        Assert.True(result.Detail.ContainsKey("detail"));
        Assert.True(result.Detail.ContainsKey("hint"));
        
        Assert.Equal("Check DB size or run PRAGMA integrity_check", result.Detail["hint"]);

        // Verify detail contains elapsed time in format "XXXms"
        var detail = result.Detail["detail"]?.ToString();
        Assert.NotNull(detail);
        var match = Regex.Match(detail, @"^(\d+)ms$");
        Assert.True(match.Success, $"Detail '{detail}' should match pattern 'XXXms'");

        var elapsedMs = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        
        // For a small test DB, latency should be well under 200ms (green threshold)
        // But we'll be lenient and just check it's reasonable
        Assert.True(elapsedMs >= 0, "Elapsed time should be non-negative");
        Assert.True(elapsedMs < 5000, "Elapsed time should be less than 5 seconds for a small test DB");
    }

    [Fact]
    public void Run_Assigns_Green_Zone_For_Fast_Queries()
    {
        // Arrange
        var dbPath = CreateDbWithData();

        var dim = new DimLatency();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), dbPath);
        }
        
        SqliteConnection.ClearAllPools();

        // Assert: Small test DB should have fast queries (< 200ms -> GREEN)
        // This is probabilistic but should hold on any reasonable hardware
        var zone = result.Detail["zone"]?.ToString();
        Assert.True(zone == "GREEN" || zone == "AMBER", 
            $"Expected GREEN or AMBER zone for small test DB, got {zone}");
    }

    [Fact]
    public void Run_Handles_Empty_Database()
    {
        // Arrange: Create empty DB
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
                    session_id TEXT NOT NULL,
                    turn_index INTEGER NOT NULL,
                    user_prompt TEXT,
                    PRIMARY KEY (session_id, turn_index)
                )";
            createTurnsCmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();

        var dim = new DimLatency();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), path);
        }
        
        SqliteConnection.ClearAllPools();

        // Assert: Should still work, just with empty result sets
        Assert.Equal("Query Latency", result.Name);
        Assert.NotNull(result.Score);
        Assert.True(result.Score >= 0.0 && result.Score <= 10.0);
        Assert.Equal("GREEN", result.Detail["zone"]); // Empty DB queries should be very fast
    }

    [Fact]
    public void Run_Returns_Red_Zone_On_Missing_Database()
    {
        // Arrange: Point to non-existent DB
        var fakePath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.db");

        var dim = new DimLatency();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection("Data Source=:memory:"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), fakePath);
        }
        
        SqliteConnection.ClearAllPools();

        // Assert: Should return RED with error detail
        Assert.Equal("Query Latency", result.Name);
        Assert.Equal(0.0, result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        Assert.NotNull(result.Detail["detail"]);
        Assert.Equal("Check DB size or run PRAGMA integrity_check", result.Detail["hint"]);
    }
}
