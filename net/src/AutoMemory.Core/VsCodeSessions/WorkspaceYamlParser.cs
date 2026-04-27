using System;
using System.IO;

namespace AutoMemory.Core.VsCodeSessions;

/// <summary>
/// Parses workspace.yaml using a simple key: value line scanner.
/// Avoids a third-party YAML library; the file format is flat and predictable.
/// </summary>
public static class WorkspaceYamlParser
{
    public static VsCodeSession Parse(string sessionDir)
    {
        var yamlPath = Path.Combine(sessionDir, "workspace.yaml");
        var lines = File.ReadAllLines(yamlPath);

        string? id = null, repo = null, branch = null, cwd = null, summary = null;
        DateTimeOffset createdAt = default, updatedAt = default;
        bool inSummary = false;
        var summaryLines = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (inSummary)
            {
                // Summary is a multiline YAML block scalar starting with a space
                if (line.StartsWith(' ') || line.StartsWith('\t'))
                {
                    summaryLines.AppendLine(line.TrimStart());
                    continue;
                }
                inSummary = false;
            }

            var kv = SplitKv(line);
            if (kv is null) continue;
            var (key, val) = kv.Value;

            switch (key)
            {
                case "id":         id = val; break;
                case "repository": repo = val; break;
                case "branch":     branch = val; break;
                case "cwd":        cwd = val; break;
                case "created_at":
                    _ = DateTimeOffset.TryParse(val, out createdAt); break;
                case "updated_at":
                    _ = DateTimeOffset.TryParse(val, out updatedAt); break;
                case "summary":
                    if (val == "|" || val == ">")
                        inSummary = true;
                    else
                        summary = UnquoteYaml(val);
                    break;
            }
        }

        if (summaryLines.Length > 0)
            summary = summaryLines.ToString().Trim();

        return new VsCodeSession
        {
            Id            = id ?? Path.GetFileName(sessionDir),
            Repository    = repo,
            Branch        = branch,
            Cwd           = cwd,
            Summary       = summary,
            CreatedAt     = createdAt,
            UpdatedAt     = updatedAt,
            DirectoryPath = sessionDir,
        };
    }

    private static (string key, string val)? SplitKv(string line)
    {
        var colon = line.IndexOf(':');
        if (colon <= 0) return null;
        var key = line[..colon].Trim();
        var val = line[(colon + 1)..].Trim();
        return (key, val);
    }

    private static string UnquoteYaml(string s)
    {
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            return s[1..^1].Replace("\\r\\n", "\n").Replace("\\n", "\n");
        return s;
    }
}
