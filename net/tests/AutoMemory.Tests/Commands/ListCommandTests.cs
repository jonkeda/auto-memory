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
public sealed class ListCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalErr;
    private StringWriter _captureOut;
    private StringWriter _captureErr;
    private readonly string? _originalDbPath;

    public ListCommandTests()
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
            ('s1000000', '/tmp', 'owner/repo', 'main', 'Test session 1', datetime('now'), datetime('now'), 'local'),
            ('s2000000', '/tmp', 'owner/repo', 'main', 'Test session 2', datetime('now', '-1 day'), datetime('now'), 'local'),
            ('s3000000', '/tmp', 'other/repo', 'dev', 'Other repo', datetime('now'), datetime('now'), 'local')
            """);

        conn.ExecuteNonQuery("""
            INSERT INTO turns VALUES
            (1, 's1000000', 0, 'hello', 'hi', datetime('now')),
            (2, 's1000000', 1, 'q2', 'a2', datetime('now'))
            """);

        conn.ExecuteNonQuery("""
            INSERT INTO session_files VALUES
            (1, 's1000000', '/tmp/test.md', 'edit', 0, datetime('now'))
            """);
    }

    [Fact]
    public void List_FiltersBy_Repo()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--json");
        
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal("owner/repo", output.GetProperty("repo").GetString());
        Assert.Equal(2, output.GetProperty("count").GetInt32());

        var sessions = output.GetProperty("sessions");
        Assert.Equal(2, sessions.GetArrayLength());

        foreach (var session in sessions.EnumerateArray())
        {
            Assert.Equal("owner/repo", session.GetProperty("repository").GetString());
        }
    }

    [Fact]
    public void List_RespectsLimit()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--limit", "1", "--json");
        
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(1, output.GetProperty("count").GetInt32());
        Assert.Equal(1, output.GetProperty("sessions").GetArrayLength());
    }

    [Fact]
    public void List_JsonHasExpectedShape()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--json");
        
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.True(output.TryGetProperty("repo", out _));
        Assert.True(output.TryGetProperty("count", out _));
        Assert.True(output.TryGetProperty("sessions", out _));
        Assert.True(output.TryGetProperty("recent_files", out _));

        var sessions = output.GetProperty("sessions");
        if (sessions.GetArrayLength() > 0)
        {
            var firstSession = sessions[0];
            Assert.True(firstSession.TryGetProperty("turns_count", out _));
            Assert.True(firstSession.TryGetProperty("id_short", out _));
            Assert.True(firstSession.TryGetProperty("id_full", out _));
        }
    }

    [Fact]
    public void List_DefaultDaysIs30()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        // Create a session that's 31 days old
        using (var insertConn = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            insertConn.Open();
            insertConn.ExecuteNonQuery("""
                INSERT INTO sessions VALUES
                ('s4000000', '/tmp', 'owner/repo', 'main', 'Old session', datetime('now', '-31 days'), datetime('now'), 'local')
                """);
        }

        var args = CreateArgs("--repo", "owner/repo", "--json");
        
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        // Should not include the 31-day-old session with default days=30
        Assert.Equal(2, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void List_HumanFormat_HasHeader()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo");
        
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        Assert.Contains("ID", output);
        Assert.Contains("Date", output);
        Assert.Contains("Turns", output);
        Assert.Contains("Summary", output);
    }

    [Fact]
    public void List_EmptyDb_ReturnsNoSessions()
    {
        // Create a fresh empty database
        var emptyDbPath = Path.Combine(Path.GetTempPath(), $"test_empty_{Guid.NewGuid():N}.db");
        try
        {
            using (var setupConn = new SqliteConnection($"Data Source={emptyDbPath}"))
            {
                setupConn.Open();
                // Create schema but don't insert any data
                setupConn.ExecuteNonQuery("""
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
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE turns (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        turn_index INTEGER,
                        user_message TEXT,
                        assistant_response TEXT,
                        timestamp TEXT
                    )
                    """);
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE session_files (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        file_path TEXT,
                        tool_name TEXT,
                        turn_index INTEGER,
                        first_seen_at TEXT
                    )
                    """);
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE session_refs (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        ref_type TEXT,
                        ref_value TEXT,
                        turn_index INTEGER,
                        created_at TEXT
                    )
                    """);
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE checkpoints (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        checkpoint_number INTEGER,
                        title TEXT,
                        overview TEXT,
                        created_at TEXT
                    )
                    """);
            }

            using var conn = Connect.ConnectReadOnlyCore(emptyDbPath);
            var args = CreateArgs("--json");
            
            var exitCode = ListCommand.RunCore(args, conn);

            Assert.Equal(0, exitCode);

            var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
            Assert.Equal(0, output.GetProperty("count").GetInt32());
            Assert.Equal(0, output.GetProperty("sessions").GetArrayLength());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            System.Threading.Thread.Sleep(50);
            if (File.Exists(emptyDbPath))
            {
                try
                {
                    File.Delete(emptyDbPath);
                }
                catch (IOException)
                {
                    // Ignore - file is still locked
                }
            }
        }
    }

    [Fact]
    public void List_EmptyDb_HumanMode_ShowsBanner()
    {
        // Create a fresh empty database
        var emptyDbPath = Path.Combine(Path.GetTempPath(), $"test_empty_{Guid.NewGuid():N}.db");
        try
        {
            using (var setupConn = new SqliteConnection($"Data Source={emptyDbPath}"))
            {
                setupConn.Open();
                // Create schema but don't insert any data
                setupConn.ExecuteNonQuery("""
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
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE turns (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        turn_index INTEGER,
                        user_message TEXT,
                        assistant_response TEXT,
                        timestamp TEXT
                    )
                    """);
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE session_files (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        file_path TEXT,
                        tool_name TEXT,
                        turn_index INTEGER,
                        first_seen_at TEXT
                    )
                    """);
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE session_refs (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        ref_type TEXT,
                        ref_value TEXT,
                        turn_index INTEGER,
                        created_at TEXT
                    )
                    """);
                setupConn.ExecuteNonQuery("""
                    CREATE TABLE checkpoints (
                        id INTEGER PRIMARY KEY,
                        session_id TEXT,
                        checkpoint_number INTEGER,
                        title TEXT,
                        overview TEXT,
                        created_at TEXT
                    )
                    """);
            }

            using var conn = Connect.ConnectReadOnlyCore(emptyDbPath);
            var args = CreateArgs();
            
            var exitCode = ListCommand.RunCore(args, conn);

            Assert.Equal(0, exitCode);

            var output = _captureOut.ToString();
            Assert.Contains("No sessions found.", output);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            System.Threading.Thread.Sleep(50);
            if (File.Exists(emptyDbPath))
            {
                try
                {
                    File.Delete(emptyDbPath);
                }
                catch (IOException)
                {
                    // Ignore - file is still locked
                }
            }
        }
    }

    [Fact]
    public void List_OrdersByCreatedAt_MostRecentFirst()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--json");
        
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        var sessions = output.GetProperty("sessions");
        Assert.Equal(2, sessions.GetArrayLength());

        // s1000000 was created "now", s2000000 was created "-1 day"
        // So s1000000 should come first (most recent)
        var firstSession = sessions[0];
        Assert.Equal("s1000000", firstSession.GetProperty("id_short").GetString());
    }

    private static ParsedArgs CreateArgs(params string[] argv)
    {
        var parser = new ArgParser("test", "test")
            .AddOption("repo", defaultValue: null)
            .AddOption("limit", defaultValue: null)
            .AddOption("days", defaultValue: null)
            .AddOption("json", isFlag: true);

        return parser.Parse(argv);
    }
}

internal static class SqliteConnectionExtensions
{
    internal static void ExecuteNonQuery(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
