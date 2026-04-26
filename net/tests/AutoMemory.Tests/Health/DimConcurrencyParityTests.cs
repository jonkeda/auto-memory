using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AutoMemory.Core;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

/// <summary>
/// Parity test for DimConcurrency: verifies .NET output matches Python reference.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimConcurrencyParityTests : IDisposable
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

    private static string Ts(int offsetMin = 0)
    {
        return DateTime.UtcNow.AddMinutes(-offsetMin).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Fixture 1: Zero busy hits (healthy scenario).
    /// Expected: GREEN zone, score >= 7.0, busy=0.0%
    /// </summary>
    [Fact]
    public void Parity_zero_busy_hits()
    {
        // Arrange: 50 entries, no busy hits
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 50; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = 0,
                ["attempts"] = 1,
                ["duration_ms"] = 100 + i
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert - match Python output
        Assert.NotNull(result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);
        Assert.True(result.Score >= 7.0);

        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=0.0%", detail);
        Assert.Contains("avg_attempts=1.00", detail);
        Assert.Contains("n=50", detail);
    }

    /// <summary>
    /// Fixture 2: 3% busy rate (GREEN threshold = 5%).
    /// Expected: GREEN zone, busy=3.0%
    /// </summary>
    [Fact]
    public void Parity_low_busy_rate_green()
    {
        // Arrange: 100 entries, 3 busy hits = 3% rate
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 100; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 3 ? 1 : 0,
                ["attempts"] = i < 3 ? 2 : 1,
                ["duration_ms"] = 80 + i * 2
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.NotNull(result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);

        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=3.0%", detail);
    }

    /// <summary>
    /// Fixture 3: 10% busy rate (between 5% and 20%).
    /// Expected: AMBER zone, 4.0 <= score < 7.0
    /// </summary>
    [Fact]
    public void Parity_medium_busy_rate_amber()
    {
        // Arrange: 100 entries, 10 busy hits = 10% rate
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 100; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 10 ? 1 : 0,
                ["attempts"] = i < 10 ? 2 : 1,
                ["duration_ms"] = 100 + i * 3
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.NotNull(result.Score);
        Assert.Equal("AMBER", result.Detail["zone"]);
        Assert.True(result.Score >= 4.0 && result.Score < 7.0);

        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=10.0%", detail);
    }

    /// <summary>
    /// Fixture 4: 25% busy rate (above 20% threshold).
    /// Expected: RED zone, score < 4.0
    /// </summary>
    [Fact]
    public void Parity_high_busy_rate_red()
    {
        // Arrange: 100 entries, 25 busy hits = 25% rate
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 100; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 25 ? 1 : 0,
                ["attempts"] = i < 25 ? 3 : 1,
                ["duration_ms"] = 150 + i * 4
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.NotNull(result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        Assert.True(result.Score < 4.0);

        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=25.0%", detail);
    }

    /// <summary>
    /// Fixture 5: Boundary case at green threshold (5%).
    /// Expected: GREEN zone
    /// </summary>
    [Fact]
    public void Parity_boundary_green_threshold()
    {
        // Arrange: 100 entries, 5 busy hits = exactly 5% rate
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 100; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 5 ? 1 : 0,
                ["attempts"] = i < 5 ? 2 : 1,
                ["duration_ms"] = 90 + i
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.NotNull(result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);

        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=5.0%", detail);
    }

    /// <summary>
    /// Fixture 6: Boundary case at amber threshold (20%).
    /// Expected: AMBER zone
    /// </summary>
    [Fact]
    public void Parity_boundary_amber_threshold()
    {
        // Arrange: 100 entries, 20 busy hits = exactly 20% rate
        var entries = new List<Dictionary<string, object>>();
        for (var i = 0; i < 100; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 20 ? 1 : 0,
                ["attempts"] = i < 20 ? 2 : 1,
                ["duration_ms"] = 110 + i * 2
            });
        }
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.NotNull(result.Score);
        Assert.Equal("AMBER", result.Detail["zone"]);

        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=20.0%", detail);
    }
}
