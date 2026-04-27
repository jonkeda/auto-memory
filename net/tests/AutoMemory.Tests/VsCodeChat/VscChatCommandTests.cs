using System;
using System.IO;
using System.Text.Json;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using AutoMemory.Core.VsCodeChat;

namespace AutoMemory.Tests.VsCodeChat;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class VscChatCommandTests
{
    private static string FixtureRoot => Path.Combine(
        AppContext.BaseDirectory,
        "fixtures", "chat-transcripts");

    private static ChatSessionStore CreateFixtureStore()
    {
        var ws1 = new ChatWorkspace
        {
            Hash = "ws-hash-0001",
            WorkspacePath = "e:/repos/Owner/RepoA",
            TranscriptDir = Path.Combine(FixtureRoot, "ws-hash-0001", "GitHub.copilot-chat", "transcripts")
        };

        var ws2 = new ChatWorkspace
        {
            Hash = "ws-hash-0002",
            WorkspacePath = "e:/repos/Owner/RepoB",
            TranscriptDir = Path.Combine(FixtureRoot, "ws-hash-0002", "GitHub.copilot-chat", "transcripts")
        };

        return ChatSessionStore.FromWorkspaces(new[] { ws1, ws2 });
    }

    [Fact]
    public void VscChatListCommand_OutputsJsonWithSourceField()
    {
        // Arrange
        var args = new ParsedArgs();
        args.SetOption("json", "true");
        args.SetOption("limit", "10");
        
        var originalOut = Console.Out;
        using var sw = new StringWriter();

        // Act
        try
        {
            Console.SetOut(sw);
            var exitCode = VscChatListCommand.Run(args);

            // Assert
            var output = sw.ToString();
            var json = JsonDocument.Parse(output);
            Assert.True(json.RootElement.TryGetProperty("source", out var source));
            Assert.Equal("vscode-chat", source.GetString());
            Assert.True(json.RootElement.TryGetProperty("sessions", out _));
            Assert.True(json.RootElement.TryGetProperty("total", out _));
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void VscChatShowCommand_UnknownId_Returns1()
    {
        // Arrange
        var args = new ParsedArgs();
        args.SetPositional(0, "nonexistent-session-id-xyz");
        
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var swOut = new StringWriter();
        using var swErr = new StringWriter();

        // Act
        try
        {
            Console.SetOut(swOut);
            Console.SetError(swErr);
            var exitCode = VscChatShowCommand.Run(args);

            // Assert
            Assert.Equal(1, exitCode);
            var errOutput = swErr.ToString();
            Assert.Contains("no session", errOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void VscChatSearchCommand_OutputsJsonWithSourceField()
    {
        // Arrange
        var args = new ParsedArgs();
        args.SetPositional(0, "test");
        args.SetOption("json", "true");
        args.SetOption("limit", "10");
        
        var originalOut = Console.Out;
        using var sw = new StringWriter();

        // Act
        try
        {
            Console.SetOut(sw);
            var exitCode = VscChatSearchCommand.Run(args);

            // Assert
            var output = sw.ToString();
            var json = JsonDocument.Parse(output);
            Assert.True(json.RootElement.TryGetProperty("source", out var source));
            Assert.Equal("vscode-chat", source.GetString());
            Assert.True(json.RootElement.TryGetProperty("sessions", out _));
            Assert.True(json.RootElement.TryGetProperty("query", out _));
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void VscChatSearchCommand_EmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var args = new ParsedArgs();
        args.SetOption("json", "true");
        
        var originalOut = Console.Out;
        using var sw = new StringWriter();

        // Act
        try
        {
            Console.SetOut(sw);
            var exitCode = VscChatSearchCommand.Run(args);

            // Assert
            var output = sw.ToString();
            var json = JsonDocument.Parse(output);
            Assert.True(json.RootElement.TryGetProperty("sessions", out var sessions));
            Assert.Equal(JsonValueKind.Array, sessions.ValueKind);
            Assert.Equal(0, sessions.GetArrayLength());
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void VscChatShowCommand_MissingId_Returns2()
    {
        // Arrange
        var args = new ParsedArgs();
        
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var swOut = new StringWriter();
        using var swErr = new StringWriter();

        // Act
        try
        {
            Console.SetOut(swOut);
            Console.SetError(swErr);
            var exitCode = VscChatShowCommand.Run(args);

            // Assert
            Assert.Equal(2, exitCode);
            var errOutput = swErr.ToString();
            Assert.Contains("required", errOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }
}

