using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 5: Summary Coverage — percentage of sessions with a summary.
/// </summary>
public sealed class DimSummaryCoverage : IHealthDimension
{
    private const string Hint = "Summaries fill post-session; ghost sessions (0 turns) are excluded";
    private const double GreenThreshold = 80.0;
    private const double AmberThreshold = 40.0;

    public string Name => "Summary Coverage";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        try
        {
            // Count total sessions with at least 1 turn
            var totalCmd = conn.CreateCommand();
            totalCmd.CommandText =
                "SELECT COUNT(*) FROM sessions s " +
                "WHERE EXISTS (SELECT 1 FROM turns t WHERE t.session_id = s.id)";
            var total = (long)(totalCmd.ExecuteScalar() ?? 0L);

            // Count sessions with non-empty summary AND at least 1 turn
            var withSummaryCmd = conn.CreateCommand();
            withSummaryCmd.CommandText =
                "SELECT COUNT(*) FROM sessions s " +
                "WHERE s.summary IS NOT NULL AND s.summary != '' " +
                "AND EXISTS (SELECT 1 FROM turns t WHERE t.session_id = s.id)";
            var withSummary = (long)(withSummaryCmd.ExecuteScalar() ?? 0L);

            // Count ghost sessions (0 turns)
            var ghostsCmd = conn.CreateCommand();
            ghostsCmd.CommandText =
                "SELECT COUNT(*) FROM sessions s " +
                "WHERE NOT EXISTS (SELECT 1 FROM turns t WHERE t.session_id = s.id)";
            var ghosts = (long)(ghostsCmd.ExecuteScalar() ?? 0L);

            // Calculate percentage
            var pct = total > 0 ? (double)withSummary / total * 100.0 : 0.0;

            // Score the dimension
            var (zone, score) = ScoreDim(pct, GreenThreshold, AmberThreshold, higherIsBetter: true);

            // Format detail string
            var detail = string.Format(CultureInfo.InvariantCulture, "{0:F0}% ({1}/{2})", pct, withSummary, total);
            if (ghosts > 0)
            {
                detail += string.Format(CultureInfo.InvariantCulture, " — {0} ghost sessions excluded", ghosts);
            }

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
