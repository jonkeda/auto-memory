namespace AutoMemory.Core.VsCodeChat;

/// <summary>One workspaceStorage hash folder that has Copilot Chat data.</summary>
public sealed record ChatWorkspace
{
    public required string Hash          { get; init; }
    public          string? WorkspacePath { get; init; }
    public required string TranscriptDir  { get; init; }
}
