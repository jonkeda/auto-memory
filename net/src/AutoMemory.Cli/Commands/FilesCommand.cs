using System;
using System.Collections.Generic;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Queries;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

#nullable enable

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Implementation of the 'files' subcommand.
/// </summary>
public static class FilesCommand
{
    /// <summary>
    /// Execute the files subcommand.
    /// </summary>
    /// <param name="args">Parsed command-line arguments.</param>
    /// <returns>Exit code: 0=success, 2=schema drift, 3/4=DB errors.</returns>
    public static int Run(ParsedArgs args)
    {
        using var conn = Connect.ConnectReadOnly(Config.DbPath);
        return RunCore(args, conn);
    }

    /// <summary>
    /// Execute the files subcommand with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
        var repo = args.GetOption("repo") ?? DetectRepo.Detect();
        var limit = args.GetInt("limit") ?? 10;
        var days = args.GetInt("days"); // No default per Python
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

        // Query files
        using var filesCmd = FileQueries.SelectRecentFiles(conn, repo, limit, days);
        var files = new List<FilesFileItem>();
        using (var reader = filesCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var filePath = reader.GetString(0);
                var toolName = reader.GetString(1);
                var firstSeenAt = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var sessionId = reader.GetString(3);
                var summary = reader.IsDBNull(4) ? null : reader.GetString(4);

                files.Add(new FilesFileItem
                {
                    FilePath = filePath,
                    ToolName = toolName,
                    Date = firstSeenAt.Length >= 10 ? firstSeenAt[..10] : string.Empty,
                    SessionId = sessionId.Length >= 8 ? sessionId[..8] : sessionId,
                    SessionSummary = summary
                });
            }
        }

        // Build result
        var result = new FilesResult
        {
            Repo = repo ?? "all",
            Count = files.Count,
            Files = files
        };

        // Output
        FormatOutput.Output(result, jsonMode);

        return 0;
    }
}
