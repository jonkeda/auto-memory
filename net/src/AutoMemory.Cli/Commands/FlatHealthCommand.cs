using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMemory.Core.Health;
using AutoMemory.Core.Health.Flat;
using AutoMemory.Core.Util;

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Flat-file health command — run health dimensions against VS Code flat-file backends
/// when no SQLite database is available.
/// </summary>
internal static class FlatHealthCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming", 
        "IL2026", 
        Justification = "Fallback JSON serialization for flat-file health; anonymous types OK for this use case")]
    public static int Run(ParsedArgs args)
    {
        var repo = DetectRepo.Detect();
        var ctx  = FlatHealthContext.Collect(repo);

        IFlatHealthDimension[] dims =
        [
            new DimFlatFreshness(),
            new DimFlatCorpus(),
            new DimFlatRepoCoverage(),
            new DimFlatSummaryCoverage(),
            new DimFlatRecentActivity(),
        ];

        var results = dims.Select(d => d.Score(ctx)).ToArray();

        // Compute overall score: average of non-null scores, null if all CALIBRATING
        var scoredResults = results.Where(r => r.Score.HasValue).ToArray();
        double? overall = scoredResults.Length > 0
            ? scoredResults.Average(r => r.Score!.Value)
            : null;

        // Build top_hints: hints from RED or AMBER zones, max 3
        var hints = results
            .Where(r => (r.Zone == "RED" || r.Zone == "AMBER") && !string.IsNullOrWhiteSpace(r.Hint))
            .Select(r => r.Hint)
            .Distinct()
            .Take(3)
            .ToArray();

        var output = new
        {
            overall_score = overall.HasValue ? Math.Round(overall.Value, 1) : (double?)null,
            dims = results.Select(r => new
            {
                name   = r.Name,
                score  = r.Score.HasValue ? Math.Round(r.Score.Value, 1) : (double?)null,
                zone   = r.Zone,
                detail = r.Detail,
                hint   = r.Hint,
            }).ToArray(),
            top_hints = hints,
            source    = "vscode-flat",
            backends  = new
            {
                session_state = ctx.SessionStateSessions,
                vscode_chat   = ctx.ChatSessions,
            },
        };

        Console.WriteLine(JsonSerializer.Serialize(output, s_jsonOptions));
        return 0;
    }
}
