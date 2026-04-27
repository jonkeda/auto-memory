using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeSessions;

namespace AutoMemory.Cli.Commands;

public static class VsCodeListCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode session fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var store  = SessionStateStore.Default();
        var repo   = args.GetOption("repo");
        var limit  = args.GetInt("limit") ?? 10;
        var days   = args.GetInt("days") ?? 30;
        var json   = args.GetFlag("json");

        var sessions = store.List(limit, days, repo);

        if (json)
        {
            var items = sessions.Select(s => new
            {
                id      = s.Id,
                date    = s.CreatedAt.ToString("yyyy-MM-dd"),
                repo    = s.Repository,
                summary = Truncate(s.Summary, 100),
            });
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                sessions = items,
                source   = "vscode-session-state",
            }, s_jsonOptions));
        }
        else
        {
            foreach (var s in sessions)
                Console.WriteLine($"{s.CreatedAt:yyyy-MM-dd}  {s.Id[..8]}  {s.Repository ?? "-"}  {Truncate(s.Summary, 60)}");
        }
        return 0;
    }

    private static string? Truncate(string? s, int max) =>
        s is null ? null : s.Length <= max ? s : s[..max] + "…";
}
