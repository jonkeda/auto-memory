using System;
using System.Collections.Generic;
using System.IO;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimCorpusTests : IDisposable
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

    private string CreateDbWithSessions(int sessionCount)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.Delete(path); // GetTempFileName creates the file, we need it not to exist

        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();

            using var createCmd = conn.CreateCommand();
            createCmd.CommandText = @"
                CREATE TABLE sessions (
                    id TEXT PRIMARY KEY,
                    repository TEXT,
                    branch TEXT,
                    summary TEXT,
                    created_at TEXT,
                    updated_at TEXT
                )";
            createCmd.ExecuteNonQuery();

            // Insert the specified number of sessions
            for (int i = 0; i < sessionCount; i++)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO sessions (id, repository, branch, created_at) VALUES (@id, 'repo', 'main', @ts)";
                insertCmd.Parameters.AddWithValue("@id", $"session-{i:D5}");
                insertCmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("o"));
                insertCmd.ExecuteNonQuery();
            }
        }

        SqliteConnection.ClearAllPools();
        return path;
    }

    [Fact]
    public void Green_Zone_Above_50_Sessions()
    {
        // Arrange: 100 sessions -> GREEN zone
        var path = CreateDbWithSessions(100);
        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.True(result.Score >= 7.0, $"Expected score >= 7.0, got {result.Score}");
        Assert.Equal("GREEN", result.Detail["zone"]);
        Assert.Equal("100 sessions", result.Detail["detail"]);
        Assert.Equal("Cold start — will improve with usage", result.Detail["hint"]);
    }

    [Fact]
    public void Amber_Zone_Between_10_And_50_Sessions()
    {
        // Arrange: 30 sessions -> AMBER zone
        var path = CreateDbWithSessions(30);
        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.True(result.Score >= 4.0 && result.Score < 7.0, $"Expected 4.0 <= score < 7.0, got {result.Score}");
        Assert.Equal("AMBER", result.Detail["zone"]);
        Assert.Equal("30 sessions", result.Detail["detail"]);
        Assert.Equal("Cold start — will improve with usage", result.Detail["hint"]);
    }

    [Fact]
    public void Red_Zone_Below_10_Sessions()
    {
        // Arrange: 5 sessions -> RED zone
        var path = CreateDbWithSessions(5);
        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.True(result.Score < 4.0, $"Expected score < 4.0, got {result.Score}");
        Assert.Equal("RED", result.Detail["zone"]);
        Assert.Equal("5 sessions", result.Detail["detail"]);
        Assert.Equal("Cold start — will improve with usage", result.Detail["hint"]);
    }

    [Fact]
    public void Exactly_50_Sessions_Is_Green()
    {
        // Arrange: exactly at green threshold
        var path = CreateDbWithSessions(50);
        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.Equal("GREEN", result.Detail["zone"]);
        Assert.Equal(7.0, result.Score);
        Assert.Equal("50 sessions", result.Detail["detail"]);
    }

    [Fact]
    public void Exactly_10_Sessions_Is_Amber()
    {
        // Arrange: exactly at amber threshold
        var path = CreateDbWithSessions(10);
        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.Equal("AMBER", result.Detail["zone"]);
        Assert.Equal(4.0, result.Score);
        Assert.Equal("10 sessions", result.Detail["detail"]);
    }

    [Fact]
    public void Empty_Database_Returns_Red_Zone()
    {
        // Arrange: 0 sessions
        var path = CreateDbWithSessions(0);
        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.Equal(0.0, result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        Assert.Equal("0 sessions", result.Detail["detail"]);
    }

    [Fact]
    public void Missing_Sessions_Table_Returns_Error()
    {
        // Arrange: DB with no sessions table
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.Delete(path);

        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            // Create an empty database with no tables
        }
        SqliteConnection.ClearAllPools();

        var dim = new DimCorpus();

        // Act
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Corpus Size", result.Name);
        Assert.Equal(0.0, result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        Assert.Contains("no such table", result.Detail["detail"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Cold start — will improve with usage", result.Detail["hint"]);
    }
}
