using AutoMemory.Core;
using AutoMemory.Core.Util;
using Xunit;

#nullable enable

namespace AutoMemory.Tests.Util;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public class FormatOutputTests
{
    private static readonly string[] s_splitSeparator = ["  "];

    [Fact]
    public void SanitizeForTerminal_StripsCsiColorCodes()
    {
        // CSI color codes should be stripped
        var result = FormatOutput.SanitizeForTerminal("\u001b[31mred text\u001b[0m");
        Assert.Equal("red text", result);
    }

    [Fact]
    public void SanitizeForTerminal_StripsOscTitle()
    {
        // OSC title-set should be stripped entirely
        var result = FormatOutput.SanitizeForTerminal("hi\u001b]0;HACKED\u0007there");
        Assert.Equal("hithere", result);
    }

    [Fact]
    public void SanitizeForTerminal_StripsOscClipboard()
    {
        // OSC 52 clipboard injection should be stripped
        var result = FormatOutput.SanitizeForTerminal("\u001b]52;c;ZXZpbA==\u0007ok");
        Assert.Equal("ok", result);
    }

    [Fact]
    public void SanitizeForTerminal_StripsOscHyperlink()
    {
        // OSC 8 hyperlink should strip wrapper, keep visible text
        var input = "\u001b]8;;evil.com\u0007github.com\u001b]8;;\u0007";
        var result = FormatOutput.SanitizeForTerminal(input);
        Assert.Equal("github.com", result);
        Assert.DoesNotContain("\u001b", result);
    }

    [Fact]
    public void SanitizeForTerminal_StripsC0Controls()
    {
        // NUL, BEL, BS, DEL should be stripped
        // Note: Using \u#### Unicode escapes to avoid ambiguous hex escape sequences
        var input = "a\u0000b\u0007c\u0008d\u007F";
        var result = FormatOutput.SanitizeForTerminal(input);
        Assert.Equal("abcd", result);
    }

    [Fact]
    public void SanitizeForTerminal_PreservesTabNewlineCr()
    {
        // TAB, LF, CR are legitimate whitespace — must survive
        var result = FormatOutput.SanitizeForTerminal("a\tb\nc\rd");
        Assert.Equal("a\tb\nc\rd", result);
    }

    [Fact]
    public void SanitizeForTerminal_HandlesNull()
    {
        // None input returns empty string
        var result = FormatOutput.SanitizeForTerminal(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeForTerminal_PreservesUnicode()
    {
        // Non-ASCII printable characters should pass through unchanged
        var result = FormatOutput.SanitizeForTerminal("café 日本 🎉");
        Assert.Equal("café 日本 🎉", result);
    }

    [Fact]
    public void SanitizeForTerminal_HandlesEmptyString()
    {
        var result = FormatOutput.SanitizeForTerminal(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FmtJson_ProducesValidJson()
    {
        var data = new { Name = "Test", Value = 42 };
        var json = FormatOutput.FmtJson(data);
        
        // Should be valid JSON
        Assert.NotNull(json);
        Assert.Contains("\"name\"", json); // snake_case
        Assert.Contains("\"value\"", json);
        Assert.Contains("\"Test\"", json);
        Assert.Contains("42", json);
    }

    [Fact]
    public void FmtJson_UsesSnakeCase()
    {
        var data = new { FirstName = "John", LastName = "Doe" };
        var json = FormatOutput.FmtJson(data);
        
        // Should use snake_case for property names
        Assert.Contains("\"first_name\"", json);
        Assert.Contains("\"last_name\"", json);
        Assert.DoesNotContain("FirstName", json);
        Assert.DoesNotContain("LastName", json);
    }

    [Fact]
    public void FmtJson_UsesTwoSpaceIndentation()
    {
        var data = new { Name = "Test" };
        var json = FormatOutput.FmtJson(data);
        
        // Should be indented (contains newlines)
        Assert.Contains("\n", json);
        
        // Should use 2-space indentation (check for "  " at start of property line)
        var lines = json.Split('\n');
        Assert.Contains(lines, line => line.StartsWith("  \""));
    }

    [Fact]
    public void FmtHumanSessions_HandlesEmptyList()
    {
        var sessions = new List<SessionRecord>();
        var result = FormatOutput.FmtHumanSessions(sessions);
        
        Assert.Equal("No sessions found.", result);
    }

    [Fact]
    public void FmtHumanSessions_FormatsTableCorrectly()
    {
        var sessions = new List<SessionRecord>
        {
            new SessionRecord
            {
                Id = "abcd1234567890",
                Repository = "test-repo",
                Branch = "main",
                Summary = "Test session summary",
                CreatedAt = "2025-04-01T10:00:00Z",
                UpdatedAt = "2025-04-01T11:00:00Z",
                TurnsCount = 12,
                FilesCount = 5
            },
            new SessionRecord
            {
                Id = "xyz98765",
                Repository = "another-repo",
                Branch = "dev",
                Summary = "Another test with a very long summary that will be truncated at forty characters exactly",
                CreatedAt = "2025-03-15T08:30:00Z",
                UpdatedAt = "2025-03-15T09:00:00Z",
                TurnsCount = 3,
                FilesCount = 2
            }
        };

        var result = FormatOutput.FmtHumanSessions(sessions);
        
        // Check header
        Assert.Contains("ID", result);
        Assert.Contains("Date", result);
        Assert.Contains("Turns", result);
        Assert.Contains("Summary", result);
        
        // Check separator line
        Assert.Contains("----", result);
        
        // Check data rows
        Assert.Contains("abcd1234", result); // ID truncated to 8 chars
        Assert.Contains("2025-04-01", result); // Date truncated to 10 chars
        Assert.Contains("12", result); // Turns count
        Assert.Contains("Test session summary", result);
        
        Assert.Contains("xyz98765", result);
        Assert.Contains("2025-03-15", result);
        Assert.Contains("3", result);
        
        // Summary should be truncated to 40 chars
        var lines = result.Split('\n');
        var dataLine = lines.FirstOrDefault(l => l.Contains("Another test"));
        Assert.NotNull(dataLine);
        
        // Extract the summary portion (after the third column)
        var parts = dataLine.Split(s_splitSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            var summary = parts[3];
            Assert.True(summary.Length <= 40, $"Summary length is {summary.Length}, expected <= 40");
        }
    }

    [Fact]
    public void FmtHumanSessions_SanitizesControlSequences()
    {
        var sessions = new List<SessionRecord>
        {
            new SessionRecord
            {
                Id = "\u001b[31mabcd1234\u001b[0m",
                Repository = "test-repo",
                Branch = "main",
                Summary = "Test \u001b]0;HACKED\u0007 summary",
                CreatedAt = "2025-04-01T10:00:00Z",
                UpdatedAt = "2025-04-01T11:00:00Z",
                TurnsCount = 5,
                FilesCount = 3
            }
        };

        var result = FormatOutput.FmtHumanSessions(sessions);
        
        // Should not contain any escape sequences
        Assert.DoesNotContain("\u001b", result);
        Assert.Contains("abcd1234", result);
        Assert.Contains("Test  summary", result); // HACKED stripped
    }

    [Fact]
    public void FmtHumanSessions_HandlesUntitledSummary()
    {
        var sessions = new List<SessionRecord>
        {
            new SessionRecord
            {
                Id = "test1234",
                Repository = "test-repo",
                Branch = "main",
                Summary = "",
                CreatedAt = "2025-04-01T10:00:00Z",
                UpdatedAt = "2025-04-01T11:00:00Z",
                TurnsCount = 1,
                FilesCount = 0
            }
        };

        var result = FormatOutput.FmtHumanSessions(sessions);
        
        Assert.Contains("(untitled)", result);
    }

    [Fact]
    public void FmtHumanSessions_HandlesShortId()
    {
        var sessions = new List<SessionRecord>
        {
            new SessionRecord
            {
                Id = "abc",
                Repository = "test-repo",
                Branch = "main",
                Summary = "Test",
                CreatedAt = "2025-04-01T10:00:00Z",
                UpdatedAt = "2025-04-01T11:00:00Z",
                TurnsCount = 1,
                FilesCount = 0
            }
        };

        var result = FormatOutput.FmtHumanSessions(sessions);
        
        // Should handle IDs shorter than 8 characters
        Assert.Contains("abc", result);
    }

    [Fact]
    public void FmtHumanSessions_HandlesShortDate()
    {
        var sessions = new List<SessionRecord>
        {
            new SessionRecord
            {
                Id = "test1234",
                Repository = "test-repo",
                Branch = "main",
                Summary = "Test",
                CreatedAt = "2025",
                UpdatedAt = "2025",
                TurnsCount = 1,
                FilesCount = 0
            }
        };

        var result = FormatOutput.FmtHumanSessions(sessions);
        
        // Should handle dates shorter than 10 characters
        Assert.Contains("2025", result);
    }

    [Fact]
    public void FmtJson_IntegersHaveNoTrailingDecimal()
    {
        // Python json.dumps never adds .0 to integers; we must match
        var data = new { Count = 42, Score = 100 };
        var json = FormatOutput.FmtJson(data);
        
        // Should contain bare integers, not "42.0"
        Assert.Contains("42", json);
        Assert.Contains("100", json);
        Assert.DoesNotContain(".0", json);
    }

    [Fact]
    public void FmtJson_FieldOrderingIsDeterministic()
    {
        // Field ordering must be stable across runs (based on record property declaration order)
        var data = new SessionListItem
        {
            IdShort = "abc12345",
            IdFull = "abc123456789",
            Repository = "test-repo",
            Branch = "main",
            Summary = "Test session",
            Date = "2025-04-01",
            CreatedAt = "2025-04-01T10:00:00Z",
            TurnsCount = 5,
            FilesCount = 3
        };

        // Serialize twice
        var json1 = FormatOutput.FmtJson(data);
        var json2 = FormatOutput.FmtJson(data);

        // Should be byte-identical
        Assert.Equal(json1, json2);

        // Field order should match record declaration order
        // (id_short before id_full before repository, etc.)
        var lines = json1.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith('"')).ToList();
        
        // Check that fields appear in expected order
        var idShortIdx = lines.FindIndex(l => l.StartsWith("\"id_short\"", StringComparison.Ordinal));
        var idFullIdx = lines.FindIndex(l => l.StartsWith("\"id_full\"", StringComparison.Ordinal));
        var repoIdx = lines.FindIndex(l => l.StartsWith("\"repository\"", StringComparison.Ordinal));
        var branchIdx = lines.FindIndex(l => l.StartsWith("\"branch\"", StringComparison.Ordinal));
        var summaryIdx = lines.FindIndex(l => l.StartsWith("\"summary\"", StringComparison.Ordinal));

        Assert.True(idShortIdx < idFullIdx, "id_short should appear before id_full");
        Assert.True(idFullIdx < repoIdx, "id_full should appear before repository");
        Assert.True(repoIdx < branchIdx, "repository should appear before branch");
        Assert.True(branchIdx < summaryIdx, "branch should appear before summary");
    }

    [Fact]
    public void FmtJson_RoundTripCompatibilityWithPython()
    {
        // This test ensures our JSON output matches Python json.dumps behavior
        // Python: json.dumps(data, indent=2, default=str)
        // Key requirements:
        // 1. snake_case field names
        // 2. No trailing .0 on integers
        // 3. Deterministic field ordering
        // 4. Two-space indentation

        var data = new ListResult
        {
            Repo = "test-repo",
            Count = 2,
            Sessions = new List<SessionListItem>
            {
                new SessionListItem
                {
                    IdShort = "abc12345",
                    IdFull = "abc123456789abcdef",
                    Repository = "test-repo",
                    Branch = "main",
                    Summary = "First session",
                    Date = "2025-04-01",
                    CreatedAt = "2025-04-01T10:00:00Z",
                    TurnsCount = 5,
                    FilesCount = 3
                },
                new SessionListItem
                {
                    IdShort = "def67890",
                    IdFull = "def67890123456789",
                    Repository = "test-repo",
                    Branch = "dev",
                    Summary = "Second session",
                    Date = "2025-04-02",
                    CreatedAt = "2025-04-02T11:00:00Z",
                    TurnsCount = 10,
                    FilesCount = 7
                }
            },
            RecentFiles = new List<RecentFileItem>()
        };

        var json = FormatOutput.FmtJson(data);

        // Verify snake_case
        Assert.Contains("\"repo\"", json);
        Assert.Contains("\"count\"", json);
        Assert.Contains("\"sessions\"", json);
        Assert.Contains("\"id_short\"", json);
        Assert.Contains("\"id_full\"", json);
        Assert.Contains("\"turns_count\"", json);
        Assert.Contains("\"files_count\"", json);
        Assert.Contains("\"recent_files\"", json);

        // Verify no trailing .0 on integers
        Assert.DoesNotContain("2.0", json);
        Assert.DoesNotContain("5.0", json);
        Assert.DoesNotContain("3.0", json);
        Assert.DoesNotContain("10.0", json);
        Assert.DoesNotContain("7.0", json);

        // Verify two-space indentation
        var lines = json.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(lines, l => l.StartsWith("  \"", StringComparison.Ordinal));
        
        // Verify proper JSON structure
        Assert.StartsWith("{", json.Trim());
        Assert.EndsWith("}", json.Trim());
    }

    [Fact]
    public void FmtJson_HandlesEmptyArrays()
    {
        var data = new ListResult
        {
            Repo = "test-repo",
            Count = 0,
            Sessions = new List<SessionListItem>(),
            RecentFiles = new List<RecentFileItem>()
        };

        var json = FormatOutput.FmtJson(data);

        // Empty arrays should be []
        Assert.Contains("[]", json);
        Assert.Contains("\"count\": 0", json);
        Assert.DoesNotContain("0.0", json);
    }
}
