using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeSessions;

namespace AutoMemory.Cli.Commands;

public static class VsCodeSearchCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode session fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var store = SessionStateStore.Default();
        var query = args.GetPositional(0) ?? string.Empty;
        var limit = args.GetInt("limit") ?? 5;
        var jsonMode = args.GetFlag("json");

        if (string.IsNullOrWhiteSpace(query))
        {
            var emptyResult = new
            {
                query,
                count = 0,
                results = Array.Empty<object>(),
                source = "vscode-session-state",
                warning = "Empty query — nothing to search"
            };
            Console.WriteLine(JsonSerializer.Serialize(emptyResult, s_jsonOptions));
            return 0;
        }

        var results = store.Search(query, limit);

        if (jsonMode)
        {
            var items = results.Select(r => new
            {
                session_id = r.SessionId.Length >= 8 ? r.SessionId[..8] : r.SessionId,
                session_id_full = r.SessionId,
                source_type = r.Source,
                date = r.Date,
                excerpt = r.Excerpt
            });
            
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                query,
                count = results.Count,
                results = items,
                source = "vscode-session-state"
            }, s_jsonOptions));
        }
        else
        {
            foreach (var r in results)
                Console.WriteLine($"{r.Date}  {r.SessionId[..8]}  {r.Source}  {r.Excerpt}");
        }

        return 0;
    }
}
