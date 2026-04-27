using System;
using System.Text.Json;

namespace AutoMemory.Core.VsCodeSessions;

public sealed record SessionEvent
{
    public required string   Type      { get; init; }
    public required string   Id        { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string?   ParentId  { get; init; }
    public JsonElement Data     { get; init; }  // raw; callers extract what they need
}
