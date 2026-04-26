using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 1: DB freshness — how recently was session-store.db modified.
/// </summary>
public sealed class DimFreshness : IHealthDimension
{
    private const string Hint = "Use Copilot CLI — DB only updates from active sessions";
    private const int GreenThreshold = 24;
    private const int AmberThreshold = 72;

    public string Name => "DB Freshness";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        return RunInternal(Config.DbPath);
    }

    /// <summary>
    /// Internal implementation that accepts a file path for testing.
    /// </summary>
    internal DimensionResult RunInternal(string dbPath)
    {
        try
        {
            if (!File.Exists(dbPath))
            {
                return new DimensionResult(
                    Name,
                    0.0,
                    new Dictionary<string, object?>
                    {
                        ["zone"] = "RED",
                        ["detail"] = "DB not found",
                        ["hint"] = Hint
                    }
                );
            }

            var mtime = File.GetLastWriteTimeUtc(dbPath);
            var ageHours = (DateTime.UtcNow - mtime).TotalHours;

            var (zone, score) = ScoreDim(ageHours, GreenThreshold, AmberThreshold, higherIsBetter: false);

            var detail = string.Format(CultureInfo.InvariantCulture, "{0:F1}h old", ageHours);

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
