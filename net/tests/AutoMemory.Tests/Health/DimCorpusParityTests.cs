using System;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

/// <summary>
/// Verify exact score parity with Python reference implementation.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimCorpusParityTests
{
    [Theory]
    [InlineData(30, 5.5, "AMBER")]  // Python: {'score': 5.5, 'zone': 'AMBER'}
    [InlineData(50, 7.0, "GREEN")]  // Python: {'score': 7.0, 'zone': 'GREEN'}
    [InlineData(10, 4.0, "AMBER")]  // Python: {'score': 4.0, 'zone': 'AMBER'}
    [InlineData(5, 1.5, "RED")]     // Python: {'score': 1.5, 'zone': 'RED'}
    [InlineData(0, 0.0, "RED")]     // Edge case: zero sessions
    [InlineData(100, 10.0, "GREEN")] // Edge case: score capped at 10.0
    public void Scores_Match_Python_Reference(int sessionCount, double expectedScore, string expectedZone)
    {
        // Arrange: create in-memory DB with specified number of sessions
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using (var createCmd = conn.CreateCommand())
        {
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
        }

        for (int i = 0; i < sessionCount; i++)
        {
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = "INSERT INTO sessions (id) VALUES (@id)";
            insertCmd.Parameters.AddWithValue("@id", $"session-{i:D5}");
            insertCmd.ExecuteNonQuery();
        }

        var dim = new DimCorpus();

        // Act
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));

        // Assert
        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(expectedZone, result.Detail["zone"]);
    }
}
