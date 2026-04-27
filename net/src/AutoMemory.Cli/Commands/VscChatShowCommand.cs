using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeChat;

namespace AutoMemory.Cli.Commands;

public static class VscChatShowCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode Chat fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var id    = args.GetPositional(0);
        var store = ChatSessionStore.Default();

        if (string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("error: the following arguments are required: session_id");
            return 2;
        }

        var s = store.Show(id);

        if (s is null)
        {
            Console.Error.WriteLine($"No session found matching '{id}'");
            return 1;
        }

        // Stream the first 50 user messages for detail view
        var turns = TranscriptReader.Read(s.TranscriptPath)
            .Where(e => e.Type == "user.message")
            .Take(50)
            .Select(e => new
            {
                role    = "user",
                ts      = e.Timestamp.ToString("o"),
                content = e.Data.TryGetProperty("content", out var c) ? c.GetString() : null,
            })
            .ToList();

        var result = new
        {
            id        = s.Id,
            date      = s.CreatedAt.ToString("yyyy-MM-dd"),
            repo      = s.Repository,
            workspace = s.WorkspacePath,
            summary   = s.Summary,
            turns     = turns,
            source    = "vscode-chat",
        };

        Console.WriteLine(JsonSerializer.Serialize(result, s_jsonOptions));
        return 0;
    }
}
