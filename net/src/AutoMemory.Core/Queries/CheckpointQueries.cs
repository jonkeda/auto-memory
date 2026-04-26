using System;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Queries;

/// <summary>
/// Parameterized SQL queries for checkpoint data.
/// </summary>
public static class CheckpointQueries
{
    private const string BaseQuery = """
        SELECT c.checkpoint_number, c.title, c.overview, c.created_at,
               c.session_id, s.summary as session_summary FROM checkpoints c
               JOIN sessions s ON s.id = c.session_id
        """;

    /// <summary>
    /// Build command for selecting recent checkpoints, optionally filtered by repository and days.
    /// </summary>
    /// <param name="connection">Open SQLite connection.</param>
    /// <param name="repo">Repository name or null for all repositories.</param>
    /// <param name="limit">Maximum number of checkpoints to return.</param>
    /// <param name="days">Number of days to look back (positive value), or null for no date filter.</param>
    /// <returns>Configured SqliteCommand ready to execute.</returns>
    public static SqliteCommand SelectRecentCheckpoints(
        SqliteConnection connection,
        string? repo,
        int limit,
        int? days)
    {
        var cmd = connection.CreateCommand();
        var hasRepo = !string.IsNullOrEmpty(repo) && repo != "all";
        var hasDays = days.HasValue;

        // Build WHERE clause
        string whereClause;
        if (hasRepo && hasDays)
        {
            whereClause = " WHERE s.repository = @repo AND c.created_at >= datetime('now', @days_arg)";
        }
        else if (hasRepo)
        {
            whereClause = " WHERE s.repository = @repo";
        }
        else if (hasDays)
        {
            whereClause = " WHERE c.created_at >= datetime('now', @days_arg)";
        }
        else
        {
            whereClause = "";
        }

        cmd.CommandText = BaseQuery + whereClause + " ORDER BY c.created_at DESC LIMIT @limit";

        // Add parameters
        if (hasRepo)
        {
            cmd.Parameters.AddWithValue("@repo", repo!);
        }
        if (hasDays)
        {
            var daysArg = $"-{days!.Value.ToString(CultureInfo.InvariantCulture)} days";
            cmd.Parameters.AddWithValue("@days_arg", daysArg);
        }
        cmd.Parameters.AddWithValue("@limit", limit);

        return cmd;
    }
}
