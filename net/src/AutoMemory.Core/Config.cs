using System;
using System.IO;

namespace AutoMemory.Core;

/// <summary>
/// Configuration constants for auto-memory CLI.
/// </summary>
public static class Config
{
    /// <summary>
    /// Path to the session store database.
    /// Overrideable via SESSION_RECALL_DB environment variable.
    /// Default: ~/.copilot/session-store.db
    /// </summary>
    public static readonly string DbPath;

    /// <summary>
    /// Path to the telemetry JSON log file.
    /// Overrideable via SESSION_RECALL_TELEMETRY environment variable.
    /// Default: ~/.copilot/scripts/.session-recall-stats.json
    /// </summary>
    public static readonly string TelemetryPath;

    /// <summary>
    /// Retry delay schedule in milliseconds for SQLite busy/locked errors.
    /// </summary>
    public static readonly int[] RetryDelaysMs = { 50, 150, 450 };

    /// <summary>
    /// Maximum number of retry attempts (derived from RetryDelaysMs length).
    /// </summary>
    public static readonly int MaxRetries = 3;

    /// <summary>
    /// Expected schema version of the session store database.
    /// </summary>
    public static readonly int ExpectedSchemaVersion = 1;

    static Config()
    {
        var home = GetHomePath();

        DbPath = Environment.GetEnvironmentVariable("SESSION_RECALL_DB")
            ?? Path.GetFullPath(Path.Combine(home, ".copilot", "session-store.db"));

        TelemetryPath = Environment.GetEnvironmentVariable("SESSION_RECALL_TELEMETRY")
            ?? Path.GetFullPath(Path.Combine(home, ".copilot", "scripts", ".session-recall-stats.json"));
    }

    private static string GetHomePath()
    {
        // Try UserProfile first (standard on Windows)
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
            return userProfile;

        // WSL2 edge case: fallback to HOME env var if UserProfile is unset
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(home))
            return home;

        throw new InvalidOperationException(
            "Unable to determine home directory: neither UserProfile nor HOME are set");
    }
}
