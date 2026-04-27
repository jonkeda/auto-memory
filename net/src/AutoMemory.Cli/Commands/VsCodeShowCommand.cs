using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeSessions;

namespace AutoMemory.Cli.Commands;

public static class VsCodeShowCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode session fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var store = SessionStateStore.Default();
        var sessionId = args.GetPositional(0);
        
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Console.Error.WriteLine("error: the following arguments are required: session_id");
            return 2;
        }

        var session = store.Show(sessionId);
        if (session is null)
        {
            Console.Error.WriteLine($"No session found matching '{sessionId}'");
            return 1;
        }

        var jsonMode = args.GetFlag("json");
        var fullFlag = args.GetFlag("full");
        var maxLength = fullFlag ? 99999 : 500;

        // Get tool calls from events
        var toolCalls = EventsReader.ToolCalls(session.DirectoryPath).ToList();

        // Get files from tool arguments
        var files = EventsReader.TouchedFiles(session.DirectoryPath)
            .Select(f => new { file_path = f })
            .ToList();

        // Get checkpoints if they exist
        var checkpoints = ReadCheckpoints(session.DirectoryPath);

        var result = new
        {
            id = session.Id,
            repository = session.Repository ?? "",
            branch = session.Branch ?? "",
            summary = session.Summary ?? "",
            created_at = session.CreatedAt.ToString("o"),
            tool_calls = toolCalls.Select(tc => new
            {
                name = tc.Name,
                timestamp = tc.Timestamp.ToString("o")
            }),
            files,
            checkpoints = checkpoints.Select(c => new
            {
                n = c.N,
                title = c.Title,
                overview = Truncate(c.Overview, maxLength)
            }),
            source = "vscode-session-state"
        };

        Console.WriteLine(JsonSerializer.Serialize(result, s_jsonOptions));
        return 0;
    }

    private static List<(int N, string Title, string Overview)> ReadCheckpoints(string sessionDir)
    {
        var indexPath = Path.Combine(sessionDir, "checkpoints", "index.md");
        if (!File.Exists(indexPath))
            return new List<(int, string, string)>();

        var checkpoints = new List<(int N, string Title, string Overview)>();
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
                    checkpoints.Add((n, title, overview.Trim()));
                }
            }
        }

        return checkpoints;
    }

    private static string? Truncate(string? s, int max) =>
        s is null ? null : s.Length <= max ? s : s[..max] + "…";
}
