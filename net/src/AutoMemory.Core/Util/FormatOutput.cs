using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#nullable enable

namespace AutoMemory.Core.Util;

/// <summary>
/// Output formatting for human-readable and JSON modes.
/// </summary>
public static class FormatOutput
{
    // Regex pattern that matches all ANSI/OSC/control sequences that should be stripped.
    // - CSI sequences: ESC\[[0-?]*[ -/]*[@-~]  (colors, cursor moves)
    // - OSC sequences: ESC\][^BEL,ESC]*(?:BEL|ESC\\)  (title, clipboard, hyperlinks)
    // - Other ESC-prefixed: ESC[@-Z\\-_]  (Fp, Fe, Fs)
    // - C0 controls (except TAB \x09, LF \x0a, CR \x0d): [\x00-\x08\x0b\x0c\x0e-\x1f\x7f]
    // - C1 controls: [\x80-\x9f]
    private static readonly Regex s_controlSequenceRegex = new(
        @"\u001b\[[0-?]*[ -/]*[@-~]|\u001b\][^\u0007\u001b]*(?:\u0007|\u001b\\)|\u001b[@-Z\\-_]|[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]|[\u0080-\u009F]",
        RegexOptions.Compiled);

    /// <summary>
    /// Fallback JSON options for types not in the source-generated context (e.g., test fixtures).
    /// </summary>
    private static readonly JsonSerializerOptions s_fallbackJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Strip ANSI/OSC/control sequences so session content can't hijack the terminal.
    /// Preserves UTF-8 printable characters and legitimate whitespace (TAB, LF, CR).
    /// </summary>
    /// <param name="s">Input string (may be null).</param>
    /// <returns>Sanitized string with control sequences removed, or empty string if input is null.</returns>
    public static string SanitizeForTerminal(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }
        return s_controlSequenceRegex.Replace(s, string.Empty);
    }

    /// <summary>
    /// Format data as JSON with 2-space indentation and snake_case property names.
    /// Uses source-generated serialization for AOT/trimming compatibility.
    /// </summary>
    /// <param name="data">Data to serialize (object, list, etc.).</param>
    /// <returns>Pretty-printed JSON string.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Fallback for test types only; production types use source generation")]
    public static string FmtJson(object? data)
    {
        if (data == null)
        {
            return "null";
        }

        // Use type-specific serialization from the source-generated context for production types
        var json = data switch
        {
            ListResult lr => JsonSerializer.Serialize(lr, AutoMemoryJsonContext.Default.ListResult),
            SearchResult sr => JsonSerializer.Serialize(sr, AutoMemoryJsonContext.Default.SearchResult),
            FilesResult fr => JsonSerializer.Serialize(fr, AutoMemoryJsonContext.Default.FilesResult),
            CheckpointsResult cr => JsonSerializer.Serialize(cr, AutoMemoryJsonContext.Default.CheckpointsResult),
            ShowSessionResult ssr => JsonSerializer.Serialize(ssr, AutoMemoryJsonContext.Default.ShowSessionResult),
            SessionListItem sli => JsonSerializer.Serialize(sli, AutoMemoryJsonContext.Default.SessionListItem),
            HealthResult hr => JsonSerializer.Serialize(hr, AutoMemoryJsonContext.Default.HealthResult),
            SchemaCheckJsonResult scjr => JsonSerializer.Serialize(scjr, AutoMemoryJsonContext.Default.SchemaCheckJsonResult),
            List<HealthDimResult> hdr => JsonSerializer.Serialize(hdr, AutoMemoryJsonContext.Default.ListHealthDimResult),
            List<TelemetryEntry> te => JsonSerializer.Serialize(te, AutoMemoryJsonContext.Default.ListTelemetryEntry),
            _ => JsonSerializer.Serialize(data, s_fallbackJsonOptions)
        };

        // Post-process SearchResult JSON to fix null handling issues
        if (data is SearchResult)
        {
            // Remove "warning": null line (handle comma before the field, and the newline after)
            json = Regex.Replace(json, @",\s*\n\s*""warning"":\s*null", "");
            // Fix empty string summaries to be null (preserve the field but change "" to null)
            json = Regex.Replace(json, @"(""summary"":\s*)""""", "$1null");
        }

        return json;
    }

    /// <summary>
    /// Format session list as a human-readable table with fixed-width columns.
    /// </summary>
    /// <param name="sessions">Session records to display.</param>
    /// <returns>Formatted table string.</returns>
    public static string FmtHumanSessions(IEnumerable<SessionRecord> sessions)
    {
        var sessionList = sessions.ToList();
        if (sessionList.Count == 0)
        {
            return "No sessions found.";
        }

        var sb = new StringBuilder();
        
        // Header row
        sb.AppendLine($"{"ID",-8}  {"Date",-10}  {"Turns",5}  Summary");
        sb.AppendLine(new string('-', 60));
        
        // Data rows
        foreach (var session in sessionList)
        {
            // ID: sanitize first, then take first 8 characters
            string idShort = SanitizeForTerminal(session.Id);
            if (idShort.Length > 8)
            {
                idShort = idShort[..8];
            }
            
            // Date: sanitize first, then take first 10 characters (YYYY-MM-DD)
            string date = SanitizeForTerminal(session.CreatedAt);
            if (date.Length > 10)
            {
                date = date[..10];
            }
            
            // Turns: formatted as string
            string turns = session.TurnsCount.ToString(CultureInfo.InvariantCulture);
            
            // Summary: sanitize first, then truncate to 40 characters with fallback to "(untitled)"
            string summary = string.IsNullOrEmpty(session.Summary) ? "(untitled)" : session.Summary;
            summary = SanitizeForTerminal(summary);
            if (summary.Length > 40)
            {
                summary = summary[..40];
            }
            
            sb.AppendLine($"{idShort,-8}  {date,-10}  {turns,5}  {summary}");
        }
        
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Print data in the requested format to stdout.
    /// </summary>
    /// <param name="data">Data to output.</param>
    /// <param name="jsonMode">If true, always output JSON; otherwise use human-readable format when applicable.</param>
    public static void Output(object? data, bool jsonMode = false)
    {
        if (jsonMode)
        {
            Console.WriteLine(FmtJson(data));
        }
        else if (data is IEnumerable<SessionRecord> sessions)
        {
            Console.WriteLine(FmtHumanSessions(sessions));
        }
        else if (data is ListResult listResult)
        {
            // For ListResult in human mode, just show sessions table
            Console.WriteLine(FmtHumanSessionListItems(listResult.Sessions));
        }
        else if (data is SearchResult searchResult)
        {
            // For SearchResult, always use JSON in human mode (Python behavior)
            Console.WriteLine(FmtJson(searchResult));
        }
        else
        {
            // For other types, fall back to JSON
            Console.WriteLine(FmtJson(data));
        }
    }

    /// <summary>
    /// Format session list items as a human-readable table.
    /// </summary>
    /// <param name="sessions">Session list items to display.</param>
    /// <returns>Formatted table string.</returns>
    private static string FmtHumanSessionListItems(IEnumerable<SessionListItem> sessions)
    {
        var sessionList = sessions.ToList();
        if (sessionList.Count == 0)
        {
            return "No sessions found.";
        }

        var sb = new StringBuilder();
        
        // Header row
        sb.AppendLine($"{"ID",-8}  {"Date",-10}  {"Turns",5}  Summary");
        sb.AppendLine(new string('-', 60));
        
        // Data rows
        foreach (var session in sessionList)
        {
            // ID: sanitize first, then take first 8 characters
            string idShort = SanitizeForTerminal(session.IdShort);
            if (idShort.Length > 8)
            {
                idShort = idShort[..8];
            }
            
            // Date: sanitize first, then take first 10 characters (YYYY-MM-DD)
            string date = SanitizeForTerminal(session.Date ?? session.CreatedAt);
            if (date.Length > 10)
            {
                date = date[..10];
            }
            
            // Turns: formatted as string
            string turns = session.TurnsCount.ToString(CultureInfo.InvariantCulture);
            
            // Summary: sanitize first, then truncate to 40 characters with fallback to "(untitled)"
            string summary = string.IsNullOrEmpty(session.Summary) ? "(untitled)" : session.Summary;
            summary = SanitizeForTerminal(summary);
            if (summary.Length > 40)
            {
                summary = summary[..40];
            }
            
            sb.AppendLine($"{idShort,-8}  {date,-10}  {turns,5}  {summary}");
        }
        
        return sb.ToString().TrimEnd();
    }
}
