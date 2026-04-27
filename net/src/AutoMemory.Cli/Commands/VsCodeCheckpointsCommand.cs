using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeSessions;

namespace AutoMemory.Cli.Commands;

public static class VsCodeCheckpointsCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode session fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var store = SessionStateStore.Default();
        var limit = args.GetInt("limit") ?? 5;
        var days = args.GetInt("days") ?? 30;
        var jsonMode = args.GetFlag("json");

        // Get recent sessions and extract checkpoints from each
        var sessions = store.List(limit: 50, days, repo: null);
        var checkpoints = new List<CheckpointItem>();

        foreach (var session in sessions)
        {
            var sessionCheckpoints = ReadCheckpoints(session.DirectoryPath, session.Id, session.CreatedAt.ToString("yyyy-MM-dd"));
            checkpoints.AddRange(sessionCheckpoints);
        }

        // Sort by date descending and take limit
        checkpoints = checkpoints
            .OrderByDescending(c => c.Date)
            .Take(limit)
            .ToList();

        if (jsonMode)
        {
            var items = checkpoints.Select(c => new
            {
                checkpoint_number = c.Number,
                title = c.Title,
                overview = c.Overview,
                date = c.Date,
                session_id = c.SessionId
            });

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                count = checkpoints.Count,
                checkpoints = items,
                source = "vscode-session-state"
            }, s_jsonOptions));
        }
        else
        {
            foreach (var c in checkpoints)
                Console.WriteLine($"{c.Date}  {c.SessionId}  Checkpoint {c.Number}: {c.Title}");
        }

        return 0;
    }

    private static List<CheckpointItem> ReadCheckpoints(string sessionDir, string sessionId, string date)
    {
        var checkpoints = new List<CheckpointItem>();
        var indexPath = Path.Combine(sessionDir, "checkpoints", "index.md");
        
        if (!File.Exists(indexPath))
            return checkpoints;

        var lines = File.ReadAllLines(indexPath);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // Match lines like "## Checkpoint 1: Title here"
            if (line.StartsWith("## Checkpoint ", StringComparison.Ordinal))
            {
                var parts = line["## Checkpoint ".Length..].Split(':', 2);
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var n))
                {
                    var title = parts[1].Trim();
                    // Read overview (next few lines until next checkpoint or end)
                    var overview = "";
                    for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("## Checkpoint ", StringComparison.Ordinal); j++)
                    {
                        if (!string.IsNullOrWhiteSpace(lines[j]))
                            overview += lines[j] + " ";
                    }
                    
                    // Truncate to 300 chars to match Python behavior
                    var truncatedOverview = overview.Trim();
                    if (truncatedOverview.Length > 300)
                        truncatedOverview = truncatedOverview[..300];

                    checkpoints.Add(new CheckpointItem
                    {
                        Number = n,
                        Title = title,
                        Overview = truncatedOverview,
                        Date = date,
                        SessionId = sessionId.Length >= 8 ? sessionId[..8] : sessionId
                    });
                }
            }
        }

        return checkpoints;
    }

    private sealed record CheckpointItem
    {
        public required int Number { get; init; }
        public required string Title { get; init; }
        public required string Overview { get; init; }
        public required string Date { get; init; }
        public required string SessionId { get; init; }
    }
}
