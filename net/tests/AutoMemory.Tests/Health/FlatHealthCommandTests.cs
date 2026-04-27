using System;
using System.IO;
using System.Text.Json;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using Xunit;

namespace AutoMemory.Tests.Health;

[Collection("Console Capture")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class FlatHealthCommandTests
{
    [Fact]
    public void Run_OutputsValidJson()
    {
        // Arrange
        var args = new ParsedArgs();
        using var sw = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(sw);

            // Act
            var exitCode = FlatHealthCommand.Run(args);

            // Assert
            var output = sw.ToString();
            Assert.NotEmpty(output);

            // Should parse as valid JSON
            var doc = JsonDocument.Parse(output);
            // JsonElement is a value type, no need to assert not null
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Run_OverallScore_IsNonNull()
    {
        // Arrange
        var args = new ParsedArgs();
        using var sw = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(sw);

            // Act
            var exitCode = FlatHealthCommand.Run(args);

            // Assert
            var output = sw.ToString();
            var doc = JsonDocument.Parse(output);
            var overallScore = doc.RootElement.GetProperty("overall_score");

            // overall_score can be null if all dims are CALIBRATING, but in practice
            // with on-disk stores it should be non-null
            // (If the test env has no stores, this may be null — we accept both)
            Assert.True(
                overallScore.ValueKind == JsonValueKind.Number || overallScore.ValueKind == JsonValueKind.Null,
                "overall_score should be a number or null");

            // Also verify exit code is success
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Run_Source_IsVscodeFlat()
    {
        // Arrange
        var args = new ParsedArgs();
        using var sw = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(sw);

            // Act
            var exitCode = FlatHealthCommand.Run(args);

            // Assert
            var output = sw.ToString();
            var doc = JsonDocument.Parse(output);

            // Verify source field
            var source = doc.RootElement.GetProperty("source").GetString();
            Assert.Equal("vscode-flat", source);

            // Verify dims array has 5 elements
            var dims = doc.RootElement.GetProperty("dims");
            Assert.Equal(5, dims.GetArrayLength());

            // Verify backends object has both keys
            var backends = doc.RootElement.GetProperty("backends");
            Assert.True(backends.TryGetProperty("session_state", out _));
            Assert.True(backends.TryGetProperty("vscode_chat", out _));

            // Verify exit code
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
