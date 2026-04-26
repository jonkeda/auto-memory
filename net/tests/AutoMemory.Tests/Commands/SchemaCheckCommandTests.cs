using System;
using System.IO;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Commands;

[Collection("Console Capture")]
public sealed class SchemaCheckCommandTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string? _originalDbPath;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalErr;
    private StringWriter _captureOut;
    private StringWriter _captureErr;
    
    // Static arrays to satisfy CA1861
    private static readonly string[] s_sessionsColumns = ["id", "repository", "branch", "summary", "created_at", "updated_at"];
    private static readonly string[] s_turnsColumns = ["session_id", "turn_index", "user_message", "assistant_response", "timestamp"];
    private static readonly string[] s_sessionFilesColumns = ["session_id", "file_path", "tool_name", "turn_index", "first_seen_at"];
    private static readonly string[] s_sessionRefsColumns = ["session_id", "ref_type", "ref_value", "turn_index", "created_at"];
    private static readonly string[] s_checkpointsColumns = ["session_id", "checkpoint_number", "title", "overview", "created_at"];
    
    public SchemaCheckCommandTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_schema_{Guid.NewGuid():N}.db");
        _originalDbPath = Environment.GetEnvironmentVariable("SESSION_RECALL_DB");
        
        // Capture stdout/stderr
        _originalOut = Console.Out;
        _originalErr = Console.Error;
        _captureOut = new StringWriter();
        _captureErr = new StringWriter();
        Console.SetOut(_captureOut);
        Console.SetError(_captureErr);
    }
    
    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalErr);
        _captureOut.Dispose();
        _captureErr.Dispose();
        
        // Clear connection pools to release file locks
        SqliteConnection.ClearAllPools();
        
        // Give it a moment for cleanup
        System.Threading.Thread.Sleep(50);
        
        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch (IOException)
            {
                // Ignore - file is still locked, will be cleaned up by temp folder cleanup
            }
        }
        
        // Restore original environment variable
        Environment.SetEnvironmentVariable("SESSION_RECALL_DB", _originalDbPath);
    }

    private void CreateDb(Action<SqliteConnection> configure)
    {
        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        configure(conn);
    }

    private static void CreateTable(SqliteConnection conn, string tableName, string[] columns)
    {
        var colDefs = string.Join(", ", Array.ConvertAll(columns, c => $"{c} TEXT"));
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE TABLE {tableName} ({colDefs})";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void CorrectSchemaReturnsZero()
    {
        // Create DB with all expected tables
        CreateDb(conn =>
        {
            CreateTable(conn, "sessions", s_sessionsColumns);
            CreateTable(conn, "turns", s_turnsColumns);
            CreateTable(conn, "session_files", s_sessionFilesColumns);
            CreateTable(conn, "session_refs", s_sessionRefsColumns);
            CreateTable(conn, "checkpoints", s_checkpointsColumns);
        });

        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        
        var args = new ParsedArgs(
            new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.List<string>());
        
        var exitCode = SchemaCheckCommand.RunCore(args, conn);
        Assert.Equal(0, exitCode);
        
        // Verify exact text output matches Python
        var output = _captureOut.ToString().TrimEnd();
        Assert.Equal("✅ Schema OK", output);
        
        // Stderr should be empty
        var errOutput = _captureErr.ToString();
        Assert.Empty(errOutput);
    }

    [Fact]
    public void MissingColumnReturnsTwo()
    {
        // Create DB with missing 'summary' column in sessions table
        CreateDb(conn =>
        {
            CreateTable(conn, "sessions", ["id", "repository", "branch", "created_at", "updated_at"]); // missing summary
            CreateTable(conn, "turns", s_turnsColumns);
            CreateTable(conn, "session_files", s_sessionFilesColumns);
            CreateTable(conn, "session_refs", s_sessionRefsColumns);
            CreateTable(conn, "checkpoints", s_checkpointsColumns);
        });

        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        
        var args = new ParsedArgs(
            new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.List<string>());
        
        var exitCode = SchemaCheckCommand.RunCore(args, conn);
        Assert.Equal(2, exitCode);
        
        // Verify text output matches Python
        var errOutput = _captureErr.ToString();
        Assert.Contains("❌ Schema drift. Copilot CLI may have been upgraded.", errOutput);
        Assert.Contains("summary", errOutput);
        
        // Stdout should be empty in error case
        var output = _captureOut.ToString();
        Assert.Empty(output);
    }

    [Fact]
    public void MissingTableReturnsTwo()
    {
        // Create DB without checkpoints table
        CreateDb(conn =>
        {
            CreateTable(conn, "sessions", s_sessionsColumns);
            CreateTable(conn, "turns", s_turnsColumns);
            CreateTable(conn, "session_files", s_sessionFilesColumns);
            CreateTable(conn, "session_refs", s_sessionRefsColumns);
            // missing checkpoints table
        });

        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        
        var args = new ParsedArgs(
            new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.List<string>());
        
        var exitCode = SchemaCheckCommand.RunCore(args, conn);
        Assert.Equal(2, exitCode);
        
        // Verify text output matches Python
        var errOutput = _captureErr.ToString();
        Assert.Contains("❌ Schema drift. Copilot CLI may have been upgraded.", errOutput);
        Assert.Contains("MISSING TABLE: checkpoints", errOutput);
        
        // Stdout should be empty in error case
        var output = _captureOut.ToString();
        Assert.Empty(output);
    }

    [Fact]
    public void ExtraColumnsReturnsZero()
    {
        // Create DB with extra columns beyond expected (should be OK)
        CreateDb(conn =>
        {
            CreateTable(conn, "sessions", ["id", "repository", "branch", "summary", "created_at", "updated_at", "extra_col"]);
            CreateTable(conn, "turns", ["session_id", "turn_index", "user_message", "assistant_response", "timestamp", "extra_col"]);
            CreateTable(conn, "session_files", ["session_id", "file_path", "tool_name", "turn_index", "first_seen_at", "extra_col"]);
            CreateTable(conn, "session_refs", ["session_id", "ref_type", "ref_value", "turn_index", "created_at", "extra_col"]);
            CreateTable(conn, "checkpoints", ["session_id", "checkpoint_number", "title", "overview", "created_at", "extra_col"]);
        });

        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        
        var args = new ParsedArgs(
            new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.List<string>());
        
        var exitCode = SchemaCheckCommand.RunCore(args, conn);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void JsonModeOutputsJsonFormat()
    {
        // Create valid DB
        CreateDb(conn =>
        {
            CreateTable(conn, "sessions", s_sessionsColumns);
            CreateTable(conn, "turns", s_turnsColumns);
            CreateTable(conn, "session_files", s_sessionFilesColumns);
            CreateTable(conn, "session_refs", s_sessionRefsColumns);
            CreateTable(conn, "checkpoints", s_checkpointsColumns);
        });

        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        
        var args = new ParsedArgs(
            new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["json"] = "true"
            },
            new System.Collections.Generic.List<string>());
        
        var exitCode = SchemaCheckCommand.RunCore(args, conn);
        Assert.Equal(0, exitCode);
        
        // Verify JSON structure matches Python: {"ok": true, "problems": []}
        var output = _captureOut.ToString();
        Assert.Contains("\"ok\"", output);
        Assert.Contains("true", output);
        Assert.Contains("\"problems\"", output);
        Assert.Contains("[]", output);
        
        // Stderr should be empty
        var errOutput = _captureErr.ToString();
        Assert.Empty(errOutput);
    }
    
    [Fact]
    public void JsonModeWithProblemsOutputsJsonFormat()
    {
        // Create DB with missing column
        CreateDb(conn =>
        {
            CreateTable(conn, "sessions", ["id", "repository", "branch", "created_at", "updated_at"]); // missing summary
            CreateTable(conn, "turns", s_turnsColumns);
            CreateTable(conn, "session_files", s_sessionFilesColumns);
            CreateTable(conn, "session_refs", s_sessionRefsColumns);
            CreateTable(conn, "checkpoints", s_checkpointsColumns);
        });

        using var conn = new SqliteConnection($"Data Source={_tempDbPath}");
        conn.Open();
        
        var args = new ParsedArgs(
            new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["json"] = "true"
            },
            new System.Collections.Generic.List<string>());
        
        var exitCode = SchemaCheckCommand.RunCore(args, conn);
        Assert.Equal(2, exitCode);
        
        // Verify JSON structure matches Python: {"ok": false, "problems": [...]}
        var output = _captureOut.ToString();
        Assert.Contains("\"ok\"", output);
        Assert.Contains("false", output);
        Assert.Contains("\"problems\"", output);
        Assert.Contains("summary", output);
        
        // In JSON mode, errors go to stdout, not stderr
        var errOutput = _captureErr.ToString();
        Assert.Empty(errOutput);
    }
}
