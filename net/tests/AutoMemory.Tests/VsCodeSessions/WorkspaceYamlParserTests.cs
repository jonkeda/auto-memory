using System;
using System.IO;
using AutoMemory.Core.VsCodeSessions;
using Xunit;

namespace AutoMemory.Tests.VsCodeSessions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class WorkspaceYamlParserTests
{
    private static string FixtureDir(string sessionId)
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "session-state", sessionId);
        if (!Directory.Exists(basePath))
            throw new DirectoryNotFoundException($"Fixture directory not found: {basePath}");
        return basePath;
    }

    [Fact]
    public void Parse_RealFields_Correct()
    {
        var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000001");
        var s = WorkspaceYamlParser.Parse(dir);
        
        Assert.Equal("a1b2c3d4-0000-0000-0000-000000000001", s.Id);
        Assert.Equal("owner/my-project", s.Repository);
        Assert.Equal("main", s.Branch);
        Assert.Equal(2026, s.CreatedAt.Year);
        Assert.Equal(1, s.CreatedAt.Month);
        Assert.Equal(15, s.CreatedAt.Day);
        Assert.Contains("login", s.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_MissingOptionalFields_DoesNotThrow()
    {
        // workspace.yaml with only id and created_at
        var dir = WriteMinimalYaml();
        try
        {
            var s = WorkspaceYamlParser.Parse(dir);
            Assert.NotNull(s.Id);
            Assert.Null(s.Repository);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Parse_Session2_CorrectFields()
    {
        var dir = FixtureDir("a1b2c3d4-0000-0000-0000-000000000002");
        var s = WorkspaceYamlParser.Parse(dir);
        
        Assert.Equal("a1b2c3d4-0000-0000-0000-000000000002", s.Id);
        Assert.Equal("owner/other-project", s.Repository);
        Assert.Equal("feature/api", s.Branch);
        Assert.Equal(2026, s.CreatedAt.Year);
        Assert.Equal(1, s.CreatedAt.Month);
        Assert.Equal(10, s.CreatedAt.Day);
        Assert.Contains("REST", s.Summary, StringComparison.Ordinal);
    }

    private static string WriteMinimalYaml()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        var yamlPath = Path.Combine(tempDir, "workspace.yaml");
        File.WriteAllText(yamlPath, """
            id: minimal-test-id
            created_at: 2026-01-01T00:00:00.000Z
            updated_at: 2026-01-01T00:00:00.000Z
            """);
        
        return tempDir;
    }
}
