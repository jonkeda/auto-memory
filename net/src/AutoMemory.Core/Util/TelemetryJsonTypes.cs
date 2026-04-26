using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoMemory.Core.Util;

/// <summary>
/// Wrapper for telemetry JSON file structure.
/// </summary>
public sealed class TelemetryJsonWrapper
{
    [JsonPropertyName("entries")]
    public List<TelemetryJsonEntry> Entries { get; set; } = new();
}

/// <summary>
/// Telemetry entry for health dimension analysis.
/// Contains fields needed by DimConcurrency and DimDisclosure.
/// </summary>
public sealed class TelemetryJsonEntry
{
    [JsonPropertyName("ts")]
    public string? Ts { get; set; }

    [JsonPropertyName("cmd")]
    public string? Cmd { get; set; }

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("busy_hits")]
    public int BusyHits { get; set; }

    [JsonPropertyName("attempts")]
    public int Attempts { get; set; } = 1;

    [JsonPropertyName("tier")]
    public int? Tier { get; set; }

    [JsonPropertyName("query_hash")]
    public string? QueryHash { get; set; }
}
