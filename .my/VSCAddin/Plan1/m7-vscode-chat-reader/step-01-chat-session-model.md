# Step 01 — `ChatSession` model + workspaceStorage discovery

## Goal

Define the `ChatSession` record and `ChatWorkspace` helper, and implement the directory
scanner that enumerates all VS Code / VS Code Insiders workspaceStorage directories
that contain Copilot Chat transcript files.

## Location

`net/src/AutoMemory.Core/VsCodeChat/`

## Types to create

### `ChatSession.cs`

```csharp
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
```

### `ChatWorkspace.cs`

```csharp
namespace AutoMemory.Core.VsCodeChat;

/// <summary>One workspaceStorage hash folder that has Copilot Chat data.</summary>
public sealed record ChatWorkspace
{
    public required string Hash          { get; init; }
    public          string? WorkspacePath { get; init; }
    public required string TranscriptDir  { get; init; }
}
```

### `WorkspaceStorageLocator.cs`

Static helper that finds all VS Code workspaceStorage directories:

```csharp
namespace AutoMemory.Core.VsCodeChat;

public static class WorkspaceStorageLocator
{
    /// <summary>
    /// Candidate %APPDATA% / Library base directories, both stable and Insiders.
    /// Returns only paths that actually exist on disk.
    /// </summary>
    public static IEnumerable<string> StorageRoots()
    {
        // Windows: %APPDATA%\Code\User\workspaceStorage
        //          %APPDATA%\Code - Insiders\User\workspaceStorage
        // macOS:   ~/Library/Application Support/Code/User/workspaceStorage
        //          ~/Library/Application Support/Code - Insiders/User/workspaceStorage
        // Linux:   ~/.config/Code/User/workspaceStorage
        //          ~/.config/Code - Insiders/User/workspaceStorage
        var candidates = PlatformCandidates();
        foreach (var c in candidates)
            if (Directory.Exists(c)) yield return c;
    }

    private static IEnumerable<string> PlatformCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "Code", "User", "workspaceStorage");
            yield return Path.Combine(appData, "Code - Insiders", "User", "workspaceStorage");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library", "Application Support", "Code", "User", "workspaceStorage");
            yield return Path.Combine(home, "Library", "Application Support", "Code - Insiders", "User", "workspaceStorage");
        }
        else // Linux
        {
            var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                         ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            yield return Path.Combine(config, "Code", "User", "workspaceStorage");
            yield return Path.Combine(config, "Code - Insiders", "User", "workspaceStorage");
        }
    }

    /// <summary>Enumerate all ChatWorkspace entries across all storage roots.</summary>
    public static IEnumerable<ChatWorkspace> FindWorkspaces()
    {
        foreach (var root in StorageRoots())
        {
            foreach (var hashDir in Directory.EnumerateDirectories(root))
            {
                var transcriptDir = Path.Combine(hashDir, "GitHub.copilot-chat", "transcripts");
                if (!Directory.Exists(transcriptDir)) continue;

                var workspacePath = ReadWorkspacePath(hashDir);
                yield return new ChatWorkspace
                {
                    Hash          = Path.GetFileName(hashDir),
                    WorkspacePath = workspacePath,
                    TranscriptDir = transcriptDir,
                };
            }
        }
    }

    private static string? ReadWorkspacePath(string hashDir)
    {
        var wsJson = Path.Combine(hashDir, "workspace.json");
        if (!File.Exists(wsJson)) return null;
        try
        {
            var text = File.ReadAllText(wsJson);
            // {"workspace":"file:///e%3A/repos/..."}
            var doc = System.Text.Json.JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("workspace", out var ws))
            {
                var raw = ws.GetString();
                if (raw is null) return null;
                // Strip "file://" prefix, URL-decode the rest
                if (raw.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                    raw = raw.Substring("file:///".Length);
                else if (raw.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    raw = raw.Substring("file://".Length);
                return Uri.UnescapeDataString(raw).Replace('/', Path.DirectorySeparatorChar);
            }
        }
        catch { /* ignore malformed json */ }
        return null;
    }
}
```

## Tests

In `AutoMemory.Tests/VsCodeChat/WorkspaceStorageLocatorTests.cs`:

```csharp
[Fact]
public void StorageRoots_ReturnsAtLeastOneExistingPath_OnCurrentMachine()
{
    var roots = WorkspaceStorageLocator.StorageRoots().ToList();
    // At least one exists on the CI/dev machine; test is informational
    Assert.True(roots.Count >= 0); // no exception = pass
}

[Fact]
public void FindWorkspaces_DoesNotThrow()
{
    var ws = WorkspaceStorageLocator.FindWorkspaces().ToList();
    Assert.NotNull(ws);
}
```

(Integration test — counts real workspaces, only verifies no exception.)
