using System;
using System.Collections.Generic;
using System.Globalization;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Search;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

#nullable enable

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Implementation of the 'search' subcommand.
/// </summary>
public static class SearchCommand
{
    private const string SearchIndexSql = """
        SELECT si.content, si.session_id, si.source_type,
               s.summary, s.created_at, s.repository
        FROM search_index si JOIN sessions s ON s.id = si.session_id
        WHERE search_index MATCH @query
        """;

    private const string SessionFilesSql = """
        SELECT sf.file_path, sf.session_id, sf.tool_name, sf.first_seen_at,
               s.summary, s.created_at, s.repository
        FROM session_files sf JOIN sessions s ON s.id = sf.session_id
        WHERE sf.file_path LIKE @pattern
        """;

    private const string SearchIndexSqlWithRepo = """
        SELECT si.content, si.session_id, si.source_type,
               s.summary, s.created_at, s.repository
        FROM search_index si JOIN sessions s ON s.id = si.session_id
        WHERE search_index MATCH @query AND s.repository = @repo
        """;

    private const string SessionFilesSqlWithRepo = """
        SELECT sf.file_path, sf.session_id, sf.tool_name, sf.first_seen_at,
               s.summary, s.created_at, s.repository
        FROM session_files sf JOIN sessions s ON s.id = sf.session_id
        WHERE sf.file_path LIKE @pattern AND s.repository = @repo
        """;

    /// <summary>
    /// Execute the search subcommand.
    /// </summary>
    /// <param name="args">Parsed command-line arguments.</param>
    /// <returns>Exit code: 0=success, 2=schema drift, 3/4=DB errors.</returns>
    public static int Run(ParsedArgs args)
    {
        using var conn = Connect.ConnectReadOnly(Config.DbPath);
        return RunCore(args, conn);
    }

    /// <summary>
    /// Execute the search subcommand with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
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

        var rawQuery = args.GetPositional(0) ?? string.Empty;
        var repo = args.GetOption("repo") ?? DetectRepo.Detect();
        var limit = args.GetInt("limit") ?? 5;
        var days = args.GetInt("days");
        var jsonMode = args.GetFlag("json");

        // Sanitize FTS5 query
        var ftsQuery = FtsSanitizer.SanitizeFts5Query(rawQuery);
        if (ftsQuery is null)
        {
            var emptyResult = new SearchResult
            {
                Query = rawQuery,
                Repo = repo ?? "all",
                Count = 0,
                Results = new List<SearchResultItem>(),
                Warning = "Empty query — nothing to search"
            };
            FormatOutput.Output(emptyResult, jsonMode);
            return 0;
        }

        var hasRepo = !string.IsNullOrEmpty(repo) && repo != "all";

        // Query search_index via FTS5
        var results = new List<SearchResultItem>();
        var seen = new HashSet<(string sessionId, string sourceType)>();

        using (var cmd = conn.CreateCommand())
        {
            // Build SQL with repo and days filters
            var sql = hasRepo ? SearchIndexSqlWithRepo : SearchIndexSql;
            if (days.HasValue)
            {
                sql += " AND s.created_at >= datetime('now', @days)";
            }
            sql += " ORDER BY rank LIMIT @limit";

            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@query", ftsQuery);
            if (hasRepo)
            {
                cmd.Parameters.AddWithValue("@repo", repo);
            }

            if (days.HasValue)
            {
                cmd.Parameters.AddWithValue("@days", $"-{days.Value} days");
            }
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var sessionId = reader.GetString(1);
                var sourceType = reader.GetString(2);
                var content = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var summaryRaw = reader.IsDBNull(3) ? null : reader.GetString(3);
                var summary = string.IsNullOrEmpty(summaryRaw) ? null : summaryRaw;
                var createdAt = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

                seen.Add((sessionId, sourceType));

                results.Add(new SearchResultItem
                {
                    SessionId = sessionId.Length >= 8 ? sessionId[..8] : sessionId,
                    SessionIdFull = sessionId,
                    SourceType = sourceType,
                    Summary = summary,
                    Date = createdAt.Length >= 10 ? createdAt[..10] : string.Empty,
                    Excerpt = content.Length > 200 ? content[..200] : content
                });
            }
        }

        // Query session_files via LIKE pattern
        var likePattern = $"%{rawQuery}%";
        using (var cmd = conn.CreateCommand())
        {
            // Build SQL with repo and days filters
            var sql = hasRepo ? SessionFilesSqlWithRepo : SessionFilesSql;
            if (days.HasValue)
            {
                sql += " AND s.created_at >= datetime('now', @days)";
            }
            sql += " ORDER BY sf.first_seen_at DESC LIMIT @limit";

            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@pattern", likePattern);
            if (hasRepo)
            {
                cmd.Parameters.AddWithValue("@repo", repo);
            }

            if (days.HasValue)
            {
                cmd.Parameters.AddWithValue("@days", $"-{days.Value} days");
            }
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var sessionId = reader.GetString(1);
                if (seen.Contains((sessionId, "file")))
                {
                    continue;
                }

                var filePath = reader.GetString(0);
                var toolName = reader.GetString(2);
                var summaryRaw = reader.IsDBNull(4) ? null : reader.GetString(4);
                var summary = string.IsNullOrEmpty(summaryRaw) ? null : summaryRaw;
                var createdAt = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);

                seen.Add((sessionId, "file"));

                results.Add(new SearchResultItem
                {
                    SessionId = sessionId.Length >= 8 ? sessionId[..8] : sessionId,
                    SessionIdFull = sessionId,
                    SourceType = "file",
                    Summary = summary,
                    Date = createdAt.Length >= 10 ? createdAt[..10] : string.Empty,
                    Excerpt = $"{filePath} ({toolName})"
                });
            }
        }

        // Limit total results
        if (results.Count > limit)
        {
            results = results.GetRange(0, limit);
        }

        var searchResult = new SearchResult
        {
            Query = rawQuery,
            Repo = repo ?? "all",
            Count = results.Count,
            Results = results
        };

        FormatOutput.Output(searchResult, jsonMode);

        return 0;
    }
}
