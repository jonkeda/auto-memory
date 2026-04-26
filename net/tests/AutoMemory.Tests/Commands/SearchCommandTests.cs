using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using AutoMemory.Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Commands;

[Collection("Console Capture")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class SearchCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalErr;
    private StringWriter _captureOut;
    private StringWriter _captureErr;
    private readonly string? _originalDbPath;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SearchCommandTests()
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

        // Create FTS5 search_index table
        conn.ExecuteNonQuery("""
            CREATE VIRTUAL TABLE search_index USING fts5(
                content,
                session_id UNINDEXED,
                source_type UNINDEXED
            )
            """);

        // Insert test sessions
        conn.ExecuteNonQuery("""
            INSERT INTO sessions VALUES
            ('s1000000', '/tmp', 'owner/repo', 'main', 'Test OAuth implementation', datetime('now'), datetime('now'), 'local'),
            ('s2000000', '/tmp', 'owner/repo', 'main', 'Fix bug in CLAUDE.md parser', datetime('now', '-1 day'), datetime('now'), 'local'),
            ('s3000000', '/tmp', 'other/repo', 'dev', 'Other repo session', datetime('now'), datetime('now'), 'local')
            """);

        // Insert test search index entries
        conn.ExecuteNonQuery("""
            INSERT INTO search_index VALUES
            ('Implement OAuth2 authentication flow', 's1000000', 'turn'),
            ('Fix CLAUDE.md parsing logic', 's2000000', 'turn'),
            ('Update README documentation', 's3000000', 'turn')
            """);

        // Insert test files
        conn.ExecuteNonQuery("""
            INSERT INTO session_files VALUES
            (1, 's1000000', '/tmp/test.md', 'edit', 0, datetime('now')),
            (2, 's2000000', '/tmp/CLAUDE.md', 'read', 0, datetime('now', '-1 day')),
            (3, 's3000000', '/tmp/README.md', 'edit', 0, datetime('now'))
            """);
    }

    [Fact]
    public void SearchReturnsResults()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        var args = new ParsedArgs();
        args.SetPositional(0, "OAuth");
        args.SetOption("repo", "all");  // Explicitly disable repo filtering
        args.SetOption("json", "true");

        var exitCode = SearchCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var result = JsonSerializer.Deserialize<SearchResult>(output, s_jsonOptions);

        Assert.NotNull(result);
        Assert.Equal("OAuth", result.Query);
        Assert.True(result.Count > 0);
        Assert.Contains(result.Results, r => r.SessionIdFull == "s1000000");
    }

    [Fact]
    public void EmptyQueryReturnsWarning()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        var args = new ParsedArgs();
        args.SetPositional(0, "   ");
        args.SetOption("repo", "all");
        args.SetOption("json", "true");

        var exitCode = SearchCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var result = JsonSerializer.Deserialize<SearchResult>(output, s_jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
        Assert.Equal("Empty query — nothing to search", result.Warning);
    }

    [Fact]
    public void SearchWithRepoFilter()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        var args = new ParsedArgs();
        args.SetPositional(0, "README");
        args.SetOption("repo", "other/repo");
        args.SetOption("json", "true");

        var exitCode = SearchCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var result = JsonSerializer.Deserialize<SearchResult>(output, s_jsonOptions);

        Assert.NotNull(result);
        Assert.Equal("other/repo", result.Repo);
        // Should only find results from other/repo
        Assert.All(result.Results, r => Assert.Equal("s3000000", r.SessionIdFull));
    }

    [Fact]
    public void SearchWithLimit()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        var args = new ParsedArgs();
        args.SetPositional(0, "md");
        args.SetOption("repo", "all");
        args.SetOption("limit", "1");
        args.SetOption("json", "true");

        var exitCode = SearchCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var result = JsonSerializer.Deserialize<SearchResult>(output, s_jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Count <= 1);
    }

    [Fact]
    public void SearchTruncatesExcerptTo200Chars()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        // Insert a long content entry
        conn.ExecuteNonQuery("""
            INSERT INTO search_index VALUES
            ('Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident.', 's1000000', 'turn')
            """);

        var args = new ParsedArgs();
        args.SetPositional(0, "Lorem");
        args.SetOption("repo", "all");
        args.SetOption("json", "true");

        var exitCode = SearchCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var result = JsonSerializer.Deserialize<SearchResult>(output, s_jsonOptions);

        Assert.NotNull(result);
        var loremResult = result.Results.Find(r => r.Excerpt.StartsWith("Lorem", StringComparison.Ordinal));
        Assert.NotNull(loremResult);
        Assert.True(loremResult.Excerpt.Length <= 200);
    }

    [Fact]
    public void SearchIncludesFilesWithLikePattern()
    {
        using var conn = new SqliteConnection($"Data Source={_testDbPath}");
        conn.Open();

        var args = new ParsedArgs();
        args.SetPositional(0, "CLAUDE.md");
        args.SetOption("repo", "all");
        args.SetOption("json", "true");

        var exitCode = SearchCommand.RunCore(args, conn);

        Assert.Equal(0, exitCode);

        var output = _captureOut.ToString();
        var result = JsonSerializer.Deserialize<SearchResult>(output, s_jsonOptions);

        Assert.NotNull(result);
        // Should find both the FTS result and the file result
        Assert.Contains(result.Results, r => r.SourceType == "file" && r.Excerpt.Contains("CLAUDE.md", StringComparison.Ordinal));
    }
}
