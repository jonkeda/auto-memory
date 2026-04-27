using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeChat;

namespace AutoMemory.Cli.Commands;

public static class VscChatSearchCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode Chat fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var query = args.GetPositional(0) ?? args.GetOption("query") ?? string.Empty;
        var limit = args.GetInt("limit") ?? 10;
        var store = ChatSessionStore.Default();
        var json  = args.GetFlag("json");

        if (string.IsNullOrWhiteSpace(query))
        {
            var emptyResult = new
            {
                query,
                sessions = Array.Empty<object>(),
                source   = "vscode-chat",
            };
            Console.WriteLine(JsonSerializer.Serialize(emptyResult, s_jsonOptions));
            return 0;
        }

        var hits = store.Search(query, limit);

        if (json)
        {
            var items = hits.Select(s => new
            {
                id      = s.Id.Length >= 8 ? s.Id[..8] : s.Id,
                date    = s.CreatedAt.ToString("yyyy-MM-dd"),
                repo    = s.Repository,
                summary = s.Summary,
                turns   = s.TurnCount,
            });

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                query,
                sessions = items,
                source   = "vscode-chat",
            }, s_jsonOptions));
        }
        else
        {
            foreach (var s in hits)
                Console.WriteLine($"{s.CreatedAt:yyyy-MM-dd}  {s.Id[..8]}  {s.Repository ?? "-"}  {Truncate(s.Summary, 60)}");
        }

        return 0;
    }

    private static string? Truncate(string? s, int max) =>
        s is null ? null : s.Length <= max ? s : s[..max] + "…";
}
