using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 7: Concurrency — SQLITE_BUSY rate from telemetry.
/// </summary>
public sealed class DimConcurrency : IHealthDimension
{
    private const string Hint = "Increase busy_timeout or reduce concurrent use";
    private const int GreenThreshold = 5;
    private const int AmberThreshold = 20;

    public string Name => "Concurrency";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        return Run(conn, ctx, null);
    }

    internal DimensionResult Run(SqliteConnection conn, HealthContext ctx, string? telemetryPathOverride)
    {
        var path = telemetryPathOverride ?? Config.TelemetryPath;

        if (!File.Exists(path))
        {
            return new DimensionResult(
                Name,
                5.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "AMBER",
                    ["detail"] = "No telemetry data yet",
                    ["hint"] = "Run session-recall a few times first"
                }
            );
        }

        var entries = LoadEntries(telemetryPathOverride);

        if (entries.Count == 0)
        {
            return new DimensionResult(
                Name,
                5.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "AMBER",
                    ["detail"] = "Empty telemetry",
                    ["hint"] = "Run session-recall a few times first"
                }
            );
        }

        try
        {
            var totalBusy = entries.Sum(e => e.BusyHits);
            var busyRate = (totalBusy / (double)entries.Count) * 100.0;
            var avgAttempts = entries.Average(e => e.Attempts);
            var durations = entries.Select(e => e.DurationMs).OrderBy(d => d).ToList();
            var p95Idx = (int)(durations.Count * 0.95);
            var p95 = durations[Math.Min(p95Idx, durations.Count - 1)];

            var (zone, score) = ScoreDim(busyRate, GreenThreshold, AmberThreshold, higherIsBetter: false);

            var detail = string.Format(CultureInfo.InvariantCulture,
                "busy={0:F1}%, avg_attempts={1:F2}, p95={2}ms, n={3}",
                busyRate, avgAttempts, p95, entries.Count);

            return new DimensionResult(
                Name,
                score,
                new Dictionary<string, object?>
                {
                    ["zone"] = zone,
                    ["detail"] = detail,
                    ["hint"] = Hint
                }
            );
        }
        catch (Exception ex)
        {
            return new DimensionResult(
                Name,
                5.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "AMBER",
                    ["detail"] = $"Telemetry file unreadable: {ex.Message}",
                    ["hint"] = "Delete and let it regenerate"
                }
            );
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TelemetryJsonWrapper uses source-generated JSON serialization.")]
    private static List<TelemetryJsonEntry> LoadEntries(string? telemetryPathOverride = null)
    {
        try
        {
            var path = telemetryPathOverride ?? Config.TelemetryPath;
            if (!File.Exists(path))
                return new List<TelemetryJsonEntry>();

            var json = File.ReadAllText(path);
            var wrapper = JsonSerializer.Deserialize(json, AutoMemoryJsonContext.Default.TelemetryJsonWrapper);
            return wrapper?.Entries ?? new List<TelemetryJsonEntry>();
        }
        catch
        {
            return new List<TelemetryJsonEntry>();
        }
    }

    private static (string Zone, double Score) ScoreDim(double value, double greenThreshold, double amberThreshold, bool higherIsBetter)
    {
        string zone;
        double score;

        if (higherIsBetter)
        {
            if (value >= greenThreshold)
            {
                zone = "GREEN";
                score = Math.Min(10.0, 7.0 + 3.0 * (value - greenThreshold) / Math.Max(greenThreshold, 1.0));
            }
            else if (value >= amberThreshold)
            {
                zone = "AMBER";
                score = 4.0 + 3.0 * (value - amberThreshold) / Math.Max(greenThreshold - amberThreshold, 1.0);
            }
            else
            {
                zone = "RED";
                score = Math.Max(0.0, 3.0 * value / Math.Max(amberThreshold, 1.0));
            }
        }
        else
        {
            if (value <= greenThreshold)
            {
                zone = "GREEN";
                score = Math.Min(10.0, 7.0 + 3.0 * (greenThreshold - value) / Math.Max(greenThreshold, 1.0));
            }
            else if (value <= amberThreshold)
            {
                zone = "AMBER";
                score = 4.0 + 3.0 * (amberThreshold - value) / Math.Max(amberThreshold - greenThreshold, 1.0);
            }
            else
            {
                zone = "RED";
                score = Math.Max(0.0, 3.0 * amberThreshold / Math.Max(value, 1.0));
            }
        }

        // Round to 1 decimal place, matching Python's round(x, 1)
        score = Math.Round(Math.Min(10.0, Math.Max(0.0, score)), 1, MidpointRounding.ToEven);
        return (zone, score);
    }
}
