using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Commands;

[Collection("Console Capture")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class CheckpointsCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalErr;
    private StringWriter _captureOut;
    private StringWriter _captureErr;
    private readonly string? _originalDbPath;

    public CheckpointsCommandTests()
    {
        // Save original environment variable
        _originalDbPath = Environment.GetEnvironmentVariable("SESSION_RECALL_DB");

        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        CreateTestDb();

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

        // Close all SQLite connections to this database
        SqliteConnection.ClearAllPools();

        // Give it a moment for cleanup
        System.Threading.Thread.Sleep(50);

        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch (IOException)
            {
                // Ignore - file is still locked, will be cleaned up by temp folder cleanup
            }
        }

        // Restore original environment variable
        Environment.SetEnvironmentVariable("SESSION_RECALL_DB", _originalDbPath);
    }

    private void CreateTestDb()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        conn.ExecuteNonQuery("""
            CREATE TABLE sessions (
                id TEXT PRIMARY KEY,
                cwd TEXT,
                repository TEXT,
                branch TEXT,
                summary TEXT,
                created_at TEXT,
                updated_at TEXT,
                host_type TEXT
            )
            """);

        conn.ExecuteNonQuery("""
            CREATE TABLE turns (
                id INTEGER PRIMARY KEY,
                session_id TEXT,
                turn_index INTEGER,
                user_message TEXT,
                assistant_response TEXT,
                timestamp TEXT
            )
            """);

        conn.ExecuteNonQuery("""
            CREATE TABLE session_files (
                id INTEGER PRIMARY KEY,
                session_id TEXT,
                file_path TEXT,
                tool_name TEXT,
                turn_index INTEGER,
                first_seen_at TEXT
            )
            """);

        conn.ExecuteNonQuery("""
            CREATE TABLE session_refs (
                id INTEGER PRIMARY KEY,
                session_id TEXT,
                ref_type TEXT,
                ref_value TEXT,
                turn_index INTEGER,
                created_at TEXT
            )
            """);

        conn.ExecuteNonQuery("""
            CREATE TABLE checkpoints (
                id INTEGER PRIMARY KEY,
                session_id TEXT,
                checkpoint_number INTEGER,
                title TEXT,
                overview TEXT,
                created_at TEXT
            )
            """);

        // Insert test data
        conn.ExecuteNonQuery("""
            INSERT INTO sessions VALUES
            ('s1000000', '/tmp', 'owner/repo', 'main', 'Session 1', datetime('now'), datetime('now'), 'local'),
            ('s2000000', '/tmp', 'owner/repo', 'dev', 'Session 2', datetime('now', '-3 days'), datetime('now'), 'local'),
            ('s3000000', '/tmp', 'other/repo', 'main', 'Session 3', datetime('now', '-10 days'), datetime('now'), 'local'),
            ('s4000000', '/tmp', 'owner/repo', 'main', 'Session 4', datetime('now', '-60 days'), datetime('now'), 'local')
            """);

        conn.ExecuteNonQuery("""
            INSERT INTO checkpoints VALUES
            (1, 's1000000', 1, 'Checkpoint 1', 'Recent checkpoint', datetime('now')),
            (2, 's2000000', 1, 'Checkpoint 2', 'Three days old', datetime('now', '-3 days')),
            (3, 's3000000', 1, 'Checkpoint 3', 'From other repo', datetime('now', '-10 days')),
            (4, 's4000000', 1, 'Checkpoint 4', 'Very old', datetime('now', '-60 days'))
            """);
    }

    [Fact]
    public void Checkpoints_Days1_ReturnsOnlyRecentCheckpoint()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--days", "1", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(1, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Checkpoints_Days7_ReturnsTwoCheckpoints()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--days", "7", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(2, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Checkpoints_NoDays_ReturnsAllCheckpoints()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--limit", "100", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        // Should return all 4 checkpoints including 60d old
        Assert.Equal(4, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Checkpoints_Limit5_DefaultBehavior()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        // Default limit is 5, we have 4, so should return all 4
        Assert.Equal(4, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Checkpoints_RepoFilter_ReturnsOnlyOwnerRepo()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--limit", "100", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        // Should return 3 checkpoints from owner/repo
        Assert.Equal(3, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Checkpoints_JsonOutput_ContainsRequiredFields()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--limit", "1", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal("all", output.GetProperty("repo").GetString());
        Assert.Equal(1, output.GetProperty("count").GetInt32());

        var checkpoints = output.GetProperty("checkpoints");
        Assert.Equal(1, checkpoints.GetArrayLength());

        var checkpoint = checkpoints[0];
        Assert.True(checkpoint.TryGetProperty("checkpoint_number", out _));
        Assert.True(checkpoint.TryGetProperty("title", out _));
        Assert.True(checkpoint.TryGetProperty("overview", out _));
        Assert.True(checkpoint.TryGetProperty("date", out _));
        Assert.True(checkpoint.TryGetProperty("session_id", out _));
        Assert.True(checkpoint.TryGetProperty("session_summary", out _));
    }

    [Fact]
    public void Checkpoints_OverviewTruncation_Limits300Chars()
    {
        // Create a checkpoint with very long overview
        using (var setupConn = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            setupConn.Open();
            
            setupConn.ExecuteNonQuery("""
                INSERT INTO sessions VALUES
                ('s5000000', '/tmp', 'owner/repo', 'main', 'Session 5', datetime('now'), datetime('now'), 'local')
                """);

            var longOverview = new string('x', 500);
            using (var cmd = setupConn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO checkpoints VALUES (5, 's5000000', 1, 'Long checkpoint', @overview, datetime('now', '+1 hour'))";
                cmd.Parameters.AddWithValue("@overview", longOverview);
                cmd.ExecuteNonQuery();
            }
        }

        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--limit", "10", "--json");
        
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        var checkpoints = output.GetProperty("checkpoints");
        
        // Find the checkpoint with title 'Long checkpoint'
        foreach (var cp in checkpoints.EnumerateArray())
        {
            if (cp.GetProperty("title").GetString() == "Long checkpoint")
            {
                var overview = cp.GetProperty("overview").GetString();
                // Should be truncated to 300 chars
                Assert.Equal(300, overview?.Length);
                return;
            }
        }
        
        Assert.Fail("Long checkpoint not found in results");
    }

    private static ParsedArgs CreateArgs(params string[] argv)
    {
        var parser = new ArgParser("test-checkpoints")
            .AddOption("repo", defaultValue: null)
            .AddOption("limit", defaultValue: null)
            .AddOption("days", defaultValue: null)
            .AddOption("json", isFlag: true);

        return parser.Parse(argv);
    }
}
