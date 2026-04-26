using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 4: Corpus Size — number of sessions in the database.
/// </summary>
public sealed class DimCorpus : IHealthDimension
{
    private const string Hint = "Cold start — will improve with usage";
    private const int GreenThreshold = 50;
    private const int AmberThreshold = 10;

    public string Name => "Corpus Size";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sessions";
            var count = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);

            var (zone, score) = ScoreDim(count, GreenThreshold, AmberThreshold, higherIsBetter: true);

            return new DimensionResult(
                Name,
                score,
                new Dictionary<string, object?>
                {
                    ["zone"] = zone,
                    ["detail"] = $"{count} sessions",
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
