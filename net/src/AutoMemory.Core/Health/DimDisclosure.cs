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
/// Dimension 9: Progressive Disclosure — analyzes tier usage patterns from telemetry.
/// Stage 1 behavior: returns score=null, zone="CALIBRATING".
/// Stage 2 (after operator runs calibrate --analyze and hand-edits thresholds):
///   operator fills in concrete GREEN/AMBER/RED thresholds and flips ScoringActive=true.
/// </summary>
public sealed class DimDisclosure : IHealthDimension
{
    public string Name => "Progressive Disclosure";
    public double Weight => 1.0 / 9.0;

    // Stage-2 gate. Operator flips this to True after hand-editing thresholds.
    private const bool ScoringActive = false;

    // Placeholders — operator replaces with observed μ±σ from `calibrate --analyze`.
    private const double GreenAvgLow = 1.15;
    private const double GreenAvgHigh = 2.63;    // μ ± 1σ band (example)
    private const double AmberAvgLow = 0.41;
    private const double AmberAvgHigh = 3.37;    // μ ± 2σ band (example)
    private const double T3PolicyFloor = 0.30;   // T3 > 30% is always suspicious

    private const int MinSampleSize = 200;
    private const int MinSampleDays = 7;
    private const double UnknownCeiling = 0.5;   // if unknown_count / total > 0.5, force CALIBRATING

    private const int SweepWindowMin = 5;  // minutes; T3→T3 within this is drill-down, beyond is suspicious

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        return Run(conn, ctx, null);
    }

    // Internal overload for testing
    internal DimensionResult Run(SqliteConnection conn, HealthContext ctx, string? telemetryPathOverride)
    {
        var result = Check(telemetryPathOverride);
        var detail = new Dictionary<string, object?>
        {
            ["zone"] = result.Zone,
            ["detail"] = result.Detail,
            ["hint"] = result.Hint,
            ["unknown_entries"] = result.UnknownEntries,
            ["meta_entries"] = result.MetaEntries,
            ["scored_entries"] = result.ScoredEntries
        };

        if (result.Transitions is not null)
        {
            detail["transitions"] = result.Transitions;
        }

        return new DimensionResult(Name, result.Score, detail);
    }

    private static CheckResult Check(string? telemetryPathOverride = null)
    {
        var entries = LoadEntries(telemetryPathOverride);
        var total = entries.Count;
        var unknownCount = entries.Count(e => e.Tier is null);
        var metaCount = entries.Count(e => e.Tier == 0);
        var scored = entries.Where(e => e.Tier is >= 1 and <= 3).ToList();
        var scoredN = scored.Count;

        var baseResult = new CheckResult
        {
            UnknownEntries = unknownCount,
            MetaEntries = metaCount,
            ScoredEntries = scoredN
        };

        // Guard 1: mixed-schema drain
        if (total > 0 && (double)unknownCount / total > UnknownCeiling)
        {
            return baseResult with
            {
                Score = null,
                Zone = "CALIBRATING",
                Detail = $"unknown={unknownCount}/{total} (>{(int)(UnknownCeiling * 100)}%) — legacy entries draining",
                Hint = "mixed-schema telemetry — waiting for legacy entries to drain"
            };
        }

        // Guard 2: insufficient sample
        var firstTs = entries
            .Select(e => ParseTs(e.Ts))
            .FirstOrDefault(ts => ts.HasValue);
        var ageDays = firstTs.HasValue
            ? (DateTime.UtcNow - firstTs.Value).Days
            : 0;

        if (scoredN < MinSampleSize && ageDays < MinSampleDays)
        {
            return baseResult with
            {
                Score = null,
                Zone = "CALIBRATING",
                Detail = $"{scoredN}/{MinSampleSize} non-meta entries, {ageDays}d since first",
                Hint = $"Collecting baseline. Score activates at {MinSampleSize} entries or {MinSampleDays} days."
            };
        }

        // Distribution
        var tiers = scored.Select(e => e.Tier!.Value).ToList();
        var t1 = tiers.Count(t => t == 1) / (double)scoredN;
        var t2 = tiers.Count(t => t == 2) / (double)scoredN;
        var t3 = tiers.Count(t => t == 3) / (double)scoredN;
        var avg = tiers.Average();
        var transitions = ClassifyTransitions(scored);
        var escRate = EscalationRate(transitions);

        // Format percentages without space (match Python's .0% format)
        var t1Pct = $"{t1 * 100:0}%";
        var t2Pct = $"{t2 * 100:0}%";
        var t3Pct = $"{t3 * 100:0}%";
        var escRatePct = escRate.HasValue ? $"{escRate.Value * 100:0}%" : "";

        var detail = escRate.HasValue
            ? $"T1={t1Pct} T2={t2Pct} T3={t3Pct} avg={avg:F2} esc_rate={escRatePct} "
            : $"T1={t1Pct} T2={t2Pct} T3={t3Pct} avg={avg:F2} ";
        detail += $"(n={scoredN}, meta={metaCount}, unknown={unknownCount})";

        if (!ScoringActive)
        {
            return baseResult with
            {
                Score = null,
                Zone = "CALIBRATING",
                Detail = detail,
                Hint = "Baseline collected. Run `session-recall calibrate --analyze`, then hand-edit thresholds + flip SCORING_ACTIVE=True."
            };
        }

#pragma warning disable CS0162 // Unreachable code detected (Stage 1: ScoringActive=false by design)
        // Stage 2 scoring (only reached after operator activates)
        string zone;
        double score;
        string hint;

        if (t3 > T3PolicyFloor)
        {
            zone = "RED";
            score = 2.0;
            hint = $"T3={t3:P0} > policy floor {T3PolicyFloor:P0} — agent skipping ladder";
        }
        else if (avg >= GreenAvgLow && avg <= GreenAvgHigh)
        {
            zone = "GREEN";
            score = 8.0;
            hint = "";
        }
        else if (avg >= AmberAvgLow && avg <= AmberAvgHigh)
        {
            zone = "AMBER";
            score = 5.0;
            hint = $"avg_tier={avg:F2} outside 1σ band";
        }
        else
        {
            zone = "RED";
            score = 2.0;
            hint = $"avg_tier={avg:F2} outside 2σ band";
        }

        return baseResult with
        {
            Score = score,
            Zone = zone,
            Detail = detail,
            Hint = hint,
            Transitions = transitions
        };
#pragma warning restore CS0162
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

    private static DateTime? ParseTs(string? ts)
    {
        if (string.IsNullOrEmpty(ts))
            return null;

        try
        {
            return DateTime.ParseExact(ts, "yyyy-MM-ddTHH:mm:ssZ",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }
        catch
        {
            return null;
        }
    }

    internal static TransitionCounts ClassifyTransitions(List<TelemetryJsonEntry> scored)
    {
        var healthy = 0;
        var neutral = 0;
        var suspicious = 0;
        var repetition = 0;

        var sortedE = scored.OrderBy(e => e.Ts ?? string.Empty).ToList();

        // Analyze adjacent pairs
        for (var i = 0; i < sortedE.Count - 1; i++)
        {
            var prev = sortedE[i];
            var curr = sortedE[i + 1];
            var pt = prev.Tier;
            var ct = curr.Tier;

            if (!pt.HasValue || !ct.HasValue)
                continue;

            var pts = ParseTs(prev.Ts);
            var cts = ParseTs(curr.Ts);
            var gapMin = (pts.HasValue && cts.HasValue)
                ? (cts.Value - pts.Value).TotalMinutes
                : 0;

            if (pt == 1 && (ct == 2 || ct == 3))
            {
                healthy++;
            }
            else if (pt == 2 && ct == 3)
            {
                healthy++;
            }
            else if (pt == 3 && ct == 3)
            {
                if (gapMin <= SweepWindowMin)
                    neutral++;
                else
                    suspicious++;
            }
            else if (pt == 2 && ct == 2 &&
                     !string.IsNullOrEmpty(prev.QueryHash) &&
                     prev.QueryHash == curr.QueryHash)
            {
                repetition++;
            }
        }

        // Cold-start T3: a T3 with no T1/T2 in the preceding SWEEP_WINDOW_MIN
        for (var i = 0; i < sortedE.Count; i++)
        {
            var e = sortedE[i];
            if (e.Tier != 3)
                continue;

            var ets = ParseTs(e.Ts);
            if (!ets.HasValue)
                continue;

            var windowStart = ets.Value.AddMinutes(-SweepWindowMin);
            var preceded = sortedE.Take(i).Any(p =>
                (p.Tier == 1 || p.Tier == 2) &&
                ParseTs(p.Ts) is { } pTs &&
                pTs >= windowStart);

            if (!preceded)
                suspicious++;
        }

        return new TransitionCounts(healthy, neutral, suspicious, repetition);
    }

    private static double? EscalationRate(TransitionCounts t)
    {
        var denom = t.Healthy + t.Suspicious;
        if (denom == 0)
            return null;
        return (double)t.Healthy / denom;
    }

    private sealed record CheckResult
    {
        public double? Score { get; init; }
        public string Zone { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public string Hint { get; init; } = string.Empty;
        public int UnknownEntries { get; init; }
        public int MetaEntries { get; init; }
        public int ScoredEntries { get; init; }
        public TransitionCounts? Transitions { get; init; }
    }

    internal sealed record TransitionCounts(
        int Healthy,
        int Neutral,
        int Suspicious,
        int Repetition
    );
}
