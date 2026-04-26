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

/// <summary>
/// Tests for --days filter across list, files, checkpoints commands.
/// Verifies date-cutoff math is identical to Python session_recall.
/// 
/// Boundary behavior: SQLite's datetime('now', '-N days') creates a cutoff timestamp,
/// and the queries use '>=' comparison, making the boundary INCLUSIVE.
/// A row created exactly N*86400 seconds ago will be included in results.
/// </summary>
[Collection("Console Capture")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DaysFilterTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalErr;
    private StringWriter _captureOut;
    private StringWriter _captureErr;
    private readonly string? _originalDbPath;

    public DaysFilterTests()
    {
        // Save original environment variable
        _originalDbPath = Environment.GetEnvironmentVariable("SESSION_RECALL_DB");

        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        CreateDbWithAgedData();

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

    /// <summary>
    /// Create temp DB with sessions/files/checkpoints at various ages:
    /// - s_now: today
    /// - s_3d: 3 days ago
    /// - s_10d: 10 days ago
    /// - s_60d: 60 days ago
    /// </summary>
    private void CreateDbWithAgedData()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        // Create schema
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
                history TEXT,
                work_done TEXT,
                technical_details TEXT,
                important_files TEXT,
                next_steps TEXT,
                created_at TEXT
            )
            """);

        // Note: FTS5 virtual table commented out - not needed for days filter tests
        // conn.ExecuteNonQuery("""
        //     CREATE VIRTUAL TABLE search_index USING fts5(
        //         content,
        //         session_id UNINDEXED,
        //         source_type UNINDEXED,
        //         source_id UNINDEXED
        //     )
        //     """);

        // Insert aged data: today, 3 days ago, 10 days ago, 60 days ago
        conn.ExecuteNonQuery("""
            INSERT INTO sessions VALUES
            ('s_now', '/tmp', 'owner/repo', 'main', 'Session s_now', datetime('now', '0 days'), datetime('now', '0 days'), 'local'),
            ('s_3d', '/tmp', 'owner/repo', 'main', 'Session s_3d', datetime('now', '-3 days'), datetime('now', '-3 days'), 'local'),
            ('s_10d', '/tmp', 'owner/repo', 'main', 'Session s_10d', datetime('now', '-10 days'), datetime('now', '-10 days'), 'local'),
            ('s_60d', '/tmp', 'owner/repo', 'main', 'Session s_60d', datetime('now', '-60 days'), datetime('now', '-60 days'), 'local')
            """);

        conn.ExecuteNonQuery("""
            INSERT INTO session_files VALUES
            (NULL, 's_now', '/tmp/file-s_now.md', 'edit', 0, datetime('now', '0 days')),
            (NULL, 's_3d', '/tmp/file-s_3d.md', 'edit', 0, datetime('now', '-3 days')),
            (NULL, 's_10d', '/tmp/file-s_10d.md', 'edit', 0, datetime('now', '-10 days')),
            (NULL, 's_60d', '/tmp/file-s_60d.md', 'edit', 0, datetime('now', '-60 days'))
            """);

        conn.ExecuteNonQuery("""
            INSERT INTO checkpoints VALUES
            (NULL, 's_now', 1, 'Checkpoint s_now', 'overview', '', '', '', '', '', datetime('now', '0 days')),
            (NULL, 's_3d', 1, 'Checkpoint s_3d', 'overview', '', '', '', '', '', datetime('now', '-3 days')),
            (NULL, 's_10d', 1, 'Checkpoint s_10d', 'overview', '', '', '', '', '', datetime('now', '-10 days')),
            (NULL, 's_60d', 1, 'Checkpoint s_60d', 'overview', '', '', '', '', '', datetime('now', '-60 days'))
            """);
    }

    private void ResetCapture()
    {
        _captureOut.Dispose();
        _captureOut = new StringWriter();
        Console.SetOut(_captureOut);
    }

    private T GetJsonOutput<T>()
    {
        var json = _captureOut.ToString();
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize JSON");
    }

    // -------- LIST --------

    [Fact]
    public void List_Days_1_Returns_Only_Today()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "1", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<ListResult>();
        Assert.Equal(1, result.Count);
        Assert.Single(result.Sessions);
        Assert.Equal("s_now", result.Sessions[0].IdFull);
    }

    [Fact]
    public void List_Days_7_Includes_3d_Excludes_10d()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "7", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<ListResult>();
        var ids = new HashSet<string>();
        foreach (var session in result.Sessions)
        {
            ids.Add(session.IdFull);
        }

        Assert.Contains("s_now", ids);
        Assert.Contains("s_3d", ids);
        Assert.DoesNotContain("s_10d", ids);
        Assert.DoesNotContain("s_60d", ids);
    }

    [Fact]
    public void List_Days_30_Excludes_60d()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "30", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<ListResult>();
        var ids = new HashSet<string>();
        foreach (var session in result.Sessions)
        {
            ids.Add(session.IdFull);
        }

        Assert.DoesNotContain("s_60d", ids);
        Assert.Equal(3, ids.Count); // s_now, s_3d, s_10d
    }

    [Fact]
    public void List_Days_Default_30()
    {
        // days=null should use default 30 days (excludes 60d old)
        var args = CreateArgs("--repo", "owner/repo", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<ListResult>();
        var ids = new HashSet<string>();
        foreach (var session in result.Sessions)
        {
            ids.Add(session.IdFull);
        }

        Assert.DoesNotContain("s_60d", ids);
        Assert.Equal(3, ids.Count); // default 30 days
    }

    [Fact]
    public void List_Days_Zero_Uses_Default()
    {
        // In Python, days=0 is falsy, so default 30 applies
        // Our parser returns 0, which is not null, so we need to handle this case
        // Match Python behavior: days=0 should behave like days=30
        var args = CreateArgs("--repo", "owner/repo", "--days", "0", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<ListResult>();
        
        // days=0 should return 3 sessions (matching Python's falsy behavior -> default 30)
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void List_Days_Large_Number_Returns_All()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "3650", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = ListCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<ListResult>();
        Assert.Equal(4, result.Count); // all sessions
    }

    // -------- FILES --------

    [Fact]
    public void Files_Days_1()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "1", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<FilesResult>();
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public void Files_Days_7()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "7", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<FilesResult>();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Files_No_Days_Returns_All()
    {
        var args = CreateArgs("--repo", "owner/repo", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<FilesResult>();
        Assert.Equal(4, result.Count); // all files including 60d old
    }

    [Fact]
    public void Files_Days_With_Repo_All()
    {
        var args = CreateArgs("--repo", "all", "--days", "7", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<FilesResult>();
        Assert.Equal(2, result.Count);
    }

    // -------- CHECKPOINTS --------

    [Fact]
    public void Checkpoints_Days_1()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "1", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<CheckpointsResult>();
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public void Checkpoints_Days_15()
    {
        var args = CreateArgs("--repo", "owner/repo", "--days", "15", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<CheckpointsResult>();
        Assert.Equal(3, result.Count); // now, 3d, 10d — not 60d
    }

    [Fact]
    public void Checkpoints_No_Days_Returns_All()
    {
        var args = CreateArgs("--repo", "owner/repo", "--limit", "100", "--json");

        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();
        var exitCode = CheckpointsCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);
        var result = GetJsonOutput<CheckpointsResult>();
        Assert.Equal(4, result.Count); // all checkpoints
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
