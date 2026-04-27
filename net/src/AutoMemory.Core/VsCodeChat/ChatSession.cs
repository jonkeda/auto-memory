namespace AutoMemory.Core.VsCodeChat;

/// <summary>One VS Code Copilot Chat session (transcript file).</summary>
public sealed record ChatSession
{
    public required string Id             { get; init; }  // UUID stem of the transcript filename
    public required string WorkspaceHash  { get; init; }  // workspaceStorage folder name
    public          string? WorkspacePath { get; init; }  // URL-decoded "workspace" field from workspace.json
    public          string? Repository    { get; init; }  // derived owner/repo string
    public required DateTimeOffset CreatedAt  { get; init; }
    public          DateTimeOffset? UpdatedAt { get; init; }
    public          string? Summary      { get; init; }  // first user.message, ≤200 chars
    public          int     TurnCount    { get; init; }
    public          int     ToolCount    { get; init; }
    public required string TranscriptPath { get; init; } // absolute path to the .jsonl file
}
