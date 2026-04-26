using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AutoMemory.Core;
using AutoMemory.Core.Health;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimDisclosureTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void Dispose()
    {
        // Clean up temp files
        foreach (var path in _tempFiles)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { /* ignore */ }
            }
        }
    }

    private string WriteEntries(List<Dictionary<string, object>> entries)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);

        var wrapper = new { entries };
        var json = JsonSerializer.Serialize(wrapper, JsonOptions);
        File.WriteAllText(path, json);

        return path;
    }

    private static void SetTelemetryPath(string path)
    {
        // No longer needed - removed
    }

    private static string Ts(int offsetMin = 0)
    {
        return DateTime.UtcNow.AddMinutes(-offsetMin).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    [Fact]
    public void All_legacy_entries_returns_calibrating()
    {
        // Arrange: 50 entries without tier field
        var entries = Enumerable.Range(0, 50)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["ts"] = Ts(i)
            })
            .ToList();
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimDisclosure();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Null(result.Score);
        Assert.Equal("CALIBRATING", result.Detail["zone"]);
        Assert.Equal(50, result.Detail["unknown_entries"]);
    }

    [Fact]
    public void Meta_entries_excluded()
    {
        // Arrange: 10 meta entries (tier=0) + 5 scored entries (tier=1)
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 10; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "health",
                ["tier"] = 0,
                ["ts"] = Ts(i)
            });
        }
        for (var i = 10; i < 15; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["tier"] = 1,
                ["ts"] = Ts(i)
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimDisclosure();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Equal(10, result.Detail["meta_entries"]);
        Assert.Equal(5, result.Detail["scored_entries"]);
    }

    [Fact]
    public void Insufficient_sample_returns_calibrating()
    {
        // Arrange: only 5 tier=1 entries
        var entries = Enumerable.Range(0, 5)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["tier"] = 1,
                ["ts"] = Ts(i)
            })
            .ToList();
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimDisclosure();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Null(result.Score);
        Assert.Equal("CALIBRATING", result.Detail["zone"]);
        Assert.Contains("Collecting baseline", (string)result.Detail["hint"]!);
    }

    [Fact]
    public void Unknown_share_over_50pct_forces_calibrating()
    {
        // Arrange: 300 unknown + 250 tier=1 entries
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 300; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["ts"] = Ts(i)
            });
        }
        for (var i = 300; i < 550; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["tier"] = 1,
                ["ts"] = Ts(i)
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimDisclosure();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Null(result.Score);
        Assert.Equal("CALIBRATING", result.Detail["zone"]);
        Assert.Contains("drain", (string)result.Detail["hint"]!);
    }

    [Fact]
    public void Scoring_active_gate()
    {
        // Arrange: 250 tier=1 entries (enough sample)
        var entries = Enumerable.Range(0, 250)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["tier"] = 1,
                ["ts"] = Ts(i)
            })
            .ToList();
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimDisclosure();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert: Even with 200+ entries, if SCORING_ACTIVE=False, zone is CALIBRATING
        Assert.Null(result.Score);
        Assert.Equal("CALIBRATING", result.Detail["zone"]);
    }

    [Fact]
    public void Healthy_escalation_t1_to_t2()
    {
        // Arrange: T1 → T2 escalation
        var entries = new List<TelemetryJsonEntry>
        {
            new() { Cmd = "list", Tier = 1, Ts = Ts(10) },
            new() { Cmd = "search", Tier = 2, Ts = Ts(9) }
        };

        // Act
        var t = DimDisclosure.ClassifyTransitions(entries);

        // Assert
        Assert.Equal(1, t.Healthy);
        Assert.Equal(0, t.Suspicious);
    }

    [Fact]
    public void Cold_start_t3_is_suspicious()
    {
        // Arrange: single T3 entry with no preceding T1/T2
        var entries = new List<TelemetryJsonEntry>
        {
            new() { Cmd = "show", Tier = 3, Ts = Ts(5) }
        };

        // Act
        var t = DimDisclosure.ClassifyTransitions(entries);

        // Assert
        Assert.True(t.Suspicious >= 1);
    }

    [Fact]
    public void T3_to_t3_within_window_is_neutral()
    {
        // Arrange: T1 → T3 → T3 (within 5 min)
        var entries = new List<TelemetryJsonEntry>
        {
            new() { Cmd = "list", Tier = 1, Ts = Ts(10) },
            new() { Cmd = "show", Tier = 3, Ts = Ts(9) },
            new() { Cmd = "show", Tier = 3, Ts = Ts(8) }
        };

        // Act
        var t = DimDisclosure.ClassifyTransitions(entries);

        // Assert
        Assert.Equal(1, t.Healthy);
        Assert.Equal(1, t.Neutral);
    }

    [Fact]
    public void Repeated_search_with_same_hash_counts_repetition()
    {
        // Arrange: two T2 with same query_hash
        var entries = new List<TelemetryJsonEntry>
        {
            new() { Cmd = "search", Tier = 2, QueryHash = "abc12345", Ts = Ts(5) },
            new() { Cmd = "search", Tier = 2, QueryHash = "abc12345", Ts = Ts(4) }
        };

        // Act
        var t = DimDisclosure.ClassifyTransitions(entries);

        // Assert
        Assert.Equal(1, t.Repetition);
    }
}
