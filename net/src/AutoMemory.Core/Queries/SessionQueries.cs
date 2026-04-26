using System;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Queries;

/// <summary>
/// Parameterized SQL queries for session data.
/// </summary>
public static class SessionQueries
{
    private const string QueryRepo = """
        SELECT s.id, s.repository, s.branch, s.summary, s.created_at, s.updated_at,
               (SELECT COUNT(*) FROM turns t WHERE t.session_id = s.id) as turns_count,
               (SELECT COUNT(*) FROM session_files f WHERE f.session_id = s.id) as files_count
        FROM sessions s WHERE s.repository = @repo AND s.created_at >= datetime('now', @days_arg)
        ORDER BY s.created_at DESC LIMIT @limit
        """;

    private const string QueryAll = """
        SELECT s.id, s.repository, s.branch, s.summary, s.created_at, s.updated_at,
               (SELECT COUNT(*) FROM turns t WHERE t.session_id = s.id) as turns_count,
               (SELECT COUNT(*) FROM session_files f WHERE f.session_id = s.id) as files_count
        FROM sessions s WHERE s.created_at >= datetime('now', @days_arg)
        ORDER BY s.created_at DESC LIMIT @limit
        """;

    /// <summary>
    /// Build command for selecting recent sessions, optionally filtered by repository.
    /// </summary>
    /// <param name="connection">Open SQLite connection.</param>
    /// <param name="repo">Repository name or null for all repositories.</param>
    /// <param name="limit">Maximum number of sessions to return.</param>
    /// <param name="days">Number of days to look back (positive value).</param>
    /// <returns>Configured SqliteCommand ready to execute.</returns>
    public static SqliteCommand SelectRecentSessions(
        SqliteConnection connection,
        string? repo,
        int limit,
        int days)
    {
        var daysArg = $"-{days.ToString(CultureInfo.InvariantCulture)} days";
        var cmd = connection.CreateCommand();

        if (!string.IsNullOrEmpty(repo) && repo != "all")
        {
            cmd.CommandText = QueryRepo;
            cmd.Parameters.AddWithValue("@repo", repo);
            cmd.Parameters.AddWithValue("@days_arg", daysArg);
            cmd.Parameters.AddWithValue("@limit", limit);
        }
        else
        {
            cmd.CommandText = QueryAll;
            cmd.Parameters.AddWithValue("@days_arg", daysArg);
            cmd.Parameters.AddWithValue("@limit", limit);
        }

        return cmd;
    }
}
