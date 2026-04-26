using System.Text.Json;
using AutoMemory.Core;
using Xunit;

namespace AutoMemory.Tests;

/// <summary>
/// Tests for Types.cs record serialization.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public class TypesTests
{
    [Fact]
    public void SessionRecord_SerializesToSnakeCase()
    {
        var record = new SessionRecord
        {
            Id = "test-id",
            Repository = "test-repo",
            Branch = "main",
            Summary = "test summary",
            CreatedAt = "2026-01-01T00:00:00Z",
            UpdatedAt = "2026-01-02T00:00:00Z",
            TurnsCount = 5,
            FilesCount = 10
        };

        string json = JsonSerializer.Serialize(record);

        Assert.Contains("\"id\":", json);
        Assert.Contains("\"repository\":", json);
        Assert.Contains("\"branch\":", json);
        Assert.Contains("\"summary\":", json);
        Assert.Contains("\"created_at\":", json);
        Assert.Contains("\"updated_at\":", json);
        Assert.Contains("\"turns_count\":", json);
        Assert.Contains("\"files_count\":", json);
    }

    [Fact]
    public void TurnRecord_SerializesToSnakeCase()
    {
        var record = new TurnRecord
        {
            TurnIndex = 1,
            UserMessage = "user msg",
            AssistantResponse = "assistant msg",
            Timestamp = "2026-01-01T00:00:00Z"
        };

        string json = JsonSerializer.Serialize(record);

        Assert.Contains("\"turn_index\":", json);
        Assert.Contains("\"user_message\":", json);
        Assert.Contains("\"assistant_response\":", json);
        Assert.Contains("\"timestamp\":", json);
    }

    [Fact]
    public void FileRecord_SerializesToSnakeCase()
    {
        var record = new FileRecord
        {
            FilePath = "/path/to/file.cs",
            ToolName = "read_file",
            TurnIndex = 1
        };

        string json = JsonSerializer.Serialize(record);

        Assert.Contains("\"file_path\":", json);
        Assert.Contains("\"tool_name\":", json);
        Assert.Contains("\"turn_index\":", json);
    }

    [Fact]
    public void CheckpointRecord_SerializesToSnakeCase()
    {
        var record = new CheckpointRecord
        {
            CheckpointNumber = 1,
            Title = "Test Checkpoint",
            Overview = "Overview text",
            CreatedAt = "2026-01-01T00:00:00Z"
        };

        string json = JsonSerializer.Serialize(record);

        Assert.Contains("\"checkpoint_number\":", json);
        Assert.Contains("\"title\":", json);
        Assert.Contains("\"overview\":", json);
        Assert.Contains("\"created_at\":", json);
    }

    [Fact]
    public void HealthDimResult_SerializesToSnakeCase()
    {
        var record = new HealthDimResult
        {
            Name = "dim_test",
            Score = 0.95,
            Detail = "All checks passed"
        };

        string json = JsonSerializer.Serialize(record);

        Assert.Contains("\"name\":", json);
        Assert.Contains("\"score\":", json);
        Assert.Contains("\"detail\":", json);
    }

    [Fact]
    public void TelemetryEntry_SerializesToSnakeCase()
    {
        var record = new TelemetryEntry
        {
            Command = "search",
            Timestamp = "2026-01-01T00:00:00Z",
            DurationMs = 150,
            Ok = true
        };

        string json = JsonSerializer.Serialize(record);

        Assert.Contains("\"command\":", json);
        Assert.Contains("\"timestamp\":", json);
        Assert.Contains("\"duration_ms\":", json);
        Assert.Contains("\"ok\":", json);
    }

    [Fact]
    public void SessionRecord_DeserializesFromSnakeCase()
    {
        string json = """
        {
            "id": "test-id",
            "repository": "test-repo",
            "branch": "main",
            "summary": "test summary",
            "created_at": "2026-01-01T00:00:00Z",
            "updated_at": "2026-01-02T00:00:00Z",
            "turns_count": 5,
            "files_count": 10
        }
        """;

        var record = JsonSerializer.Deserialize<SessionRecord>(json);

        Assert.NotNull(record);
        Assert.Equal("test-id", record.Id);
        Assert.Equal("test-repo", record.Repository);
        Assert.Equal("main", record.Branch);
        Assert.Equal("test summary", record.Summary);
        Assert.Equal("2026-01-01T00:00:00Z", record.CreatedAt);
        Assert.Equal("2026-01-02T00:00:00Z", record.UpdatedAt);
        Assert.Equal(5, record.TurnsCount);
        Assert.Equal(10, record.FilesCount);
    }

    [Fact]
    public void TelemetryEntry_DeserializesFromSnakeCase()
    {
        string json = """
        {
            "command": "search",
            "timestamp": "2026-01-01T00:00:00Z",
            "duration_ms": 150,
            "ok": true
        }
        """;

        var record = JsonSerializer.Deserialize<TelemetryEntry>(json);

        Assert.NotNull(record);
        Assert.Equal("search", record.Command);
        Assert.Equal("2026-01-01T00:00:00Z", record.Timestamp);
        Assert.Equal(150, record.DurationMs);
        Assert.True(record.Ok);
    }
}
