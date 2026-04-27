using System.Text.Json;

namespace AutoMemory.Core.VsCodeChat;

public sealed record ChatEvent
{
    public required string          Type      { get; init; }
    public required string          Id        { get; init; }
    public          DateTimeOffset  Timestamp { get; init; }
    public          string?         ParentId  { get; init; }
    public          JsonElement     Data      { get; init; }
}
