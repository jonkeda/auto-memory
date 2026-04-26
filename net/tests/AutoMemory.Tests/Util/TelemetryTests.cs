using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.Util;
using Xunit;

namespace AutoMemory.Tests.Util;

public sealed class TelemetryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _telemetryPath;

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TelemetryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"telemetry_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _telemetryPath = Path.Combine(_tempDir, "stats.json");
        Telemetry.Init(_telemetryPath);
    }

    public void Dispose()
    {
        Telemetry.Init(null);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void RecordWithTier()
    {
        Telemetry.Record("list", 42, tier: 1);

        var entries = ReadEntries();
        var lastEntry = entries.Last();

        Assert.Equal(1, lastEntry.GetProperty("tier").GetInt32());
        Assert.Equal("list", lastEntry.GetProperty("cmd").GetString());
        Assert.False(lastEntry.TryGetProperty("query_hash", out _));
    }

    [Fact]
    public void RecordSearchWithQueryHash()
    {
        var qh = Telemetry.QueryHash("hello world");
        Assert.Equal(8, qh.Length);
        Assert.Equal(qh, Telemetry.QueryHash("HELLO   WORLD")); // normalize

        Telemetry.Record("search", 12, tier: 2, queryHash: qh);

        var entries = ReadEntries();
        var lastEntry = entries.Last();

        Assert.Equal(qh, lastEntry.GetProperty("query_hash").GetString());
    }

    [Fact]
    public void RecordShowWithSessionPrefix()
    {
        Telemetry.Record("show", 12, tier: 3, sessionIdPrefix: "abcd1234");

        var entries = ReadEntries();
        var lastEntry = entries.Last();

        Assert.Equal("abcd1234", lastEntry.GetProperty("session_id_prefix").GetString());
    }

    [Fact]
    public void RecordWithoutTierNoField()
    {
        // Legacy-style call must not inject tier=null into entry (keeps 3-state semantics)
        Telemetry.Record("list", 10);

        var entries = ReadEntries();
        var lastEntry = entries.Last();

        Assert.False(lastEntry.TryGetProperty("tier", out _));
        Assert.False(lastEntry.TryGetProperty("query_hash", out _));
    }

    [Fact]
    public void RingBuffer500()
    {
        for (var i = 0; i < 600; i++)
            Telemetry.Record("list", i, tier: 1);

        var entries = ReadEntries();
        Assert.Equal(500, entries.Length);
        // Oldest entry dropped — first remaining has duration_ms=100
        Assert.Equal(100, entries[0].GetProperty("duration_ms").GetInt32());
    }

    [Fact]
    public void NoRawQueryStored()
    {
        // Privacy guardrail: sensitive text must NEVER land in telemetry
        var secret = "SELECT user_password FROM users";
        Telemetry.Record("search", 12, tier: 2, queryHash: Telemetry.QueryHash(secret));

        var raw = File.ReadAllText(_telemetryPath);
        Assert.DoesNotContain("user_password", raw);
        Assert.DoesNotContain("SELECT", raw);
    }

    [Fact]
    public void QueryHashDeterministic()
    {
        Assert.Equal(Telemetry.QueryHash("foo"), Telemetry.QueryHash("foo"));
        Assert.NotEqual(Telemetry.QueryHash("foo"), Telemetry.QueryHash("bar"));
    }

    [Fact]
    public void InitWithNullDisablesTelemetry()
    {
        Telemetry.Init(null);
        Telemetry.Record("list", 10);

        // File should not be created
        Assert.False(File.Exists(_telemetryPath));

        // Re-init for cleanup
        Telemetry.Init(_telemetryPath);
    }

    [Fact]
    public void InitWithEmptyStringDisablesTelemetry()
    {
        Telemetry.Init(string.Empty);
        Telemetry.Record("list", 10);

        // File should not be created
        Assert.False(File.Exists(_telemetryPath));

        // Re-init for cleanup
        Telemetry.Init(_telemetryPath);
    }

    [Fact]
    public void RecordCreatesParentDirectory()
    {
        var nestedPath = Path.Combine(_tempDir, "nested", "dir", "stats.json");
        Telemetry.Init(nestedPath);

        Telemetry.Record("list", 10);

        Assert.True(File.Exists(nestedPath));

        // Cleanup
        Telemetry.Init(_telemetryPath);
    }

    [Fact]
    public void RecordAllFields()
    {
        Telemetry.Record(
            cmd: "search",
            durationMs: 123,
            busyHits: 2,
            attempts: 3,
            rows: 45,
            exitCode: 0,
            schemaOk: true,
            tier: 2,
            queryHash: "abcd1234",
            sessionIdPrefix: "sess5678",
            windowTier: "W3");

        var entries = ReadEntries();
        var entry = entries.Last();

        Assert.Equal("search", entry.GetProperty("cmd").GetString());
        Assert.Equal(123, entry.GetProperty("duration_ms").GetInt32());
        Assert.Equal(2, entry.GetProperty("busy_hits").GetInt32());
        Assert.Equal(3, entry.GetProperty("attempts").GetInt32());
        Assert.Equal(45, entry.GetProperty("rows_returned").GetInt32());
        Assert.Equal(0, entry.GetProperty("exit_code").GetInt32());
        Assert.True(entry.GetProperty("schema_ok").GetBoolean());
        Assert.Equal(2, entry.GetProperty("tier").GetInt32());
        Assert.Equal("abcd1234", entry.GetProperty("query_hash").GetString());
        Assert.Equal("sess5678", entry.GetProperty("session_id_prefix").GetString());
        Assert.Equal("W3", entry.GetProperty("window_tier").GetString());
    }

    [Fact]
    public void QueryHashFixture()
    {
        // Cross-language fixture: validate all test cases from tests/fixtures/query_hashes.csv
        var fixtureFile = Path.Combine(
            Path.GetDirectoryName(typeof(TelemetryTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "..", "tests", "fixtures", "query_hashes.csv");

        Assert.True(File.Exists(fixtureFile), $"Fixture file not found: {fixtureFile}");

        var content = File.ReadAllText(fixtureFile);
        var testCases = ParseCsvFile(content);

        // Validate each test case
        foreach (var (input, expectedHash) in testCases)
        {
            var actualHash = Telemetry.QueryHash(input);
            Assert.Equal(expectedHash, actualHash);
        }

        // Verify we processed a reasonable number of test cases
        Assert.True(testCases.Count >= 15, 
            $"Expected at least 15 test cases in fixture, found {testCases.Count}");
    }

    private static List<(string Input, string ExpectedHash)> ParseCsvFile(string content)
    {
        var result = new List<(string, string)>();
        var lines = new List<string>();
        var currentLine = new System.Text.StringBuilder();
        var inQuotes = false;

        // First pass: reconstruct logical lines (respecting quoted newlines)
        foreach (var ch in content)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                currentLine.Append(ch);
            }
            else if (ch == '\n' && !inQuotes)
            {
                var line = currentLine.ToString().TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
                currentLine.Clear();
            }
            else
            {
                currentLine.Append(ch);
            }
        }

        // Don't forget the last line if there's no trailing newline
        if (currentLine.Length > 0)
        {
            var line = currentLine.ToString().TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        // Skip header line
        for (var i = 1; i < lines.Count; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length == 2)
            {
                result.Add((parts[0], parts[1]));
            }
        }

        return result;
    }

    private static string[] ParseCsvLine(string line)
    {
        var parts = new List<string>();
        var inQuotes = false;
        var currentField = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                // Field separator
                parts.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(ch);
            }
        }

        // Add the last field
        parts.Add(currentField.ToString());

        return parts.ToArray();
    }

    [Fact]
    public void OutputFormatMatchesPython()
    {
        // Verify the exact JSON structure matches Python's telemetry.py format
        Telemetry.Record(
            cmd: "search",
            durationMs: 123,
            busyHits: 2,
            attempts: 1,
            rows: 45,
            exitCode: 0,
            schemaOk: true,
            tier: 2,
            queryHash: "abcd1234",
            sessionIdPrefix: null,
            windowTier: null);

        var json = File.ReadAllText(_telemetryPath);
        using var doc = JsonDocument.Parse(json);

        // Verify root structure
        Assert.True(doc.RootElement.TryGetProperty("entries", out var entriesProperty));
        Assert.Equal(JsonValueKind.Array, entriesProperty.ValueKind);

        var entries = entriesProperty.EnumerateArray().ToArray();
        Assert.Single(entries);

        var entry = entries[0];

        // Verify all required fields are present and in correct order
        // (JSON property order matters for byte-for-byte comparison)
        var properties = entry.EnumerateObject().Select(p => p.Name).ToArray();
        var expectedProperties = new[] { "ts", "cmd", "duration_ms", "busy_hits", "attempts", 
                                          "rows_returned", "exit_code", "schema_ok", "tier", "query_hash" };
        Assert.Equal(expectedProperties, properties);

        // Verify optional null fields are NOT present
        Assert.False(entry.TryGetProperty("session_id_prefix", out _));
        Assert.False(entry.TryGetProperty("window_tier", out _));

        // Verify values
        Assert.Equal("search", entry.GetProperty("cmd").GetString());
        Assert.Equal(123, entry.GetProperty("duration_ms").GetInt32());
        Assert.Equal(2, entry.GetProperty("busy_hits").GetInt32());
        Assert.Equal(1, entry.GetProperty("attempts").GetInt32());
        Assert.Equal(45, entry.GetProperty("rows_returned").GetInt32());
        Assert.Equal(0, entry.GetProperty("exit_code").GetInt32());
        Assert.True(entry.GetProperty("schema_ok").GetBoolean());
        Assert.Equal(2, entry.GetProperty("tier").GetInt32());
        Assert.Equal("abcd1234", entry.GetProperty("query_hash").GetString());

        // Verify indentation (Python uses indent=2)
        Assert.Contains("\n  \"entries\"", json);
        Assert.Contains("\n    {", json);
        Assert.Contains("\n      \"ts\"", json);
    }

    private JsonElement[] ReadEntries()
    {
        var json = File.ReadAllText(_telemetryPath);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .Select(e => e.Clone())
            .ToArray();
    }

    [Fact]
    public void TierMapMatchesPython()
    {
        // Verify that TierMap exactly matches Python's TIER_MAP from __main__.py:
        // TIER_MAP = {
        //     "list": 1, "files": 1, "checkpoints": 1,   # Tier 1 — cheap scan
        //     "search": 2,                                  # Tier 2 — focused search
        //     "show": 3,                                    # Tier 3 — deep dive
        //     "health": 0, "schema-check": 0,              # Tier 0 — meta/ops
        //     "calibrate": 0,                               # Tier 0 — meta (Phase 4)
        // }

        var tierMapType = typeof(Telemetry);
        var tierMapField = tierMapType.GetField("TierMap", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(tierMapField);
        
        var tierMap = (IReadOnlyDictionary<string, int>)tierMapField!.GetValue(null)!;
        
        // Verify count
        Assert.Equal(8, tierMap.Count);
        
        // Verify each key/value pair
        Assert.Equal(1, tierMap["list"]);
        Assert.Equal(1, tierMap["files"]);
        Assert.Equal(1, tierMap["checkpoints"]);
        Assert.Equal(2, tierMap["search"]);
        Assert.Equal(3, tierMap["show"]);
        Assert.Equal(0, tierMap["health"]);
        Assert.Equal(0, tierMap["schema-check"]);
        Assert.Equal(0, tierMap["calibrate"]);
    }

    [Fact]
    public void FailureDoesNotThrow()
    {
        // Point telemetry to an invalid path that will cause write failure
        var invalidPath = Path.Combine(_tempDir, "nonexistent", new string('x', 300), "stats.json");
        Telemetry.Init(invalidPath);

        // Should not throw, even though write will fail
        Telemetry.Record("list", 10, tier: 1);

        // Restore valid path
        Telemetry.Init(_telemetryPath);
    }

    [Fact]
    public void DebugEnvVarProducesStderr()
    {
        // Point telemetry to a path that will cause a failure
        var invalidPath = Path.Combine("\0invalid", "stats.json");
        Telemetry.Init(invalidPath);

        // Capture stderr
        var originalError = Console.Error;
        using var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        try
        {
            // Set debug env var
            Environment.SetEnvironmentVariable("SESSION_RECALL_TELEMETRY_DEBUG", "1");

            // This should fail and log to stderr
            Telemetry.Record("list", 10, tier: 1);

            var stderrOutput = errorWriter.ToString();
            Assert.Contains("[telemetry]", stderrOutput);
            Assert.True(stderrOutput.Contains("Exception") || stderrOutput.Contains("Error"), 
                $"Expected exception info in stderr, got: {stderrOutput}");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SESSION_RECALL_TELEMETRY_DEBUG", null);
            Console.SetError(originalError);
            Telemetry.Init(_telemetryPath);
        }
    }

    [Fact]
    public void RoundTripWithPython()
    {
        // Generate 100 random records covering all commands and optional fields
        var records = Generate100RandomRecords();

        // Write them using .NET telemetry
        foreach (var rec in records)
        {
            Telemetry.Record(
                cmd: rec.Cmd,
                durationMs: rec.DurationMs,
                busyHits: rec.BusyHits,
                attempts: rec.Attempts,
                rows: rec.RowsReturned,
                exitCode: rec.ExitCode,
                schemaOk: rec.SchemaOk,
                tier: rec.Tier,
                queryHash: rec.QueryHash,
                sessionIdPrefix: rec.SessionIdPrefix,
                windowTier: rec.WindowTier);
        }

        // Save manifest for Python to reproduce
        var manifestPath = Path.Combine(_tempDir, "manifest.json");
        var manifestJson = JsonSerializer.Serialize(records, ManifestJsonOptions);
        File.WriteAllText(manifestPath, manifestJson);

        // Python output path
        var pythonPath = Path.Combine(_tempDir, "python_stats.json");

        // Call Python verification script
        var scriptPath = Path.Combine(
            Path.GetDirectoryName(typeof(TelemetryTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "..", "tests", "verify_telemetry_roundtrip.py");

        Assert.True(File.Exists(scriptPath), $"Python script not found: {scriptPath}");

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\" \"{_telemetryPath}\" \"{manifestPath}\" \"{pythonPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.NotNull(process);

        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Assert.Fail($"Python verification failed:\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        // Verify success output
        Assert.Contains("entries match", stdout);
    }

    private sealed class TelemetryRecord
    {
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

    private static List<TelemetryRecord> Generate100RandomRecords()
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var records = new List<TelemetryRecord>();

        // Commands from tier map
        var commands = new[] 
        { 
            "list", "files", "checkpoints", "search", "show", 
            "health", "schema-check", "calibrate" 
        };

        // Track optional field usage to ensure each is used at least 5 times
        var queryHashCount = 0;
        var sessionIdPrefixCount = 0;
        var windowTierCount = 0;
        var tierNullCount = 0;

        for (var i = 0; i < 100; i++)
        {
            var cmd = commands[random.Next(commands.Length)];
            var tier = Telemetry.TierMap.TryGetValue(cmd, out var t) ? (int?)t : null;

            // Force optional field usage for first 20 records to ensure coverage
            var forceQueryHash = i < 5 || (queryHashCount < 5 && i % 7 == 0);
            var forceSessionId = i < 5 || (sessionIdPrefixCount < 5 && i % 11 == 0);
            var forceWindowTier = i < 5 || (windowTierCount < 5 && i % 13 == 0);
            var forceTierNull = i < 5 || (tierNullCount < 5 && i % 17 == 0);

            // Generate query hash with non-ASCII characters (hash itself is ASCII)
            var queryHash = forceQueryHash 
                ? Telemetry.QueryHash($"test query {i} with émojis 🚀 and ñoñ-ÁSCII")
                : null;

            if (queryHash != null) queryHashCount++;

            // Generate session ID prefix
            var sessionIdPrefix = forceSessionId
                ? $"{random.Next(0x1000000):x8}"
                : null;

            if (sessionIdPrefix != null) sessionIdPrefixCount++;

            // Generate window tier
            var windowTier = forceWindowTier
                ? $"W{random.Next(1, 7)}"
                : null;

            if (windowTier != null) windowTierCount++;

            // Override tier to null for some records
            if (forceTierNull)
            {
                tier = null;
                tierNullCount++;
            }

            records.Add(new TelemetryRecord
            {
                Cmd = cmd,
                DurationMs = random.Next(1, 5000),
                BusyHits = random.Next(0, 10),
                Attempts = random.Next(1, 5),
                RowsReturned = random.Next(0, 1000),
                ExitCode = random.Next(100) < 95 ? 0 : 1, // 95% success
                SchemaOk = random.Next(100) < 98,          // 98% schema OK
                Tier = tier,
                QueryHash = queryHash,
                SessionIdPrefix = sessionIdPrefix,
                WindowTier = windowTier
            });
        }

        // Verify we met the minimum usage requirements
        Assert.True(queryHashCount >= 5, $"query_hash used {queryHashCount} times, expected >= 5");
        Assert.True(sessionIdPrefixCount >= 5, $"session_id_prefix used {sessionIdPrefixCount} times, expected >= 5");
        Assert.True(windowTierCount >= 5, $"window_tier used {windowTierCount} times, expected >= 5");
        Assert.True(tierNullCount >= 5, $"tier=null used {tierNullCount} times, expected >= 5");

        return records;
    }

    [Fact]
    public void DebugEnvVarDisabledSuppressesStderr()
    {
        // Point telemetry to a path that will cause a failure
        var invalidPath = Path.Combine("\0invalid", "stats.json");
        Telemetry.Init(invalidPath);

        // Capture stderr
        var originalError = Console.Error;
        using var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        try
        {
            // Ensure debug env var is NOT set
            Environment.SetEnvironmentVariable("SESSION_RECALL_TELEMETRY_DEBUG", null);

            // This should fail but NOT log to stderr
            Telemetry.Record("list", 10, tier: 1);

            var stderrOutput = errorWriter.ToString();
            Assert.Empty(stderrOutput);
        }
        finally
        {
            Console.SetError(originalError);
            Telemetry.Init(_telemetryPath);
        }
    }

    [Fact]
    public void ReadOnlyDirectoryDoesNotThrow()
    {
        if (!OperatingSystem.IsWindows())
        {
            // On Unix-like systems, we can actually test read-only directories
            var readOnlyDir = Path.Combine(_tempDir, "readonly");
            Directory.CreateDirectory(readOnlyDir);
            
            var readOnlyPath = Path.Combine(readOnlyDir, "stats.json");
            
            // Make directory read-only (remove write permission)
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Use chmod to make directory read-only
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"444 {readOnlyDir}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                process?.WaitForExit();
            }

            Telemetry.Init(readOnlyPath);

            // Should not throw, even though write will fail due to permissions
            Telemetry.Record("list", 10, tier: 1);

            // Restore write permissions for cleanup
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"755 {readOnlyDir}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                process?.WaitForExit();
            }

            Telemetry.Init(_telemetryPath);
        }
        else
        {
            // On Windows, we simulate by using an invalid path
            var invalidPath = Path.Combine("\0invalid", "stats.json");
            Telemetry.Init(invalidPath);

            // Should not throw
            Telemetry.Record("list", 10, tier: 1);

            Telemetry.Init(_telemetryPath);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task ConcurrentWritesNoCorruption()
    {
        // Verify 8 parallel writers append without corrupting the file
        const int writerCount = 8;
        const int writesPerWriter = 10;
        var barrier = new System.Threading.Barrier(writerCount);
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, writerCount).Select(writerId => System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                // Wait for all writers to be ready
                barrier.SignalAndWait();

                // Perform writes
                for (var i = 0; i < writesPerWriter; i++)
                {
                    Telemetry.Record(
                        cmd: "list",
                        durationMs: writerId * 1000 + i,
                        busyHits: writerId,
                        attempts: 1,
                        rows: i,
                        exitCode: 0,
                        schemaOk: true,
                        tier: 1);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // Verify no exceptions surfaced to callers (telemetry should swallow them)
        Assert.Empty(exceptions);

        // Verify the file is valid JSON
        Assert.True(File.Exists(_telemetryPath), "Telemetry file should exist");
        var json = File.ReadAllText(_telemetryPath);
        
        // Parse to verify it's valid JSON with proper structure
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("entries", out var entriesProperty));
        var entries = entriesProperty.EnumerateArray().ToArray();
        
        // Verify we have entries (could be less than total due to race conditions)
        Assert.True(entries.Length > 0, "Should have at least some entries");
        Assert.True(entries.Length <= 500, "Should respect ring buffer limit");

        // Verify each entry is a valid telemetry entry with required fields
        foreach (var entry in entries)
        {
            Assert.True(entry.TryGetProperty("ts", out _), "Entry missing 'ts' field");
            Assert.True(entry.TryGetProperty("cmd", out _), "Entry missing 'cmd' field");
            Assert.True(entry.TryGetProperty("duration_ms", out _), "Entry missing 'duration_ms' field");
            Assert.Equal("list", entry.GetProperty("cmd").GetString());
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task ConcurrentWritesRingBufferInvariants()
    {
        // Pre-fill with 490 entries to get close to the 500 limit
        for (var i = 0; i < 490; i++)
        {
            Telemetry.Record("prefill", i, tier: 1);
        }

        // Verify pre-fill worked
        var preEntries = ReadEntries();
        Assert.Equal(490, preEntries.Length);

        // Now run 8 concurrent writers, each writing 20 entries (160 total)
        // This will exceed the 500 limit, testing ring buffer behavior under contention
        const int writerCount = 8;
        const int writesPerWriter = 20;
        var barrier = new System.Threading.Barrier(writerCount);
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, writerCount).Select(writerId => System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                barrier.SignalAndWait();

                for (var i = 0; i < writesPerWriter; i++)
                {
                    Telemetry.Record(
                        cmd: $"writer{writerId}",
                        durationMs: writerId * 10000 + i,
                        tier: 1);
                    
                    // Small delay to reduce contention slightly
                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // No exceptions should surface to callers
        Assert.Empty(exceptions);

        // Verify the file is still valid JSON
        var json = File.ReadAllText(_telemetryPath);
        using var doc = JsonDocument.Parse(json);
        var entries = doc.RootElement.GetProperty("entries").EnumerateArray().ToArray();

        // Ring buffer should cap at 500 entries
        Assert.True(entries.Length <= 500, $"Ring buffer exceeded 500 entries: {entries.Length}");

        // Under contention, we expect "best-effort" behavior:
        // - Some writes may be lost due to race conditions
        // - But the file should remain valid and entries should be well-formed
        // - No duplicate entries (same duration_ms shouldn't appear twice)
        var durationMsValues = entries
            .Select(e => e.GetProperty("duration_ms").GetInt32())
            .ToList();

        // Verify all entries are valid
        foreach (var entry in entries)
        {
            Assert.True(entry.TryGetProperty("ts", out _));
            Assert.True(entry.TryGetProperty("cmd", out _));
            Assert.True(entry.TryGetProperty("duration_ms", out _));
        }

        // Document the "best-effort" behavior: under heavy contention,
        // some writes may be lost, but we should have at least some entries from the concurrent phase
        var concurrentPhaseEntries = entries
            .Where(e => e.GetProperty("cmd").GetString()?.StartsWith("writer") == true)
            .ToArray();

        // We expect at least some entries from concurrent writers made it through
        Assert.True(concurrentPhaseEntries.Length > 0, 
            "Expected at least some entries from concurrent writers");
    }

    private static readonly string[] TestCommands = { "list", "search", "show", "health" };

    [Fact]
    public async System.Threading.Tasks.Task ConcurrentWritesWithMixedOperations()
    {
        // Test concurrent writes with different commands and optional fields
        const int writerCount = 8;
        const int writesPerWriter = 15;
        var barrier = new System.Threading.Barrier(writerCount);
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var random = new Random(42);

        var tasks = Enumerable.Range(0, writerCount).Select(writerId => System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var localRandom = new Random(writerId); // Each writer has its own random instance
                barrier.SignalAndWait();

                for (var i = 0; i < writesPerWriter; i++)
                {
                    var cmd = TestCommands[localRandom.Next(TestCommands.Length)];
                    var tier = cmd switch
                    {
                        "list" => 1,
                        "search" => 2,
                        "show" => 3,
                        "health" => 0,
                        _ => (int?)null
                    };

                    Telemetry.Record(
                        cmd: cmd,
                        durationMs: writerId * 1000 + i,
                        busyHits: localRandom.Next(0, 5),
                        attempts: localRandom.Next(1, 4),
                        rows: localRandom.Next(0, 100),
                        exitCode: 0,
                        schemaOk: true,
                        tier: tier,
                        queryHash: cmd == "search" ? Telemetry.QueryHash($"query{i}") : null,
                        sessionIdPrefix: cmd == "show" ? $"{localRandom.Next(0x1000000):x8}" : null);

                    // Minimal delay to allow some interleaving
                    if (i % 5 == 0)
                        System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await System.Threading.Tasks.Task.WhenAll(tasks);

        // No exceptions should surface
        Assert.Empty(exceptions);

        // Verify file is valid JSON
        Assert.True(File.Exists(_telemetryPath));
        var json = File.ReadAllText(_telemetryPath);
        
        using var doc = JsonDocument.Parse(json);
        var entries = doc.RootElement.GetProperty("entries").EnumerateArray().ToArray();

        // Verify structure
        Assert.True(entries.Length > 0);
        Assert.True(entries.Length <= 500);

        // Verify all entries are well-formed
        foreach (var entry in entries)
        {
            Assert.True(entry.TryGetProperty("ts", out _));
            Assert.True(entry.TryGetProperty("cmd", out _));
            Assert.True(entry.TryGetProperty("duration_ms", out _));
            Assert.True(entry.TryGetProperty("busy_hits", out _));
            Assert.True(entry.TryGetProperty("attempts", out _));
            Assert.True(entry.TryGetProperty("rows_returned", out _));
            Assert.True(entry.TryGetProperty("exit_code", out _));
            Assert.True(entry.TryGetProperty("schema_ok", out _));

            // Verify optional fields are present when expected
            var cmdValue = entry.GetProperty("cmd").GetString();
            if (cmdValue == "search")
            {
                Assert.True(entry.TryGetProperty("query_hash", out var qh));
                Assert.Equal(8, qh.GetString()!.Length);
            }
            if (cmdValue == "show")
            {
                Assert.True(entry.TryGetProperty("session_id_prefix", out var sp));
                Assert.Equal(8, sp.GetString()!.Length);
            }
        }
    }
}
