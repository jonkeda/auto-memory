using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Health;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Health command — run all 9 health dimensions and report.
/// </summary>
public static class HealthCommand
{
    private static readonly Dictionary<string, string> ZoneIcons = new()
    {
        ["GREEN"] = "🟢",
        ["AMBER"] = "🟡",
        ["RED"] = "🔴",
        ["CALIBRATING"] = "⚪"
    };

    public static int Run(ParsedArgs args)
    {
        try
        {
            using var conn = Connect.ConnectReadOnly(Config.DbPath);
            return RunCore(args, conn);
        }        catch (DatabaseNotFoundException)
        {
            throw; // Let Program.cs handle exit code 4
        }
        catch (DatabaseLockedException)
        {
            throw; // Let Program.cs handle exit code 3
        }        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to run health check: {ex.Message}");
            return 3;
        }
    }

    /// <summary>
    /// Execute the health command with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
        var jsonMode = args.GetFlag("json");

        // Create health context
        var ctx = new HealthContext(DateTimeOffset.UtcNow, Repo: null);

        // Instantiate all 9 dimensions in order matching Python
        IHealthDimension[] dimensions =
        [
            new DimFreshness(),
            new DimSchema(),
            new DimLatency(),
            new DimCorpus(),
            new DimSummaryCoverage(),
            new DimRepoCoverage(),
            new DimConcurrency(),
            new DimE2E(),
            new DimDisclosure()
        ];

        // Run all dimensions
        var results = dimensions.Select(d => d.Run(conn, ctx)).ToList();

        // Compute overall score
        var overallScore = Scoring.OverallScore(results);

        // Extract hints from non-GREEN dimensions
        var hintsNullable = results
            .Where(r => r.Detail.TryGetValue("zone", out var zone) && 
                        zone is string zoneStr && 
                        zoneStr != "GREEN" &&
                        r.Detail.TryGetValue("hint", out var hint) && 
                        hint is string hintStr && 
                        !string.IsNullOrWhiteSpace(hintStr))
            .Select(r => r.Detail["hint"] as string)
            .Take(3)
            .ToList();
        
        // Convert to non-nullable list for HealthResult
        var hints = hintsNullable.Where(h => h != null).Select(h => h!).ToList();

        if (jsonMode)
        {
            // Dimensions that use score_dim helper return float scores (with .0 for whole numbers)
            // Dimensions that directly construct their result return integer scores
            var scoreFirstDimensions = new HashSet<string>
            {
                "DB Freshness",
                "Query Latency",
                "Corpus Size",
                "Summary Coverage",
                "Concurrency"
            };

            // Convert results to dictionaries matching Python format with correct field order
            var dimResults = results.Select(r =>
            {
                var dict = new Dictionary<string, object?>();
                var zone = r.Detail.TryGetValue("zone", out var z) && z is string zoneStr ? zoneStr : "UNKNOWN";
                var detail = r.Detail.TryGetValue("detail", out var d) && d is string detailStr ? detailStr : "";
                var hint = r.Detail.TryGetValue("hint", out var h) && h is string hintStr ? hintStr : "";
                
                // Match Python's score type: float for score_dim dimensions, int for others
                object? scoreValue;
                if (r.Score.HasValue)
                {
                    if (scoreFirstDimensions.Contains(r.Name))
                    {
                        // Float score - always include decimal point (match Python's score_dim output)
                        scoreValue = decimal.Parse(r.Score.Value.ToString("F1", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // Integer score for dimensions like Schema Integrity, E2E, Repo Coverage
                        scoreValue = (int)Math.Round(r.Score.Value);
                    }
                }
                else
                {
                    scoreValue = null;
                }
                
                // Special handling for Progressive Disclosure which has extra fields
                if (r.Name == "Progressive Disclosure")
                {
                    // Order: name, unknown_entries, meta_entries, scored_entries, score, zone, detail, hint
                    dict["name"] = r.Name;
                    if (r.Detail.TryGetValue("unknown_entries", out var ue)) dict["unknown_entries"] = ue;
                    if (r.Detail.TryGetValue("meta_entries", out var me)) dict["meta_entries"] = me;
                    if (r.Detail.TryGetValue("scored_entries", out var se)) dict["scored_entries"] = se;
                    dict["score"] = scoreValue;
                    dict["zone"] = zone;
                    dict["detail"] = detail;
                    dict["hint"] = hint;
                }
                else if (scoreFirstDimensions.Contains(r.Name))
                {
                    // Order: score, zone, name, detail, hint (for dimensions using score_dim)
                    dict["score"] = scoreValue;
                    dict["zone"] = zone;
                    dict["name"] = r.Name;
                    dict["detail"] = detail;
                    dict["hint"] = hint;
                }
                else
                {
                    // Order: name, score, zone, detail, hint (for dimensions not using score_dim)
                    dict["name"] = r.Name;
                    dict["score"] = scoreValue;
                    dict["zone"] = zone;
                    dict["detail"] = detail;
                    dict["hint"] = hint;
                }
                
                return dict;
            }).ToList();

            var output = new HealthResult
            {
                OverallScore = Math.Round(overallScore, 1),
                Dims = dimResults,
                TopHints = hints.ToList()
            };

            Console.WriteLine(FormatOutput.FmtJson(output));
        }
        else
        {
            // Text output
            Console.WriteLine();
            Console.WriteLine($"{"Dim",-3} {"Name",-22} {"Zone",-8} {"Score",5}  Detail");
            Console.WriteLine(new string('-', 70));

            for (var i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var zone = r.Detail.TryGetValue("zone", out var z) && z is string zoneStr ? zoneStr : "?";
                var icon = ZoneIcons.TryGetValue(zone, out var ic) ? ic : "?";
                var scoreStr = r.Score.HasValue ? $"{r.Score.Value,5:F1}" : "  -  ";
                var detail = r.Detail.TryGetValue("detail", out var d) && d is string detailStr ? detailStr : "";

                Console.WriteLine($" {i + 1,-2} {r.Name,-22} {icon} {zone,-5} {scoreStr}  {detail}");
            }

            Console.WriteLine(new string('-', 70));
            Console.WriteLine($"    {"Overall",-22}        {overallScore,5:F1}");

            if (hints.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("💡 Hints:");
                foreach (var hint in hints)
                {
                    Console.WriteLine($"   • {hint}");
                }
            }

            Console.WriteLine();
        }

        return 0;
    }
}
