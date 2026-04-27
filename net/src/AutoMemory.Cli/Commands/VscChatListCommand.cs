using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeChat;

namespace AutoMemory.Cli.Commands;

public static class VscChatListCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode Chat fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var store  = ChatSessionStore.Default();
        var repo   = args.GetOption("repo");
        var limit  = args.GetInt("limit") ?? 10;
        var days   = args.GetInt("days");
        var json   = args.GetFlag("json");

        var sessions = store.List(limit, days, repo);
        var total    = store.Count();

        if (json)
        {
            var items = sessions.Select(s => new
            {
                id        = s.Id.Length >= 8 ? s.Id[..8] : s.Id,
                date      = s.CreatedAt.ToString("yyyy-MM-dd"),
                repo      = s.Repository,
                workspace = s.WorkspacePath,
                summary   = s.Summary,
                turns     = s.TurnCount,
                tools     = s.ToolCount,
            });
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                sessions = items,
                source   = "vscode-chat",
                total    = total,
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
