using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AutoMemory.Tests.Health;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class DimConcurrencyTests : IDisposable
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

    [Fact]
    public void No_telemetry_file_returns_amber()
    {
        // Arrange
        var telemetryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Equal(5.0, result.Score);
        Assert.Equal("AMBER", result.Detail["zone"]);
        Assert.Equal("No telemetry data yet", result.Detail["detail"]);
        Assert.Equal("Run session-recall a few times first", result.Detail["hint"]);
    }

    [Fact]
    public void Empty_telemetry_returns_amber()
    {
        // Arrange
        var entries = new List<Dictionary<string, object>>();
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Equal(5.0, result.Score);
        Assert.Equal("AMBER", result.Detail["zone"]);
        Assert.Equal("Empty telemetry", result.Detail["detail"]);
    }

    [Fact]
    public void Zero_busy_hits_returns_green()
    {
        // Arrange: 10 entries with no busy hits
        var entries = Enumerable.Range(0, 10)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["ts"] = Ts(i),
                ["busy_hits"] = 0,
                ["attempts"] = 1,
                ["duration_ms"] = 100 + i * 10
            })
            .ToList();
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.NotNull(result.Score);
        Assert.Equal("GREEN", result.Detail["zone"]);
        Assert.True(result.Score >= 7.0);
        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("busy=0.0%", detail);
        Assert.Contains("avg_attempts=1.00", detail);
        Assert.Contains("n=10", detail);
    }

    [Fact]
    public void Low_busy_rate_returns_green()
    {
        // Arrange: 100 entries, 3 busy hits total (3% rate < 5% threshold)
        var entries = Enumerable.Range(0, 100)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 3 ? 1 : 0,
                ["attempts"] = i < 3 ? 2 : 1,
                ["duration_ms"] = 50 + i
            })
            .ToList();
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

    [Fact]
    public void Medium_busy_rate_returns_amber()
    {
        // Arrange: 100 entries, 15 busy hits total (15% rate: between 5% and 20%)
        var entries = Enumerable.Range(0, 100)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 15 ? 1 : 0,
                ["attempts"] = i < 15 ? 2 : 1,
                ["duration_ms"] = 80 + i * 2
            })
            .ToList();
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
        Assert.Contains("busy=15.0%", detail);
    }

    [Fact]
    public void High_busy_rate_returns_red()
    {
        // Arrange: 100 entries, 50 busy hits total (50% rate > 20% threshold)
        var entries = Enumerable.Range(0, 100)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "search",
                ["ts"] = Ts(i),
                ["busy_hits"] = i < 50 ? 1 : 0,
                ["attempts"] = i < 50 ? 3 : 1,
                ["duration_ms"] = 200 + i * 5
            })
            .ToList();
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
        Assert.Contains("busy=50.0%", detail);
        Assert.Contains("avg_attempts=", detail);
    }

    [Fact]
    public void P95_calculated_correctly()
    {
        // Arrange: 100 entries with known duration distribution
        var entries = Enumerable.Range(0, 100)
            .Select(i => new Dictionary<string, object>
            {
                ["cmd"] = "list",
                ["ts"] = Ts(i),
                ["busy_hits"] = 0,
                ["attempts"] = 1,
                ["duration_ms"] = i // 0-99ms
            })
            .ToList();
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        var detail = (string)result.Detail["detail"]!;
        // P95 of 0-99 is at index 95, which should be 95ms
        Assert.Contains("p95=95ms", detail);
    }

    [Fact]
    public void Avg_attempts_calculated_correctly()
    {
        // Arrange: 10 entries with varying attempts
        var entries = new List<Dictionary<string, object>>
        {
            new() { ["cmd"] = "search", ["ts"] = Ts(0), ["busy_hits"] = 0, ["attempts"] = 1, ["duration_ms"] = 100 },
            new() { ["cmd"] = "search", ["ts"] = Ts(1), ["busy_hits"] = 1, ["attempts"] = 2, ["duration_ms"] = 150 },
            new() { ["cmd"] = "search", ["ts"] = Ts(2), ["busy_hits"] = 2, ["attempts"] = 3, ["duration_ms"] = 200 },
            new() { ["cmd"] = "search", ["ts"] = Ts(3), ["busy_hits"] = 0, ["attempts"] = 1, ["duration_ms"] = 100 },
            new() { ["cmd"] = "search", ["ts"] = Ts(4), ["busy_hits"] = 0, ["attempts"] = 1, ["duration_ms"] = 100 },
        };
        // avg_attempts = (1 + 2 + 3 + 1 + 1) / 5 = 8 / 5 = 1.6
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("avg_attempts=1.60", detail);
    }

    [Fact]
    public void Hint_is_present()
    {
        // Arrange
        var entries = new List<Dictionary<string, object>>
        {
            new() { ["cmd"] = "list", ["ts"] = Ts(0), ["busy_hits"] = 0, ["attempts"] = 1, ["duration_ms"] = 100 }
        };
        var telemetryPath = WriteEntries(entries);

        // Act
        var dim = new DimConcurrency();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var result = dim.Run(conn, new HealthContext(DateTimeOffset.UtcNow, null), telemetryPath);

        // Assert
        Assert.Equal("Increase busy_timeout or reduce concurrent use", result.Detail["hint"]);
    }
}
