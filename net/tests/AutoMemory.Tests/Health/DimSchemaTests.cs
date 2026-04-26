using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimSchemaTests : IDisposable
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

    private string CreateDb(Dictionary<string, List<string>> tables)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        File.Delete(path); // GetTempFileName creates the file, we need it not to exist

        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();

            foreach (var (table, cols) in tables)
            {
                var colDefs = string.Join(", ", cols.Select(c => $"{c} TEXT"));
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE TABLE {table} ({colDefs})";
                cmd.ExecuteNonQuery();
            }
        }

        // Clear connection pool to release the file lock
        SqliteConnection.ClearAllPools();

        return path;
    }

    [Fact]
    public void ValidSchema_ReturnsGreen()
    {
        // Arrange: create DB with all expected tables and columns
        var expectedSchema = new Dictionary<string, List<string>>
        {
            ["sessions"] = new() { "id", "repository", "branch", "summary", "created_at", "updated_at" },
            ["turns"] = new() { "session_id", "turn_index", "user_message", "assistant_response", "timestamp" },
            ["session_files"] = new() { "session_id", "file_path", "tool_name", "turn_index", "first_seen_at" },
            ["session_refs"] = new() { "session_id", "ref_type", "ref_value", "turn_index", "created_at" },
            ["checkpoints"] = new() { "session_id", "checkpoint_number", "title", "overview", "created_at" }
        };
        var path = CreateDb(expectedSchema);

        // Act
        var dim = new DimSchema();
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Schema Integrity", result.Name);
        Assert.Equal(10.0, result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);
        Assert.Equal("All tables/columns OK", result.Detail["detail"]);
        Assert.Equal("", result.Detail["hint"]);
    }

    [Fact]
    public void MissingColumn_ReturnsAmber()
    {
        // Arrange: create DB missing 'summary' column from sessions table
        var schema = new Dictionary<string, List<string>>
        {
            ["sessions"] = new() { "id", "repository", "branch", "created_at", "updated_at" }, // missing 'summary'
            ["turns"] = new() { "session_id", "turn_index", "user_message", "assistant_response", "timestamp" },
            ["session_files"] = new() { "session_id", "file_path", "tool_name", "turn_index", "first_seen_at" },
            ["session_refs"] = new() { "session_id", "ref_type", "ref_value", "turn_index", "created_at" },
            ["checkpoints"] = new() { "session_id", "checkpoint_number", "title", "overview", "created_at" }
        };
        var path = CreateDb(schema);

        // Act
        var dim = new DimSchema();
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Schema Integrity", result.Name);
        Assert.Equal(5.0, result.Score);
        Assert.Equal("AMBER", result.Detail["zone"]);
        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("summary", detail);
        Assert.Contains("sessions", detail);
        Assert.Equal("Run `session-recall schema-check` for details", result.Detail["hint"]);
    }

    [Fact]
    public void MissingTable_ReturnsRed()
    {
        // Arrange: create DB missing 'checkpoints' table entirely
        var schema = new Dictionary<string, List<string>>
        {
            ["sessions"] = new() { "id", "repository", "branch", "summary", "created_at", "updated_at" },
            ["turns"] = new() { "session_id", "turn_index", "user_message", "assistant_response", "timestamp" },
            ["session_files"] = new() { "session_id", "file_path", "tool_name", "turn_index", "first_seen_at" },
            ["session_refs"] = new() { "session_id", "ref_type", "ref_value", "turn_index", "created_at" }
            // missing 'checkpoints'
        };
        var path = CreateDb(schema);

        // Act
        var dim = new DimSchema();
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert
        Assert.Equal("Schema Integrity", result.Name);
        Assert.Equal(1.0, result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("MISSING TABLE", detail);
        Assert.Contains("checkpoints", detail);
        Assert.Equal("Run `session-recall schema-check` for details", result.Detail["hint"]);
    }

    [Fact]
    public void MissingTableAndColumn_ReturnsRed()
    {
        // Arrange: create DB with both missing table and missing column
        var schema = new Dictionary<string, List<string>>
        {
            ["sessions"] = new() { "id", "repository", "branch", "created_at", "updated_at" }, // missing 'summary'
            ["turns"] = new() { "session_id", "turn_index", "user_message", "assistant_response", "timestamp" },
            ["session_files"] = new() { "session_id", "file_path", "tool_name", "turn_index", "first_seen_at" },
            ["session_refs"] = new() { "session_id", "ref_type", "ref_value", "turn_index", "created_at" }
            // missing 'checkpoints' table
        };
        var path = CreateDb(schema);

        // Act
        var dim = new DimSchema();
        DimensionResult result;
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null));
        }
        SqliteConnection.ClearAllPools();

        // Assert - missing table takes precedence (RED, score=1)
        Assert.Equal("Schema Integrity", result.Name);
        Assert.Equal(1.0, result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("MISSING TABLE", detail);
    }
}
