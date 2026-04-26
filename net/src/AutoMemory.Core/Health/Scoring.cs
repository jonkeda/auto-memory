using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMemory.Core.Health;

/// <summary>
/// Health check scoring logic — GREEN/AMBER/RED zone classification.
/// </summary>
public static class Scoring
{
    /// <summary>
    /// Score a dimension value against thresholds.
    /// </summary>
    /// <param name="value">The measured value.</param>
    /// <param name="greenThreshold">Threshold for GREEN zone.</param>
    /// <param name="amberThreshold">Threshold for AMBER zone.</param>
    /// <param name="higherIsBetter">If true, higher values are better. If false, lower values are better.</param>
    /// <returns>A dictionary with "score" (0-10) and "zone" (GREEN/AMBER/RED).</returns>
    public static IReadOnlyDictionary<string, object> ScoreDim(
        double value,
        double greenThreshold,
        double amberThreshold,
        bool higherIsBetter = true)
    {
        string zone;
        double score;

        if (higherIsBetter)
        {
            if (value >= greenThreshold)
            {
                zone = "GREEN";
                score = Math.Min(10, 7 + 3 * (value - greenThreshold) / Math.Max(greenThreshold, 1));
            }
            else if (value >= amberThreshold)
            {
                zone = "AMBER";
                score = 4 + 3 * (value - amberThreshold) / Math.Max(greenThreshold - amberThreshold, 1);
            }
            else
            {
                zone = "RED";
                score = Math.Max(0, 3 * value / Math.Max(amberThreshold, 1));
            }
        }
        else
        {
            if (value <= greenThreshold)
            {
                zone = "GREEN";
                score = Math.Min(10, 7 + 3 * (greenThreshold - value) / Math.Max(greenThreshold, 1));
            }
            else if (value <= amberThreshold)
            {
                zone = "AMBER";
                score = 4 + 3 * (amberThreshold - value) / Math.Max(amberThreshold - greenThreshold, 1);
            }
            else
            {
                zone = "RED";
                score = Math.Max(0, 3 * amberThreshold / Math.Max(value, 1));
            }
        }

        // Clamp to [0, 10] and round to 1 decimal place to match Python
        score = Math.Round(Math.Min(10, Math.Max(0, score)), 1, MidpointRounding.ToEven);

        return new Dictionary<string, object>
        {
            ["score"] = score,
            ["zone"] = zone
        };
    }

    /// <summary>
    /// Compute overall health score from dimension results.
    /// Returns the minimum score across all dimensions (most severe wins).
    /// Dimensions with null scores are skipped.
    /// </summary>
    /// <param name="dimensions">List of dimension results.</param>
    /// <returns>Overall health score (0-10), or 0.0 if no dimensions have scores.</returns>
    public static double OverallScore(IEnumerable<DimensionResult> dimensions)
    {
        var scored = dimensions
            .Where(d => d.Score.HasValue)
            .Select(d => d.Score!.Value)
            .ToList();

        return scored.Count > 0 ? scored.Min() : 0.0;
    }
}
