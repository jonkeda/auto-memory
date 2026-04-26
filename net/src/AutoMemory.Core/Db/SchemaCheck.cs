using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Db;

/// <summary>
/// Result of schema validation check.
/// </summary>
public sealed record SchemaCheckResult(bool IsValid, IReadOnlyList<string> Problems);

/// <summary>
/// Schema validation against expected Copilot CLI session-store.db structure.
/// </summary>
public static class SchemaCheck
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> s_expectedSchema = 
        new Dictionary<string, IReadOnlySet<string>>
        {
            ["sessions"] = new HashSet<string> 
            { 
                "id", "repository", "branch", "summary", "created_at", "updated_at" 
            },
            ["turns"] = new HashSet<string> 
            { 
                "session_id", "turn_index", "user_message", "assistant_response", "timestamp" 
            },
            ["session_files"] = new HashSet<string> 
            { 
                "session_id", "file_path", "tool_name", "turn_index", "first_seen_at" 
            },
            ["session_refs"] = new HashSet<string> 
            { 
                "session_id", "ref_type", "ref_value", "turn_index", "created_at" 
            },
            ["checkpoints"] = new HashSet<string> 
            { 
                "session_id", "checkpoint_number", "title", "overview", "created_at" 
            }
        };

    /// <summary>
    /// Validate DB schema. Returns list of problems (empty = OK).
    /// </summary>
    /// <param name="conn">Open database connection.</param>
    /// <returns>Result with validation status and any problems found.</returns>
    public static SchemaCheckResult CheckSchema(SqliteConnection conn)
    {
        var problems = new List<string>();

        foreach (var (table, expectedCols) in s_expectedSchema)
        {
            using var cmd = conn.CreateCommand();
#pragma warning disable AUTOMEM001 // table names come from s_expectedSchema dictionary - safe for PRAGMA
            cmd.CommandText = $"PRAGMA table_info({table})";
#pragma warning restore AUTOMEM001
            
            var actualCols = new HashSet<string>(StringComparer.Ordinal);
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    problems.Add($"MISSING TABLE: {table}");
                    continue;
                }

                while (reader.Read())
                {
                    // Column name is at index 1 (0=cid, 1=name, 2=type, 3=notnull, 4=dflt_value, 5=pk)
                    var colName = reader.GetString(1);
                    actualCols.Add(colName);
                }
            }

            // Check for missing columns (extra columns are OK)
            var missingCols = expectedCols.Except(actualCols).ToHashSet();
            if (missingCols.Count > 0)
            {
                // Format as Python set literal for exact message match
                var missingSet = "{" + string.Join(", ", missingCols.OrderBy(c => c).Select(c => $"'{c}'")) + "}";
                problems.Add($"{table}: missing columns {missingSet}");
            }
        }

        return new SchemaCheckResult(problems.Count == 0, problems);
    }
}
