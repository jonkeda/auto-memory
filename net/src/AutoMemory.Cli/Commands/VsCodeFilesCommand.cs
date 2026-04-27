using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using AutoMemory.Core.VsCodeSessions;

namespace AutoMemory.Cli.Commands;

public static class VsCodeFilesCommand
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "VsCode session fallback - not AOT compiled")]
    public static int Run(ParsedArgs args)
    {
        var store = SessionStateStore.Default();
        var limit = args.GetInt("limit") ?? 10;
        var days = args.GetInt("days") ?? 30;
        var jsonMode = args.GetFlag("json");

        var files = store.Files(limit, days);

        if (jsonMode)
        {
            var items = files.Select(f => new
            {
                file_path = f.FilePath,
                session_id = f.SessionId.Length >= 8 ? f.SessionId[..8] : f.SessionId,
                date = f.Date
            });

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                count = files.Count,
                files = items,
                source = "vscode-session-state"
            }, s_jsonOptions));
        }
        else
        {
            foreach (var f in files)
                Console.WriteLine($"{f.Date}  {f.SessionId[..8]}  {f.FilePath}");
        }

        return 0;
    }
}
