using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Xunit;

namespace AutoMemory.Parity.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method names")]
[Trait("Category", "Parity")]
public sealed class CaseFileParserTests
{
    [Fact]
    public void ParseCaseFile_SimpleCase_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"# Comment line
ARGV: list --limit 5 --json
EXIT: 0
TIER: 1
STDERR_REGEX: ^$
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            Assert.Equal("list --limit 5 --json", testCase.Argv);
            Assert.Equal(0, testCase.ExpectedExitCode);
            Assert.Equal(1, testCase.Tier);
            Assert.Equal("^$", testCase.StderrRegex);
            Assert.Null(testCase.StdoutHash);
            Assert.Null(testCase.StdoutContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseCaseFile_WithStdoutHash_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"ARGV: list --limit 5 --json
EXIT: 0
TIER: 1
STDERR_REGEX: ^$
STDOUT_HASH: abc123def456
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            Assert.Equal("abc123def456", testCase.StdoutHash);
            Assert.Null(testCase.StdoutContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseCaseFile_WithInlineStdout_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"ARGV: schema-check
EXIT: 0
TIER: 1
STDOUT:
schema OK
END_STDOUT
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            Assert.Equal("schema OK", testCase.StdoutContent);
            Assert.Null(testCase.StdoutHash);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseCaseFile_WithMultiLineStdout_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"ARGV: health --format text
EXIT: 0
TIER: 2
STDOUT:
Health Check Results
====================
Status: PASS
END_STDOUT
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            // Normalize line endings for cross-platform compatibility
            var expected = "Health Check Results\n====================\nStatus: PASS".Replace("\n", Environment.NewLine);
            Assert.Equal(expected, testCase.StdoutContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseCaseFile_WithEmptyLines_IgnoresThem()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"
ARGV: list

EXIT: 0

TIER: 1
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            Assert.Equal("list", testCase.Argv);
            Assert.Equal(0, testCase.ExpectedExitCode);
            Assert.Equal(1, testCase.Tier);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseCaseFile_DefaultValues_AppliedWhenMissing()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"ARGV: list
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            Assert.Equal("list", testCase.Argv);
            Assert.Equal(0, testCase.ExpectedExitCode); // Default
            Assert.Equal(1, testCase.Tier); // Default
            Assert.Null(testCase.StderrRegex);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseCaseFile_CaseInsensitiveKeys_ParsesCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"argv: list
exit: 2
tier: 3
");

            // Act
            var testCase = AutoMemory.Parity.ParityRunner.ParseCaseFile(tempFile);

            // Assert
            Assert.Equal("list", testCase.Argv);
            Assert.Equal(2, testCase.ExpectedExitCode);
            Assert.Equal(3, testCase.Tier);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
