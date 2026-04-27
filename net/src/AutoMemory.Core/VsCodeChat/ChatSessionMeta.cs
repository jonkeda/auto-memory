namespace AutoMemory.Core.VsCodeChat;

public sealed record ChatSessionMeta(
    string SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? Summary,
    int TurnCount,
    int ToolCount
);
