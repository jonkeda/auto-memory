using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using AutoMemory.Core;
using Microsoft.Data.Sqlite;
using Xunit;

#nullable enable

namespace AutoMemory.Tests.Commands;

/// <summary>
/// Tests for the show command (C1 fix validation).
/// </summary>
public class ShowCommandTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _conn;

    public ShowCommandTests()
    {
        // Create in-memory DB for testing
        _dbPath = $":memory:";
        _conn = new SqliteConnection($"Data Source={_dbPath}");
        _conn.Open();
        SeedDatabase();
    }

    public void Dispose()
    {
        _conn.Close();
        _conn.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SeedDatabase()
    {
        // Create schema
        _conn.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS schema_version (version INTEGER)");
        _conn.ExecuteNonQuery("INSERT OR REPLACE INTO schema_version VALUES (2)");
        _conn.ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS sessions (
                id TEXT PRIMARY KEY, 
                repository TEXT, 
                branch TEXT, 
                summary TEXT,
                created_at TEXT, 
                updated_at TEXT
            )
            """);
        _conn.ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS turns (
                session_id TEXT, 
                turn_index INTEGER, 
                user_message TEXT,
                assistant_response TEXT, 
                timestamp TEXT
            )
            """);
        _conn.ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS session_files (
                session_id TEXT, 
                file_path TEXT, 
                tool_name TEXT, 
                turn_index INTEGER,
                first_seen_at TEXT
            )
            """);
        _conn.ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS session_refs (
                session_id TEXT, 
                ref_type TEXT, 
                ref_value TEXT, 
                turn_index INTEGER,
                created_at TEXT
            )
            """);
        _conn.ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS checkpoints (
                session_id TEXT, 
                checkpoint_number INTEGER, 
                title TEXT,
                overview TEXT, 
                created_at TEXT
            )
            """);

        // Insert test session
        _conn.ExecuteNonQuery("""
            INSERT INTO sessions VALUES (
                'abcd1234-0000-0000-0000-000000000000',
                'repo',
                'main',
                'test session',
                '2026-04-17',
                '2026-04-17'
            )
            """);

        // Insert 10 turns - note: using individual statements for each turn
        _conn.ExecuteNonQuery("""
            INSERT INTO turns VALUES
            ('abcd1234-0000-0000-0000-000000000000', 0, 'user msg 0', 'assistant msg 0', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 1, 'user msg 1', 'assistant msg 1', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 2, 'user msg 2', 'assistant msg 2', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 3, 'user msg 3', 'assistant msg 3', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 4, 'user msg 4', 'assistant msg 4', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 5, 'user msg 5', 'assistant msg 5', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 6, 'user msg 6', 'assistant msg 6', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 7, 'user msg 7', 'assistant msg 7', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 8, 'user msg 8', 'assistant msg 8', '2026-04-17'),
            ('abcd1234-0000-0000-0000-000000000000', 9, 'user msg 9', 'assistant msg 9', '2026-04-17')
            """);
    }

    [Fact]
    public void TurnsPositiveLimitsCorrectly()
    {
        // --turns 3 should return exactly 3 turns on a session with 10
        var args = CreateArgs("abcd1234", "--turns", "3", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
        var result = JsonSerializer.Deserialize<ShowSessionResult>(output.ToString());
        Assert.NotNull(result);
        Assert.Equal(3, result.Turns.Count);
    }

    [Fact]
    public void TurnsZeroReturnsNoTurns()
    {
        // --turns 0 should return zero turns
        var args = CreateArgs("abcd1234", "--turns", "0", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
        var result = JsonSerializer.Deserialize<ShowSessionResult>(output.ToString());
        Assert.NotNull(result);
        Assert.Empty(result.Turns);
    }

    [Fact]
    public void TurnsNoneReturnsAll()
    {
        // Omitting --turns should return all 10 turns
        var args = CreateArgs("abcd1234", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
        var result = JsonSerializer.Deserialize<ShowSessionResult>(output.ToString());
        Assert.NotNull(result);
        Assert.Equal(10, result.Turns.Count);
    }

    [Fact]
    public void TurnsExceedingCountReturnsAll()
    {
        // --turns 9999 should return all 10 (not crash)
        var args = CreateArgs("abcd1234", "--turns", "9999", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
        var result = JsonSerializer.Deserialize<ShowSessionResult>(output.ToString());
        Assert.NotNull(result);
        Assert.Equal(10, result.Turns.Count);
    }

    [Fact]
    public void ShowRejectsPercent()
    {
        // '%' should be rejected as invalid session ID
        var args = CreateArgs("%");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(2, exitCode);
        Assert.Contains("invalid session id", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowRejectsUnderscore()
    {
        // '_' should be rejected as invalid session ID
        var args = CreateArgs("_");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void ShowRejectsEmpty()
    {
        // Empty string should be rejected
        var args = CreateArgs("");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void ShowRejectsNonHex()
    {
        // Non-hex characters should be rejected
        var args = CreateArgs("not-a-uuid");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void ShowRejectsShort()
    {
        // Less than 4 chars should be rejected
        var args = CreateArgs("abc");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void ShowAcceptsHexPrefix()
    {
        // 8-char hex prefix should find the session
        var args = CreateArgs("abcd1234", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void ShowAcceptsFullUuid()
    {
        // Full UUID with dashes should find the session
        var args = CreateArgs("abcd1234-0000-0000-0000-000000000000", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void ShowCaseInsensitive()
    {
        // Uppercase hex should work (lowered internally)
        var args = CreateArgs("ABCD1234", "--json");

        using var output = new StringWriter();
        Console.SetOut(output);

        var exitCode = ShowCommand.RunCore(args, _conn);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TurnsRejectsNegative()
    {
        // --turns -1 should be rejected
        var args = CreateArgs("abcd1234", "--turns", "-1");

        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        var ex = Assert.Throws<UsageException>(() => ShowCommand.RunCore(args, _conn));
        Assert.Contains("--turns", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ParsedArgs CreateArgs(params string[] argv)
    {
        var parser = new ArgParser("auto-memory show", "Show detailed info for a single session")
            .AddPositional("session_id", required: true, help: "Session ID or prefix (4+ chars)")
            .AddOption("turns", defaultValue: null, help: "Limit to last N turns (default: all)")
            .AddOption("full", isFlag: true, help: "Show full turn content (default: truncate at 500 chars)")
            .AddOption("json", isFlag: true, help: "Output as JSON");

        return parser.Parse(argv);
    }
}
