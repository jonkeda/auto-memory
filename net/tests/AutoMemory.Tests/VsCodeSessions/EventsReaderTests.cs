using System;
using System.IO;
using System.Linq;
using AutoMemory.Core.VsCodeSessions;
using Xunit;

namespace AutoMemory.Tests.VsCodeSessions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class EventsReaderTests
{
    private static string FixtureDir(string sessionId)
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "session-state", sessionId);
        if (!Directory.Exists(basePath))
            throw new DirectoryNotFoundException($"Fixture directory not found: {basePath}");
        return basePath;
    }

    [Fact]
    public void UserMessages_ReturnsFirst()
    {
        var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
        var msgs = EventsReader.UserMessages(dir).ToList();
        
        Assert.Single(msgs);
        Assert.Contains("login", msgs[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToolCalls_ReturnsCreateFile()
    {
        var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
        var calls = EventsReader.ToolCalls(dir).ToList();
        
        Assert.Contains(calls, c => c.Name == "create_file");
    }

    [Fact]
    public void TouchedFiles_ExtractsPath()
    {
        var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
        var files = EventsReader.TouchedFiles(dir).ToList();
        
        Assert.Contains(files, f => f.Contains("Login.tsx"));
    }

    [Fact]
    public void Read_MalformedLine_Skipped()
    {
        // Write a jsonl with one bad line in the middle
        var dir = WriteBadJsonl();
        try
        {
            var events = EventsReader.Read(dir).ToList();
            Assert.True(events.Count >= 1); // good lines still returned
            
            // Verify we got the valid events
            Assert.Contains(events, e => e.Type == "session.start");
            Assert.Contains(events, e => e.Type == "user.message");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Read_EmptyFile_ReturnsNothing()
    {
        var dir = WriteEmptyJsonl();
        try
        {
            var events = EventsReader.Read(dir).ToList();
            Assert.Empty(events);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Read_AllEvents_InOrder()
    {
        var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
        var events = EventsReader.Read(dir).ToList();
        
        Assert.Equal(5, events.Count);
        Assert.Equal("session.start", events[0].Type);
        Assert.Equal("user.message", events[1].Type);
        Assert.Equal("tool.execution_start", events[2].Type);
        Assert.Equal("tool.execution_complete", events[3].Type);
        Assert.Equal("session.shutdown", events[4].Type);
    }

    private static string WriteBadJsonl()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        var jsonlPath = Path.Combine(tempDir, "events.jsonl");
        File.WriteAllText(jsonlPath, """
            {"type":"session.start","data":{},"id":"evt-001","timestamp":"2026-01-01T00:00:00.000Z","parentId":null}
            this is not valid JSON and should be skipped
            {"type":"user.message","data":{"content":"test"},"id":"evt-002","timestamp":"2026-01-01T00:01:00.000Z","parentId":"evt-001"}
            """);
        
        return tempDir;
    }

    private static string WriteEmptyJsonl()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        var jsonlPath = Path.Combine(tempDir, "events.jsonl");
        File.WriteAllText(jsonlPath, "");
        
        return tempDir;
    }
}
