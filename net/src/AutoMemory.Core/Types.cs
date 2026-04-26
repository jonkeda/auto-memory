using System.Text.Json.Serialization;

#nullable enable

namespace AutoMemory.Core;

/// <summary>
/// JSON serialization context for all AutoMemory types. Enables source-generated serialization for AOT and trimmed builds.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(SessionRecord))]
[JsonSerializable(typeof(TurnRecord))]
[JsonSerializable(typeof(FileRecord))]
[JsonSerializable(typeof(CheckpointRecord))]
[JsonSerializable(typeof(HealthDimResult))]
[JsonSerializable(typeof(TelemetryEntry))]
[JsonSerializable(typeof(SessionListItem))]
[JsonSerializable(typeof(RecentFileItem))]
[JsonSerializable(typeof(ListResult))]
[JsonSerializable(typeof(FilesFileItem))]
[JsonSerializable(typeof(FilesResult))]
[JsonSerializable(typeof(CheckpointsCheckpointItem))]
[JsonSerializable(typeof(CheckpointsResult))]
[JsonSerializable(typeof(SearchResultItem))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(SessionRefRecord))]
[JsonSerializable(typeof(ShowTurnRecord))]
[JsonSerializable(typeof(ShowCheckpointRecord))]
[JsonSerializable(typeof(ShowSessionResult))]
[JsonSerializable(typeof(SchemaCheckJsonResult))]
[JsonSerializable(typeof(List<SessionListItem>))]
[JsonSerializable(typeof(List<RecentFileItem>))]
[JsonSerializable(typeof(List<FilesFileItem>))]
[JsonSerializable(typeof(List<CheckpointsCheckpointItem>))]
[JsonSerializable(typeof(List<SearchResultItem>))]
[JsonSerializable(typeof(List<ShowTurnRecord>))]
[JsonSerializable(typeof(List<FileRecord>))]
[JsonSerializable(typeof(List<SessionRefRecord>))]
[JsonSerializable(typeof(List<ShowCheckpointRecord>))]
[JsonSerializable(typeof(List<HealthDimResult>))]
[JsonSerializable(typeof(List<Dictionary<string, object?>>))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(List<TelemetryEntry>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(HealthResult))]
[JsonSerializable(typeof(Util.TelemetryJsonWrapper))]
[JsonSerializable(typeof(Util.TelemetryJsonEntry))]
public partial class AutoMemoryJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Represents a session record from the database.
/// </summary>
public record SessionRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    [JsonPropertyName("branch")]
    public required string Branch { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required string UpdatedAt { get; init; }

    [JsonPropertyName("turns_count")]
    public required int TurnsCount { get; init; }

    [JsonPropertyName("files_count")]
    public required int FilesCount { get; init; }
}

/// <summary>
/// Represents a turn record from the database.
/// </summary>
public record TurnRecord
{
    [JsonPropertyName("turn_index")]
    public required int TurnIndex { get; init; }

    [JsonPropertyName("user_message")]
    public required string UserMessage { get; init; }

    [JsonPropertyName("assistant_response")]
    public required string AssistantResponse { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }
}

/// <summary>
/// Represents a file record from the database.
/// </summary>
public record FileRecord
{
    [JsonPropertyName("file_path")]
    public required string FilePath { get; init; }

    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName("turn_index")]
    public required int TurnIndex { get; init; }
}

/// <summary>
/// Represents a checkpoint record from the database.
/// </summary>
public record CheckpointRecord
{
    [JsonPropertyName("checkpoint_number")]
    public required int CheckpointNumber { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("overview")]
    public required string Overview { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }
}

/// <summary>
/// Represents a health dimension result.
/// </summary>
public record HealthDimResult
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("score")]
    public required double Score { get; init; }

    [JsonPropertyName("detail")]
    public required string Detail { get; init; }
}

/// <summary>
/// Represents a full health dimension result with zone and hint (for health JSON output).
/// </summary>
public record HealthDimResultFull
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(2)]
    public required string Name { get; init; }

    [JsonPropertyName("unknown_entries")]
    [JsonPropertyOrder(-3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UnknownEntries { get; init; }

    [JsonPropertyName("meta_entries")]
    [JsonPropertyOrder(-2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MetaEntries { get; init; }

    [JsonPropertyName("scored_entries")]
    [JsonPropertyOrder(-1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ScoredEntries { get; init; }

    [JsonPropertyName("score")]
    [JsonPropertyOrder(0)]
    public double? Score { get; init; }

    [JsonPropertyName("zone")]
    [JsonPropertyOrder(1)]
    public required string Zone { get; init; }

    [JsonPropertyName("detail")]
    [JsonPropertyOrder(3)]
    public required string Detail { get; init; }

    [JsonPropertyName("hint")]
    [JsonPropertyOrder(4)]
    public string? Hint { get; init; }
}

/// <summary>
/// Represents the result of the health command.
/// </summary>
public record HealthResult
{
    [JsonPropertyName("overall_score")]
    public required double OverallScore { get; init; }

    [JsonPropertyName("dims")]
    public required List<Dictionary<string, object?>> Dims { get; init; }

    [JsonPropertyName("top_hints")]
    public required List<string> TopHints { get; init; }
}

/// <summary>
/// Represents a telemetry entry.
/// </summary>
public record TelemetryEntry
{
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("duration_ms")]
    public required int DurationMs { get; init; }

    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }
}

/// <summary>
/// Represents a session in the list command output.
/// </summary>
public record SessionListItem
{
    [JsonPropertyName("id_short")]
    public required string IdShort { get; init; }

    [JsonPropertyName("id_full")]
    public required string IdFull { get; init; }

    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    [JsonPropertyName("branch")]
    public required string Branch { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("date")]
    public required string? Date { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    [JsonPropertyName("turns_count")]
    public required int TurnsCount { get; init; }

    [JsonPropertyName("files_count")]
    public required int FilesCount { get; init; }
}

/// <summary>
/// Represents a recent file in the list command output.
/// </summary>
public record RecentFileItem
{
    [JsonPropertyName("file_path")]
    public required string FilePath { get; init; }

    [JsonPropertyName("full_path")]
    public required string FullPath { get; init; }

    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Represents the result of the list command.
/// </summary>
public record ListResult
{
    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("sessions")]
    public required List<SessionListItem> Sessions { get; init; }

    [JsonPropertyName("recent_files")]
    public required List<RecentFileItem> RecentFiles { get; init; }
}

/// <summary>
/// Represents a file item in the files command output.
/// </summary>
public record FilesFileItem
{
    [JsonPropertyName("file_path")]
    public required string FilePath { get; init; }

    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("session_summary")]
    public string? SessionSummary { get; init; }
}

/// <summary>
/// Represents the result of the files command.
/// </summary>
public record FilesResult
{
    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("files")]
    public required List<FilesFileItem> Files { get; init; }
}

/// <summary>
/// Represents a checkpoint item in the checkpoints command output.
/// </summary>
public record CheckpointsCheckpointItem
{
    [JsonPropertyName("checkpoint_number")]
    public required int CheckpointNumber { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("overview")]
    public required string Overview { get; init; }

    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("session_summary")]
    public required string? SessionSummary { get; init; }
}

/// <summary>
/// Represents the result of the checkpoints command.
/// </summary>
public record CheckpointsResult
{
    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("checkpoints")]
    public required List<CheckpointsCheckpointItem> Checkpoints { get; init; }
}

/// <summary>
/// Represents a search result item.
/// </summary>
public record SearchResultItem
{
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("session_id_full")]
    public required string SessionIdFull { get; init; }

    [JsonPropertyName("source_type")]
    public required string SourceType { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("excerpt")]
    public required string Excerpt { get; init; }
}

/// <summary>
/// Represents the result of the search command.
/// </summary>
public record SearchResult
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }

    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("count")]
    public required int Count { get; init; }

    [JsonPropertyName("results")]
    public required List<SearchResultItem> Results { get; init; }

    [JsonPropertyName("warning")]
    public string? Warning { get; init; }
}

/// <summary>
/// Represents a session reference record.
/// </summary>
public record SessionRefRecord
{
    [JsonPropertyName("ref_type")]
    public required string RefType { get; init; }

    [JsonPropertyName("ref_value")]
    public required string RefValue { get; init; }

    [JsonPropertyName("turn_index")]
    public required int TurnIndex { get; init; }
}

/// <summary>
/// Represents a turn in the show command output.
/// </summary>
public record ShowTurnRecord
{
    [JsonPropertyName("idx")]
    public required int Idx { get; init; }

    [JsonPropertyName("user")]
    public required string User { get; init; }

    [JsonPropertyName("assistant")]
    public required string Assistant { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }
}

/// <summary>
/// Represents a checkpoint in the show command output.
/// </summary>
public record ShowCheckpointRecord
{
    [JsonPropertyName("n")]
    public required int N { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("overview")]
    public required string Overview { get; init; }
}

/// <summary>
/// Represents the result of the show command.
/// </summary>
public record ShowSessionResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    [JsonPropertyName("branch")]
    public required string Branch { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    [JsonPropertyName("turns_count")]
    public required int TurnsCount { get; init; }

    [JsonPropertyName("turns")]
    public required List<ShowTurnRecord> Turns { get; init; }

    [JsonPropertyName("files")]
    public required List<FileRecord> Files { get; init; }

    [JsonPropertyName("refs")]
    public required List<SessionRefRecord> Refs { get; init; }

    [JsonPropertyName("checkpoints")]
    public required List<ShowCheckpointRecord> Checkpoints { get; init; }
}

/// <summary>
/// Schema-check command JSON result.
/// </summary>
public record SchemaCheckJsonResult
{
    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }

    [JsonPropertyName("problems")]
    public required List<string> Problems { get; init; }
}
