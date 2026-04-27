namespace AutoMemory.Core.VsCodeSessions;

/// <summary>
/// Represents a single VS Code Copilot session loaded from
/// ~/.copilot/session-state/&lt;uuid&gt;/workspace.yaml.
/// </summary>
public sealed record VsCodeSession
{
    public required string Id            { get; init; }
    public string?         Repository    { get; init; }  // "owner/repo" or null
    public string?         Branch        { get; init; }
    public string?         Cwd           { get; init; }
    public string?         Summary       { get; init; }
    public DateTimeOffset  CreatedAt     { get; init; }
    public DateTimeOffset  UpdatedAt     { get; init; }

    /// <summary>Absolute path to the session directory.</summary>
    public required string DirectoryPath { get; init; }
}
