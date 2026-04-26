using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoMemory.Core.Search;

/// <summary>
/// Sanitizes user queries for FTS5 MATCH expressions.
/// </summary>
public static partial class FtsSanitizer
{
    // FTS5 special chars that cause syntax errors when unquoted
    [GeneratedRegex(@"[.\-(){}\[\]^~*:""+/\\@#$%&!?<>=|]")]
    private static partial Regex Fts5SpecialChars();

    /// <summary>
    /// Escape FTS5 special characters and add prefix matching.
    /// Returns null for empty/whitespace-only queries.
    /// </summary>
    /// <param name="raw">The raw user query string.</param>
    /// <returns>Sanitized query string or null if empty/whitespace-only.</returns>
    public static string? SanitizeFts5Query(string raw)
    {
        var stripped = raw.Trim();
        if (string.IsNullOrWhiteSpace(stripped))
        {
            return null;
        }

        var tokens = stripped.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var safeTokens = new List<string>(tokens.Length);

        foreach (var tok in tokens)
        {
            // Escape internal double quotes
            var escaped = tok.Replace("\"", "\"\"", StringComparison.Ordinal);
            
            if (Fts5SpecialChars().IsMatch(tok))
            {
                // Quote the whole token to treat special chars as literals
                safeTokens.Add($"\"{escaped}\"");
            }
            else
            {
                // Bare token with prefix wildcard for partial matching
                safeTokens.Add($"{escaped}*");
            }
        }

        return string.Join(" ", safeTokens);
    }
}
