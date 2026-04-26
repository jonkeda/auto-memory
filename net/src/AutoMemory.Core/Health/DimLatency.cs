using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 3: Query Latency — time list + show queries.
/// </summary>
public sealed class DimLatency : IHealthDimension
{
    private const string Hint = "Check DB size or run PRAGMA integrity_check";
    private const int GreenThreshold = 200;
    private const int AmberThreshold = 500;

    public string Name => "Query Latency";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        return Run(conn, ctx, null);
    }

    internal DimensionResult Run(SqliteConnection conn, HealthContext ctx, string? dbPathOverride)
    {
        try
        {
            var dbPath = dbPathOverride ?? Config.DbPath;
            
            // Open a fresh read-only connection to measure cold query performance
            // Use ConnectReadOnlyCore to avoid Environment.Exit in health checks
            using var testConn = Db.Connect.ConnectReadOnlyCore(dbPath);
            
            var sw = Stopwatch.StartNew();
            
            // Query 1: Get recent sessions with summaries
            using (var cmd = testConn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, summary FROM sessions ORDER BY created_at DESC LIMIT 10";
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) { /* consume results */ }
            }
            
            // Query 2: Get first session ID
            string? sessionId = null;
            using (var cmd = testConn.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM sessions LIMIT 1";
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    sessionId = reader.GetString(0);
                }
            }
            
            // Query 3: If we have a session, get some turns
            if (sessionId != null)
            {
                using var cmd = testConn.CreateCommand();
                cmd.CommandText = "SELECT turn_index FROM turns WHERE session_id = @sessionId LIMIT 5";
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) { /* consume results */ }
            }
            
            sw.Stop();
            var elapsedMs = sw.Elapsed.TotalMilliseconds;
            
            var (zone, score) = ScoreDim(elapsedMs, GreenThreshold, AmberThreshold, higherIsBetter: false);
            
            var detail = string.Format(CultureInfo.InvariantCulture, "{0:F0}ms", elapsedMs);
            
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
                0.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "RED",
                    ["detail"] = ex.Message,
                    ["hint"] = Hint
                }
            );
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
