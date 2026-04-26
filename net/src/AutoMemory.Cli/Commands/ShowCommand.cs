using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

#nullable enable

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Implementation of the 'show' subcommand.
/// </summary>
public static class ShowCommand
{
    private static readonly Regex s_sidRegex = new(@"^[0-9a-fA-F-]{4,}$", RegexOptions.Compiled);

    /// <summary>
    /// Execute the show subcommand.
    /// </summary>
    /// <param name="args">Parsed command-line arguments.</param>
    /// <returns>Exit code: 0=success, 1=not found, 2=bad input/schema drift.</returns>
    public static int Run(ParsedArgs args)
    {
        using var conn = Connect.ConnectReadOnly(Config.DbPath);
        return RunCore(args, conn);
    }

    /// <summary>
    /// Execute the show subcommand with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
        // Schema check
        var schemaResult = SchemaCheck.CheckSchema(conn);
        if (!schemaResult.IsValid)
        {
            foreach (var problem in schemaResult.Problems)
            {
                Console.Error.WriteLine($"   - {problem}");
            }
            return 2;
        }

        // Get and validate session_id
        var sessionIdRaw = args.GetPositional(0);
        if (sessionIdRaw is null)
        {
            Console.Error.WriteLine("error: the following arguments are required: session_id");
            return 2;
        }
        
        var sessionId = sessionIdRaw.Trim();
        
        // Validate session ID format
        if (!s_sidRegex.IsMatch(sessionId) || sessionId.Replace("-", "", StringComparison.Ordinal).Length == 0)
        {
            Console.Error.WriteLine($"error: invalid session id '{sessionIdRaw}' (expected hex, 4+ chars)");
            return 2;
        }

        // Normalize to lowercase
        sessionId = sessionId.ToLowerInvariant();

        // Query for the session
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM sessions WHERE id = @sid OR id LIKE @sidPrefix";
        cmd.Parameters.AddWithValue("@sid", sessionId);
        cmd.Parameters.AddWithValue("@sidPrefix", $"{sessionId}%");

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            Console.Error.WriteLine($"No session found matching '{sessionId}'");
            return 1;
        }

        var fullId = reader.GetString(reader.GetOrdinal("id"));
        var repository = reader.GetString(reader.GetOrdinal("repository"));
        var branch = reader.GetString(reader.GetOrdinal("branch"));
        var summary = reader.GetString(reader.GetOrdinal("summary"));
        var createdAt = reader.GetString(reader.GetOrdinal("created_at"));
        reader.Close();

        // Get turns with optional limit
        var turns = GetTurns(conn, fullId, args);

        // Get files
        var files = GetFiles(conn, fullId);

        // Get refs
        var refs = GetRefs(conn, fullId);

        // Get checkpoints
        var checkpoints = GetCheckpoints(conn, fullId);

        // Build result
        var result = new ShowSessionResult
        {
            Id = fullId,
            Repository = repository,
            Branch = branch,
            Summary = summary,
            CreatedAt = createdAt,
            TurnsCount = turns.Count,
            Turns = turns,
            Files = files,
            Refs = refs,
            Checkpoints = checkpoints
        };

        // Output
        var jsonMode = args.GetFlag("json");
        Console.WriteLine(FormatOutput.FmtJson(result));

        return 0;
    }

    private static List<ShowTurnRecord> GetTurns(SqliteConnection conn, string fullId, ParsedArgs args)
    {
        var fullFlag = args.GetFlag("full");
        var maxLength = fullFlag ? 99999 : 500;

        var turnsStr = args.GetOption("turns");
        var hasTurnsLimit = turnsStr is not null;
        int turnsLimit = 0;

        if (hasTurnsLimit)
        {
            if (!int.TryParse(turnsStr, NumberStyles.None, CultureInfo.InvariantCulture, out turnsLimit) || turnsLimit < 0)
            {
                throw new UsageException($"argument --turns: invalid non_negative_int value: '{turnsStr}'");
            }
        }

        using var cmd = conn.CreateCommand();
        if (hasTurnsLimit)
        {
            cmd.CommandText = """
                SELECT turn_index, user_message, assistant_response, timestamp 
                FROM turns 
                WHERE session_id = @sessionId 
                ORDER BY turn_index 
                LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("@sessionId", fullId);
            cmd.Parameters.AddWithValue("@limit", turnsLimit);
        }
        else
        {
            cmd.CommandText = """
                SELECT turn_index, user_message, assistant_response, timestamp 
                FROM turns 
                WHERE session_id = @sessionId 
                ORDER BY turn_index
                """;
            cmd.Parameters.AddWithValue("@sessionId", fullId);
        }

        var turns = new List<ShowTurnRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var turnIndex = reader.GetInt32(reader.GetOrdinal("turn_index"));
            var userMessage = reader.IsDBNull(reader.GetOrdinal("user_message")) 
                ? "" 
                : reader.GetString(reader.GetOrdinal("user_message"));
            var assistantResponse = reader.IsDBNull(reader.GetOrdinal("assistant_response")) 
                ? "" 
                : reader.GetString(reader.GetOrdinal("assistant_response"));
            var timestamp = reader.GetString(reader.GetOrdinal("timestamp"));

            // Truncate to max length
            if (userMessage.Length > maxLength)
            {
                userMessage = userMessage[..maxLength];
            }
            if (assistantResponse.Length > maxLength)
            {
                assistantResponse = assistantResponse[..maxLength];
            }

            turns.Add(new ShowTurnRecord
            {
                Idx = turnIndex,
                User = userMessage,
                Assistant = assistantResponse,
                Timestamp = timestamp
            });
        }

        return turns;
    }

    private static List<FileRecord> GetFiles(SqliteConnection conn, string fullId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT file_path, tool_name, turn_index 
            FROM session_files 
            WHERE session_id = @sessionId
            """;
        cmd.Parameters.AddWithValue("@sessionId", fullId);

        var files = new List<FileRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            files.Add(new FileRecord
            {
                FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                ToolName = reader.GetString(reader.GetOrdinal("tool_name")),
                TurnIndex = reader.GetInt32(reader.GetOrdinal("turn_index"))
            });
        }

        return files;
    }

    private static List<SessionRefRecord> GetRefs(SqliteConnection conn, string fullId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT ref_type, ref_value, turn_index 
            FROM session_refs 
            WHERE session_id = @sessionId
            """;
        cmd.Parameters.AddWithValue("@sessionId", fullId);

        var refs = new List<SessionRefRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            refs.Add(new SessionRefRecord
            {
                RefType = reader.GetString(reader.GetOrdinal("ref_type")),
                RefValue = reader.GetString(reader.GetOrdinal("ref_value")),
                TurnIndex = reader.GetInt32(reader.GetOrdinal("turn_index"))
            });
        }

        return refs;
    }

    private static List<ShowCheckpointRecord> GetCheckpoints(SqliteConnection conn, string fullId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT checkpoint_number, title, overview 
            FROM checkpoints 
            WHERE session_id = @sessionId 
            ORDER BY checkpoint_number
            """;
        cmd.Parameters.AddWithValue("@sessionId", fullId);

        var checkpoints = new List<ShowCheckpointRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var overview = reader.IsDBNull(reader.GetOrdinal("overview")) 
                ? "" 
                : reader.GetString(reader.GetOrdinal("overview"));
            
            // Truncate overview to 300 chars
            if (overview.Length > 300)
            {
                overview = overview[..300];
            }

            checkpoints.Add(new ShowCheckpointRecord
            {
                N = reader.GetInt32(reader.GetOrdinal("checkpoint_number")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Overview = overview
            });
        }

        return checkpoints;
    }
}
