using AutoMemory.Core.VsCodeChat;

namespace AutoMemory.Tests.VsCodeChat;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class TranscriptReaderTests
{
    private static string FixturePath => Path.Combine(
        AppContext.BaseDirectory,
        "fixtures", "chat-transcripts", "abc12345-0001", "transcript.jsonl");

    private static readonly string[] s_malformedTestLines = new[]
    {
        """{"type":"session.start","data":{},"id":"e1","timestamp":"2026-04-20T10:00:00.000Z","parentId":null}""",
        """THIS IS NOT JSON""",
        """{"type":"user.message","data":{"content":"test"},"id":"e2","timestamp":"2026-04-20T10:00:01.000Z","parentId":"e1"}"""
    };

    [Fact]
    public void Read_AllEvents_Parsed()
    {
        // Act
        var events = TranscriptReader.Read(FixturePath).ToList();

        // Assert
        Assert.Equal(8, events.Count);
        Assert.Equal("session.start", events[0].Type);
        Assert.Equal("event-001", events[0].Id);
        Assert.Equal("user.message", events[1].Type);
        Assert.Equal("user.message", events[7].Type);
    }

    [Fact]
    public void ReadMeta_SessionId_MatchesStartEvent()
    {
        // Act
        var meta = TranscriptReader.ReadMeta(FixturePath);

        // Assert
        Assert.Equal("abc12345-0001-0000-0000-000000000000", meta.SessionId);
    }

    [Fact]
    public void ReadMeta_CreatedAt_MatchesStartTime()
    {
        // Act
        var meta = TranscriptReader.ReadMeta(FixturePath);

        // Assert
        var expected = new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, meta.CreatedAt);
    }

    [Fact]
    public void ReadMeta_UpdatedAt_IsLastEventTimestamp()
    {
        // Act
        var meta = TranscriptReader.ReadMeta(FixturePath);

        // Assert
        var expected = new DateTimeOffset(2026, 4, 20, 10, 1, 0, TimeSpan.Zero);
        Assert.Equal(expected, meta.UpdatedAt);
    }

    [Fact]
    public void ReadMeta_Summary_IsFirstUserMessageTruncated()
    {
        // Act
        var meta = TranscriptReader.ReadMeta(FixturePath);

        // Assert
        Assert.Equal("How do I add a NuGet package?", meta.Summary);
    }

    [Fact]
    public void ReadMeta_TurnCount_Is2()
    {
        // Act
        var meta = TranscriptReader.ReadMeta(FixturePath);

        // Assert
        Assert.Equal(2, meta.TurnCount);
    }

    [Fact]
    public void ReadMeta_ToolCount_Is1()
    {
        // Act
        var meta = TranscriptReader.ReadMeta(FixturePath);

        // Assert
        Assert.Equal(1, meta.ToolCount);
    }

    [Fact]
    public void Read_MalformedLine_IsSkipped()
    {
        // Arrange - create a temp file with a malformed line
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.jsonl");
        try
        {
            File.WriteAllLines(tempPath, s_malformedTestLines);

            // Act
            var events = TranscriptReader.Read(tempPath).ToList();

            // Assert
            Assert.Equal(2, events.Count); // Malformed line skipped
            Assert.Equal("session.start", events[0].Type);
            Assert.Equal("user.message", events[1].Type);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
