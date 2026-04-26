using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoMemory.Core.Util;

/// <summary>
/// Telemetry ring buffer for session-recall invocations.
/// </summary>
public static class Telemetry
{
    private static string? _telemetryPath;
    private const int MaxEntries = 500;
    private static readonly object _lock = new();
    
    /// <summary>
    /// Command tier classification (matches Python TIER_MAP).
    /// Tier 0 = meta/ops; Tier 1 = cheap scan; Tier 2 = focused search; Tier 3 = deep dive.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, int> TierMap = new Dictionary<string, int>
    {
        ["list"] = 1,
        ["files"] = 1,
        ["checkpoints"] = 1,
        ["search"] = 2,
        ["show"] = 3,
        ["health"] = 0,
        ["schema-check"] = 0,
        ["calibrate"] = 0,
    };
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Initialize telemetry with the target file path. Null or empty disables telemetry.
    /// </summary>
    public static void Init(string? path)
    {
        _telemetryPath = string.IsNullOrEmpty(path) ? null : path;
    }

    /// <summary>
    /// 8-char SHA-256 hash of whitespace-normalized, lowercased query.
    /// Collision-tolerant, not reversible. Use for repetition detection without logging raw query.
    /// </summary>
    public static string QueryHash(string query)
    {
        // Normalize: lowercase, collapse whitespace runs to single space, trim
        var normalized = string.Join(" ", query.ToLowerInvariant().Split(
            (char[]?)null, 
            StringSplitOptions.RemoveEmptyEntries));
        
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    /// <summary>
    /// Append entry to ring buffer. Silent fail on any error.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TelemetryWrapper and TelemetryEntry use JsonConverter attributes; types are preserved.")]
    public static void Record(
        string cmd,
        int durationMs,
        int busyHits = 0,
        int attempts = 1,
        int rows = 0,
        int exitCode = 0,
        bool schemaOk = true,
        int? tier = null,
        string? queryHash = null,
        string? sessionIdPrefix = null,
        string? windowTier = null)
    {
        if (_telemetryPath is null)
            return;

        try
        {
            // Synchronize to prevent concurrent writes from corrupting the file
            lock (_lock)
            {
                // Read existing entries
                var entries = new List<TelemetryEntry>();
                if (File.Exists(_telemetryPath))
                {
                    var json = File.ReadAllText(_telemetryPath);
                    var wrapper = JsonSerializer.Deserialize<TelemetryWrapper>(json, JsonOptions);
                    if (wrapper?.Entries is not null)
                        entries = wrapper.Entries;
                }

                // Create new entry
                var entry = new TelemetryEntry
                {
                    Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    Cmd = cmd,
                    DurationMs = durationMs,
                    BusyHits = busyHits,
                    Attempts = attempts,
                    RowsReturned = rows,
                    ExitCode = exitCode,
                    SchemaOk = schemaOk,
                    Tier = tier,
                    QueryHash = queryHash,
                    SessionIdPrefix = sessionIdPrefix,
                    WindowTier = windowTier
                };

                entries.Add(entry);

                // Ring buffer: keep last 500
                if (entries.Count > MaxEntries)
                    entries = entries.Skip(entries.Count - MaxEntries).ToList();

                // Write back
                var outputWrapper = new TelemetryWrapper { Entries = entries };

                // Ensure parent directory exists (best-effort)
                var directory = Path.GetDirectoryName(_telemetryPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var outputJson = JsonSerializer.Serialize(outputWrapper, JsonOptions);
                
                // Use FileShare.ReadWrite to allow concurrent readers
                // Write without BOM for JSON compatibility with Python
                using var stream = new FileStream(_telemetryPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(outputJson);
            }
        }
        catch (Exception ex)
        {
            // Silent fail — telemetry must never crash the CLI
            // Only log to stderr if SESSION_RECALL_TELEMETRY_DEBUG=1
            var debugEnabled = Environment.GetEnvironmentVariable("SESSION_RECALL_TELEMETRY_DEBUG");
            if (debugEnabled == "1")
            {
                Console.Error.WriteLine($"[telemetry] {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private sealed class TelemetryWrapper
    {
        public List<TelemetryEntry> Entries { get; set; } = new();
    }

    private sealed class TelemetryEntry
    {
        public string Ts { get; set; } = string.Empty;

        public string Cmd { get; set; } = string.Empty;

        public int DurationMs { get; set; }

        public int BusyHits { get; set; }

        public int Attempts { get; set; }

        public int RowsReturned { get; set; }

        public int ExitCode { get; set; }

        public bool SchemaOk { get; set; }

        public int? Tier { get; set; }

        public string? QueryHash { get; set; }

        public string? SessionIdPrefix { get; set; }

        public string? WindowTier { get; set; }
    }
}
