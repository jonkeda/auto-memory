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
/// Implementation of the 'checkpoints' subcommand.
/// </summary>
public static class CheckpointsCommand
{
    /// <summary>
    /// Execute the checkpoints subcommand.
    /// </summary>
    /// <param name="args">Parsed command-line arguments.</param>
    /// <returns>Exit code: 0=success, 2=schema drift, 3/4=DB errors.</returns>
    public static int Run(ParsedArgs args)
    {
        using var conn = Connect.ConnectReadOnly(Config.DbPath);
        return RunCore(args, conn);
    }

    /// <summary>
    /// Execute the checkpoints subcommand with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
        var repo = args.GetOption("repo") ?? DetectRepo.Detect();
        var limit = args.GetInt("limit") ?? 5;
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

        // Query checkpoints
        using var checkpointsCmd = CheckpointQueries.SelectRecentCheckpoints(conn, repo, limit, days);
        var checkpoints = new List<CheckpointsCheckpointItem>();
        using (var reader = checkpointsCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var checkpointNumber = reader.GetInt32(0);
                var title = reader.GetString(1);
                var overview = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var createdAt = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var sessionId = reader.GetString(4);
                var sessionSummary = reader.IsDBNull(5) ? null : reader.GetString(5);

                // Truncate overview to 300 chars to match Python
                var truncatedOverview = overview.Length > 300 ? overview[..300] : overview;

                checkpoints.Add(new CheckpointsCheckpointItem
                {
                    CheckpointNumber = checkpointNumber,
                    Title = title,
                    Overview = truncatedOverview,
                    Date = createdAt.Length >= 10 ? createdAt[..10] : string.Empty,
                    SessionId = sessionId.Length >= 8 ? sessionId[..8] : sessionId,
                    SessionSummary = sessionSummary
                });
            }
        }

        // Build result
        var result = new CheckpointsResult
        {
            Repo = repo ?? "all",
            Count = checkpoints.Count,
            Checkpoints = checkpoints
        };

        // Output
        FormatOutput.Output(result, jsonMode);

        return 0;
    }
}
