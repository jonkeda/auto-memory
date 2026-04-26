using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Queries;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

#nullable enable

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Implementation of the 'list' subcommand.
/// </summary>
public static class ListCommand
{
    private const string FilesSql = """
        SELECT sf.file_path, sf.tool_name, sf.first_seen_at, sf.session_id
        FROM session_files sf JOIN sessions s ON s.id = sf.session_id
        WHERE sf.file_path LIKE '%.md'
        ORDER BY sf.first_seen_at DESC LIMIT @limit
        """;

    private const string FilesSqlWithRepo = """
        SELECT sf.file_path, sf.tool_name, sf.first_seen_at, sf.session_id
        FROM session_files sf JOIN sessions s ON s.id = sf.session_id
        WHERE sf.file_path LIKE '%.md' AND s.repository = @repo
        ORDER BY sf.first_seen_at DESC LIMIT @limit
        """;

    /// <summary>
    /// Execute the list subcommand.
    /// </summary>
    /// <param name="args">Parsed command-line arguments.</param>
    /// <returns>Exit code: 0=success, 2=schema drift, 3/4=DB errors.</returns>
    public static int Run(ParsedArgs args)
    {
        using var conn = Connect.ConnectReadOnly(Config.DbPath);
        return RunCore(args, conn);
    }

    /// <summary>
    /// Execute the list subcommand with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
        var repo = args.GetOption("repo") ?? DetectRepo.Detect();
        var limit = args.GetInt("limit") ?? 10;
        // Match Python: days=0 is falsy, use default 30
        var daysRaw = args.GetInt("days");
        var days = daysRaw == null || daysRaw == 0 ? 30 : daysRaw.Value;
        var jsonMode = args.GetFlag("json");

        // Schema check
        var schemaResult = SchemaCheck.CheckSchema(conn);
        if (!schemaResult.IsValid)
        {
            Console.Error.WriteLine("❌ Schema drift:");
            foreach (var problem in schemaResult.Problems)
            {
                Console.Error.WriteLine($"   - {problem}");
            }
            return 2;
        }

        // Query sessions
        using var sessionCmd = SessionQueries.SelectRecentSessions(conn, repo, limit, days);
        var sessions = new List<SessionListItem>();
        using (var reader = sessionCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetString(0);
                var createdAt = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                sessions.Add(new SessionListItem
                {
                    IdShort = id.Length >= 8 ? id[..8] : id,
                    IdFull = id,
                    Repository = reader.GetString(1),
                    Branch = reader.GetString(2),
                    Summary = reader.GetString(3),
                    Date = createdAt.Length >= 10 ? createdAt[..10] : null,
                    CreatedAt = createdAt,
                    TurnsCount = reader.GetInt32(6),
                    FilesCount = reader.GetInt32(7)
                });
            }
        }

        // Query recent files
        var recentFiles = QueryRecentFiles(conn, repo, limit: 10);

        // Build result
        var result = new ListResult
        {
            Repo = repo ?? "all",
            Count = sessions.Count,
            Sessions = sessions,
            RecentFiles = recentFiles
        };

        // Output
        FormatOutput.Output(result, jsonMode);

        return 0;
    }

    private static List<RecentFileItem> QueryRecentFiles(SqliteConnection conn, string? repo, int limit)
    {
        var files = new List<RecentFileItem>();
        var cwd = Directory.GetCurrentDirectory();

        using var cmd = conn.CreateCommand();
        if (!string.IsNullOrEmpty(repo) && repo != "all")
        {
            cmd.CommandText = FilesSqlWithRepo;
            cmd.Parameters.AddWithValue("@repo", repo);
            cmd.Parameters.AddWithValue("@limit", limit);
        }
        else
        {
            cmd.CommandText = FilesSql;
            cmd.Parameters.AddWithValue("@limit", limit);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var fullPath = reader.GetString(0);
            var firstSeenAt = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var sessionId = reader.GetString(3);

            // Make relative path if it's under cwd
            var displayPath = fullPath.StartsWith(cwd, StringComparison.Ordinal)
                ? Path.GetRelativePath(cwd, fullPath)
                : fullPath;

            files.Add(new RecentFileItem
            {
                FilePath = displayPath,
                FullPath = fullPath,
                ToolName = reader.GetString(1),
                Date = firstSeenAt.Length >= 10 ? firstSeenAt[..10] : string.Empty,
                SessionId = sessionId.Length >= 8 ? sessionId[..8] : sessionId
            });
        }

        return files;
    }
}
