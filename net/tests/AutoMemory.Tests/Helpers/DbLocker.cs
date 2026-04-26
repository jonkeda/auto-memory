using System;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Tests.Helpers;

/// <summary>
/// Helper program that holds a write lock on a SQLite database.
/// Usage: DbLocker.exe <db-path> <duration-seconds>
/// </summary>
internal static class DbLocker
{
    public static void HoldLock(string dbPath, int durationSeconds)
    {
        // Ensure the database exists
        if (!File.Exists(dbPath))
        {
            // Create a minimal database
            using var createConn = new SqliteConnection($"Data Source={dbPath}");
            createConn.Open();
            createConn.Close();
        }

        // Open a connection and hold a write lock
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        
        // Start a deferred transaction and upgrade to exclusive
        using var txn = conn.BeginTransaction();
        
        // Execute a write operation to acquire exclusive lock
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS _lock_holder (id INTEGER PRIMARY KEY)";
            cmd.ExecuteNonQuery();
        }
        
        Console.WriteLine($"[DbLocker] Holding write lock on {dbPath} for {durationSeconds} seconds...");
        
        // Hold the lock
        Thread.Sleep(durationSeconds * 1000);
        
        Console.WriteLine("[DbLocker] Releasing lock...");
    }
}
