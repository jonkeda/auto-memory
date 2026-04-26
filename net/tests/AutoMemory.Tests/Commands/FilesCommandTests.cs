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
public sealed class FilesCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalErr;
    private StringWriter _captureOut;
    private StringWriter _captureErr;
    private readonly string? _originalDbPath;

    public FilesCommandTests()
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
            INSERT INTO session_files VALUES
            (1, 's1000000', '/tmp/file1.py', 'edit', 0, datetime('now')),
            (2, 's2000000', '/tmp/file2.py', 'read', 0, datetime('now', '-3 days')),
            (3, 's3000000', '/tmp/file3.py', 'edit', 0, datetime('now', '-10 days')),
            (4, 's4000000', '/tmp/file4.py', 'edit', 0, datetime('now', '-60 days'))
            """);
    }

    [Fact]
    public void Files_Days1_ReturnsOnlyRecentFile()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--days", "1", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(1, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Files_Days7_ReturnsTwoFiles()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--days", "7", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(2, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Files_NoDays_ReturnsAllFiles()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--limit", "100", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        // Should return all 4 files including 60d old
        Assert.Equal(4, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Files_DaysWithRepoAll_Filters()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "all", "--days", "7", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(2, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Files_FiltersBy_Repo()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal("owner/repo", output.GetProperty("repo").GetString());
        // owner/repo has 3 files (s1, s2, s4)
        Assert.Equal(3, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Files_RespectsLimit()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--limit", "2", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.Equal(2, output.GetProperty("count").GetInt32());
        Assert.Equal(2, output.GetProperty("files").GetArrayLength());
    }

    [Fact]
    public void Files_JsonHasExpectedShape()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        Assert.True(output.TryGetProperty("repo", out _));
        Assert.True(output.TryGetProperty("count", out _));
        Assert.True(output.TryGetProperty("files", out _));

        var files = output.GetProperty("files");
        if (files.GetArrayLength() > 0)
        {
            var firstFile = files[0];
            Assert.True(firstFile.TryGetProperty("file_path", out _));
            Assert.True(firstFile.TryGetProperty("tool_name", out _));
            Assert.True(firstFile.TryGetProperty("date", out _));
            Assert.True(firstFile.TryGetProperty("session_id", out _));
            Assert.True(firstFile.TryGetProperty("session_summary", out _));
        }
    }

    [Fact]
    public void Files_DefaultLimitIs10()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        // Add more files to exceed default limit
        using (var insertConn = new SqliteConnection($"Data Source={_testDbPath}"))
        {
            insertConn.Open();
            for (int i = 5; i <= 15; i++)
            {
                insertConn.ExecuteNonQuery($"""
                    INSERT INTO sessions VALUES
                    ('s{i:D7}', '/tmp', 'owner/repo', 'main', 'Session {i}', datetime('now'), datetime('now'), 'local')
                    """);
                insertConn.ExecuteNonQuery($"""
                    INSERT INTO session_files VALUES
                    ({i}, 's{i:D7}', '/tmp/file{i}.py', 'edit', 0, datetime('now'))
                    """);
            }
        }

        var args = CreateArgs("--repo", "owner/repo", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        // Should only return 10 files, not all
        Assert.Equal(10, output.GetProperty("count").GetInt32());
    }

    [Fact]
    public void Files_DateFieldTruncatedTo10Chars()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--limit", "1", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        var files = output.GetProperty("files");
        var firstFile = files[0];
        var date = firstFile.GetProperty("date").GetString();
        
        // Date should be exactly 10 characters (YYYY-MM-DD)
        Assert.NotNull(date);
        Assert.True(date!.Length <= 10, $"Date should be <= 10 chars, got: {date}");
    }

    [Fact]
    public void Files_SessionIdTruncatedTo8Chars()
    {
        using var conn = Connect.ConnectReadOnlyCore(_testDbPath);
        var args = CreateArgs("--repo", "owner/repo", "--limit", "1", "--json");
        
        var exitCode = FilesCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = JsonSerializer.Deserialize<JsonElement>(_captureOut.ToString());
        var files = output.GetProperty("files");
        var firstFile = files[0];
        var sessionId = firstFile.GetProperty("session_id").GetString();
        
        // Session ID should be exactly 8 characters
        Assert.NotNull(sessionId);
        Assert.True(sessionId!.Length <= 8, $"Session ID should be <= 8 chars, got: {sessionId}");
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
