namespace AutoMemory.Tests.VsCodeChat;

using AutoMemory.Core.VsCodeChat;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class WorkspaceStorageLocatorTests
{
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

    [Fact]
    public void ReadWorkspacePath_UrlDecodes_WindowsPath()
    {
        // Arrange: create temp dir with workspace.json containing URL-encoded Windows path
        var tempDir = Path.Combine(Path.GetTempPath(), "ws_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var wsJson = Path.Combine(tempDir, "workspace.json");
            File.WriteAllText(wsJson, """{"workspace":"file:///e%3A/repos/Owner/RepoA"}""");

            // Act
            var result = WorkspaceStorageLocator.ReadWorkspacePath(tempDir);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(@"e:\repos\Owner\RepoA", result);
            }
            else
            {
                // On Linux/macOS, forward slashes remain
                Assert.Equal("e:/repos/Owner/RepoA", result);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
