using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Db;

/// <summary>
/// Exception thrown when database file is not found.
/// </summary>
public sealed class DatabaseNotFoundException : Exception
{
    public DatabaseNotFoundException(string path) 
        : base($"error: database not found: {path}")
    {
        DbPath = path;
    }

    public string DbPath { get; }
}

/// <summary>
/// Exception thrown when database is locked after all retries exhausted.
/// </summary>
public sealed class DatabaseLockedException : Exception
{
    public DatabaseLockedException() 
        : base("error: database is locked — another session-recall process may be running")
    {
    }
}

/// <summary>
/// Read-only SQLite connection with exponential backoff retry.
/// </summary>
public static class Connect
{
    // Delay schedule: immediate attempt (0ms) followed by exponential backoff
    private static readonly int[] s_delaySchedule = [0, .. Config.RetryDelaysMs];

    /// <summary>
    /// Open read-only connection with busy timeout and retry on SQLITE_BUSY/SQLITE_LOCKED.
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file.</param>
    /// <returns>An open, read-only connection with query_only pragma enforced.</returns>
    /// <exception cref="DatabaseNotFoundException">When database file does not exist (exit code 4).</exception>
    /// <exception cref="DatabaseLockedException">When database is locked after all retries (exit code 3).</exception>
    public static SqliteConnection ConnectReadOnly(string dbPath)
    {
        return ConnectReadOnlyCore(dbPath);
    }

    /// <summary>
    /// Internal testable variant that throws exceptions instead of calling Environment.Exit.
    /// </summary>
    internal static SqliteConnection ConnectReadOnlyCore(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            throw new DatabaseNotFoundException(dbPath);
        }

        var random = new Random();
        Exception? lastErr = null;

        // First attempt with no delay, then retry with exponential backoff
        foreach (var delayMs in s_delaySchedule)
        {
            if (delayMs > 0)
            {
                // Sleep delay * random(0.8, 1.2) milliseconds
                var jitter = random.NextDouble() * 0.4 + 0.8; // [0.8, 1.2)
                var sleepMs = (int)(delayMs * jitter);
                System.Threading.Thread.Sleep(sleepMs);
            }

            try
            {
                var connString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                var conn = new SqliteConnection(connString);
                conn.Open();

                // Apply read-only PRAGMAs
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA busy_timeout = 500";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA query_only = ON";
                    cmd.ExecuteNonQuery();
                }

                return conn;
            }
            catch (SqliteException ex)
            {
                lastErr = ex;

                // Check if error is busy/locked using both error code and message
                var message = ex.Message ?? string.Empty;
                var isLockedOrBusy = message.Contains("locked", StringComparison.OrdinalIgnoreCase)
                                  || message.Contains("busy", StringComparison.OrdinalIgnoreCase);

                // SqliteErrorCode 5 is SQLITE_BUSY, but we also check message for robustness
                if (!isLockedOrBusy && ex.SqliteErrorCode != 5)
                {
                    throw;
                }

                // Continue retry loop for locked/busy errors
            }
        }

        // All retries exhausted
        throw new DatabaseLockedException();
    }
}
