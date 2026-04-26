using AutoMemory.Core.Search;
using Xunit;

namespace AutoMemory.Tests.Search;

/// <summary>
/// Tests for FTS5 query sanitization.
/// Covers all crash cases from the adversarial gap analysis.
/// </summary>
public class FtsSanitizerTests
{
    [Fact]
    public void DotInFilename()
    {
        Assert.Equal("\"CLAUDE.md\"", FtsSanitizer.SanitizeFts5Query("CLAUDE.md"));
    }

    [Fact]
    public void DotInPythonFile()
    {
        Assert.Equal("\"test.py\"", FtsSanitizer.SanitizeFts5Query("test.py"));
    }

    [Fact]
    public void HyphenTreatedAsNot()
    {
        Assert.Equal("\"session-recall\"", FtsSanitizer.SanitizeFts5Query("session-recall"));
    }

    [Fact]
    public void ParenthesesGrouping()
    {
        Assert.Equal("\"function()\"", FtsSanitizer.SanitizeFts5Query("function()"));
    }

    [Fact]
    public void EmptyStringReturnsNull()
    {
        Assert.Null(FtsSanitizer.SanitizeFts5Query(""));
    }

    [Fact]
    public void WhitespaceOnlyReturnsNull()
    {
        Assert.Null(FtsSanitizer.SanitizeFts5Query("   "));
    }

    [Fact]
    public void NormalWordGetsPrefixWildcard()
    {
        Assert.Equal("OAuth*", FtsSanitizer.SanitizeFts5Query("OAuth"));
    }

    [Fact]
    public void MultiWordAllGetPrefix()
    {
        Assert.Equal("memory* system*", FtsSanitizer.SanitizeFts5Query("memory system"));
    }

    [Fact]
    public void MixedSpecialAndNormal()
    {
        var result = FtsSanitizer.SanitizeFts5Query("fix CLAUDE.md now");
        Assert.Equal("fix* \"CLAUDE.md\" now*", result);
    }

    [Fact]
    public void DoubleQuotesEscaped()
    {
        var result = FtsSanitizer.SanitizeFts5Query("say \"hello\"");
        Assert.NotNull(result);
        Assert.Contains("\"\"hello\"\"", result);
    }

    [Fact]
    public void DoubleQuotesInTokenEscapedAndQuoted()
    {
        // Test with just a quoted word like "hello"
        var result = FtsSanitizer.SanitizeFts5Query("\"hello\"");
        Assert.NotNull(result);
        // Should be quoted and have escaped internal quotes
        Assert.StartsWith("\"", result);
        Assert.Contains("\"\"", result);
    }

    [Fact]
    public void AsteriskSpecial()
    {
        var result = FtsSanitizer.SanitizeFts5Query("*.py");
        Assert.NotNull(result);
        Assert.StartsWith("\"", result);
    }

    [Fact]
    public void ColonSpecial()
    {
        Assert.Equal("\"key:value\"", FtsSanitizer.SanitizeFts5Query("key:value"));
    }

    [Fact]
    public void SlashInPath()
    {
        Assert.Equal("\"src/main.py\"", FtsSanitizer.SanitizeFts5Query("src/main.py"));
    }

    // Property-style fuzz tests for special character classes
    [Theory]
    [InlineData('\x00', "word")] // NUL
    [InlineData('\x01', "word")] // SOH
    [InlineData('\x07', "word")] // BEL
    [InlineData('\x08', "word")] // BS
    [InlineData('\x09', "word")] // TAB
    [InlineData('\x0a', "word")] // LF
    [InlineData('\x0d', "word")] // CR
    [InlineData('\x1b', "word")] // ESC
    [InlineData('\x1f', "word")] // US
    public void ControlCharactersHandledSafely(char controlChar, string suffix)
    {
        // Control chars in range [\x00-\x1f] should not break sanitization
        var input = $"{controlChar}{suffix}";
        var result = FtsSanitizer.SanitizeFts5Query(input);
        Assert.NotNull(result);
        // Should produce a safe output (either quoted or with wildcard)
        Assert.True(result.EndsWith('*') || result.Contains('"'));
    }

    [Theory]
    [InlineData('"', "test")] // Double quote
    [InlineData('\'', "test")] // Single quote (not FTS5 special)
    [InlineData('`', "test")] // Backtick (not FTS5 special)
    public void QuoteCharactersHandled(char quoteChar, string suffix)
    {
        var input = $"{quoteChar}{suffix}";
        var result = FtsSanitizer.SanitizeFts5Query(input);
        Assert.NotNull(result);
        // Double quotes should cause the token to be quoted and escaped
        // Other quote types are not FTS5 special, so behavior varies
        Assert.True(result.Length > 0);
    }

    [Theory]
    [InlineData('(', "test")]
    [InlineData(')', "test")]
    [InlineData('[', "test")]
    [InlineData(']', "test")]
    [InlineData('{', "test")]
    [InlineData('}', "test")]
    public void BracketsAndParenthesesQuoted(char bracket, string suffix)
    {
        var input = $"{bracket}{suffix}";
        var result = FtsSanitizer.SanitizeFts5Query(input);
        Assert.NotNull(result);
        // Should be quoted because these are FTS5 special chars
        Assert.StartsWith("\"", result);
        Assert.Contains($"{bracket}", result);
    }

    [Theory]
    [InlineData("*", "\"*\"")]
    [InlineData("**", "\"**\"")]
    [InlineData("test*", "\"test*\"")]
    [InlineData("*test", "\"*test\"")]
    public void AsterisksAreQuoted(string input, string expected)
    {
        var result = FtsSanitizer.SanitizeFts5Query(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello world", "hello* world*")]
    [InlineData("foo bar baz", "foo* bar* baz*")]
    [InlineData("a b c", "a* b* c*")]
    public void MultipleNormalTokensGetWildcards(string input, string expected)
    {
        Assert.Equal(expected, FtsSanitizer.SanitizeFts5Query(input));
    }

    [Theory]
    [InlineData("test.py config.json", "\"test.py\" \"config.json\"")]
    [InlineData("key:value flag:set", "\"key:value\" \"flag:set\"")]
    [InlineData("path/to/file", "\"path/to/file\"")]
    public void MultipleSpecialTokensQuoted(string input, string expected)
    {
        Assert.Equal(expected, FtsSanitizer.SanitizeFts5Query(input));
    }

    [Fact]
    public void UnicodePreserved()
    {
        // Verify Unicode characters are preserved correctly
        Assert.Equal("café*", FtsSanitizer.SanitizeFts5Query("café"));
        Assert.Equal("日本語*", FtsSanitizer.SanitizeFts5Query("日本語"));
        Assert.Equal("émoji* 🚀*", FtsSanitizer.SanitizeFts5Query("émoji 🚀"));
    }

    [Theory]
    [InlineData("@mention", "\"@mention\"")]
    [InlineData("#hashtag", "\"#hashtag\"")]
    [InlineData("50%", "\"50%\"")]
    [InlineData("C++", "\"C++\"")]
    [InlineData("a&b", "\"a&b\"")]
    [InlineData("x=y", "\"x=y\"")]
    [InlineData("a|b", "\"a|b\"")]
    public void SpecialSymbolsQuoted(string input, string expected)
    {
        Assert.Equal(expected, FtsSanitizer.SanitizeFts5Query(input));
    }
}
