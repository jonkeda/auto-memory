using System;
using System.IO;
using System.Text.Json;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Commands;

[Collection("Console Capture")]
public sealed class HealthCommandTests : IDisposable
{
    private readonly string _tempDbPath;
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

    public HealthCommandTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_health_{Guid.NewGuid():N}.db");

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
    public void TextOutputShowsAllDimensions()
    {
        // Create DB with correct schema
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

        var args = new ParsedArgs();
        var exitCode = HealthCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();

        // Verify all 9 dimensions are present
        Assert.Contains("DB Freshness", output);
        Assert.Contains("Schema Integrity", output);
        Assert.Contains("Query Latency", output);
        Assert.Contains("Corpus Size", output);
        Assert.Contains("Summary Coverage", output);
        Assert.Contains("Repo Coverage", output);
        Assert.Contains("Concurrency", output);
        Assert.Contains("E2E", output);
        Assert.Contains("Progressive Disclosure", output);

        // Verify overall score is present
        Assert.Contains("Overall", output);
    }

    [Fact]
    public void JsonOutputContainsExpectedFields()
    {
        // Create DB with correct schema
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

        var parser = new ArgParser("test").AddOption("json", isFlag: true);
        var args = parser.Parse(["--json"]);
        var exitCode = HealthCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var json = JsonDocument.Parse(output);

        // Verify top-level fields
        Assert.True(json.RootElement.TryGetProperty("overall_score", out var overallScore));
        Assert.True(json.RootElement.TryGetProperty("dims", out var dims));
        Assert.True(json.RootElement.TryGetProperty("top_hints", out var hints));

        // Verify dims is an array of 9 elements
        Assert.Equal(JsonValueKind.Array, dims.ValueKind);
        Assert.Equal(9, dims.GetArrayLength());

        // Verify each dim has the expected fields
        foreach (var dim in dims.EnumerateArray())
        {
            Assert.True(dim.TryGetProperty("name", out _));
            Assert.True(dim.TryGetProperty("score", out _));
            Assert.True(dim.TryGetProperty("zone", out _));
            Assert.True(dim.TryGetProperty("detail", out _));
            Assert.True(dim.TryGetProperty("hint", out _));
        }
    }
}
