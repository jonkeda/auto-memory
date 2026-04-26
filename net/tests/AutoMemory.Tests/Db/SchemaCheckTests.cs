using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMemory.Core.Db;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Db;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class SchemaCheckTests : IDisposable
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
    public void CorrectSchema_ReturnsNoProblems()
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

        // Act & Assert
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            var result = SchemaCheck.CheckSchema(conn);
            Assert.True(result.IsValid);
            Assert.Empty(result.Problems);
        }
        SqliteConnection.ClearAllPools();
    }

    [Fact]
    public void MissingColumn_ReportsError()
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

        // Act & Assert
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            var result = SchemaCheck.CheckSchema(conn);
            Assert.False(result.IsValid);
            Assert.Single(result.Problems);
            Assert.Contains("summary", result.Problems[0]);
            Assert.Contains("sessions", result.Problems[0]);
        }
        SqliteConnection.ClearAllPools();
    }

    [Fact]
    public void MissingTable_ReportsError()
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

        // Act & Assert
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            var result = SchemaCheck.CheckSchema(conn);
            Assert.False(result.IsValid);
            Assert.Contains(result.Problems, p => p.Contains("MISSING TABLE: checkpoints"));
        }
        SqliteConnection.ClearAllPools();
    }

    [Fact]
    public void ExtraColumns_Ignored()
    {
        // Arrange: create DB with extra columns in all tables
        var schema = new Dictionary<string, List<string>>
        {
            ["sessions"] = new() { "id", "repository", "branch", "summary", "created_at", "updated_at", "extra_col" },
            ["turns"] = new() { "session_id", "turn_index", "user_message", "assistant_response", "timestamp", "extra_col" },
            ["session_files"] = new() { "session_id", "file_path", "tool_name", "turn_index", "first_seen_at", "extra_col" },
            ["session_refs"] = new() { "session_id", "ref_type", "ref_value", "turn_index", "created_at", "extra_col" },
            ["checkpoints"] = new() { "session_id", "checkpoint_number", "title", "overview", "created_at", "extra_col" }
        };
        var path = CreateDb(schema);

        // Act & Assert
        using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            conn.Open();
            var result = SchemaCheck.CheckSchema(conn);
            Assert.True(result.IsValid);
            Assert.Empty(result.Problems);
        }
        SqliteConnection.ClearAllPools();
    }
}
