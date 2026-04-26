using System;
using System.Collections.Generic;
using System.IO;
using AutoMemory.Core;
using AutoMemory.Core.Health;
using Microsoft.Data.Sqlite;
using Xunit;

#pragma warning disable CA1707 // Test method names use underscores for readability

namespace AutoMemory.Tests.Health;

public sealed class DimFreshnessTests : IDisposable
{
    private readonly string _tempDbPath;

    public DimFreshnessTests()
    {
        // Create a temporary DB file
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test-dimfreshness-{Guid.NewGuid()}.db");
        File.WriteAllText(_tempDbPath, "dummy");
    }

    public void Dispose()
    {
        // Clean up temp file
        if (File.Exists(_tempDbPath))
        {
            File.Delete(_tempDbPath);
        }
    }

    [Theory]
    [InlineData(0, "GREEN")]       // 0 hours - freshest possible
    [InlineData(12, "GREEN")]      // 12 hours - well within green
    [InlineData(23, "GREEN")]      // 23 hours - threshold - 1
    [InlineData(23.9, "GREEN")]    // Just before green threshold
    [InlineData(24.1, "AMBER")]    // Just after green threshold
    [InlineData(25, "AMBER")]      // 25 hours - threshold + 1
    [InlineData(48, "AMBER")]      // 48 hours - mid amber
    [InlineData(71, "AMBER")]      // 71 hours - amber threshold - 1
    [InlineData(71.9, "AMBER")]    // Just before amber threshold
    [InlineData(72.1, "RED")]      // Just after amber threshold
    [InlineData(73, "RED")]        // 73 hours - amber threshold + 1
    [InlineData(168, "RED")]       // 168 hours (7 days) - well into red
    public void Run_WithVariousAges_ReturnsExpectedZone(double ageHours, string expectedZone)
    {
        // Arrange
        var targetTime = DateTime.UtcNow.AddHours(-ageHours);
        File.SetLastWriteTimeUtc(_tempDbPath, targetTime);

        var dim = new DimFreshness();

        // Act
        var result = dim.RunInternal(_tempDbPath);

        // Assert
        Assert.Equal("DB Freshness", result.Name);
        Assert.NotNull(result.Score);
        Assert.True(result.Detail.ContainsKey("zone"));
        Assert.Equal(expectedZone, result.Detail["zone"]);
        Assert.True(result.Detail.ContainsKey("detail"));
        Assert.True(result.Detail.ContainsKey("hint"));
    }

    [Fact]
    public void Run_WithFreshDb_ReturnsHighScore()
    {
        // Arrange
        File.SetLastWriteTimeUtc(_tempDbPath, DateTime.UtcNow);
        var dim = new DimFreshness();

        // Act
        var result = dim.RunInternal(_tempDbPath);

        // Assert
        Assert.Equal("DB Freshness", result.Name);
        Assert.NotNull(result.Score);
        Assert.True(result.Score >= 7.0); // Fresh DB should score high
        Assert.Equal("GREEN", result.Detail["zone"]);
    }

    [Fact]
    public void Run_WithVeryOldDb_ReturnsLowScore()
    {
        // Arrange
        File.SetLastWriteTimeUtc(_tempDbPath, DateTime.UtcNow.AddDays(-30));
        var dim = new DimFreshness();

        // Act
        var result = dim.RunInternal(_tempDbPath);

        // Assert
        Assert.Equal("DB Freshness", result.Name);
        Assert.NotNull(result.Score);
        Assert.True(result.Score < 4.0); // Very old DB should score low
        Assert.Equal("RED", result.Detail["zone"]);
    }

    [Fact]
    public void Run_WithMissingDb_ReturnsZeroScore()
    {
        // Arrange
        File.Delete(_tempDbPath);
        var dim = new DimFreshness();

        // Act
        var result = dim.RunInternal(_tempDbPath);

        // Assert
        Assert.Equal("DB Freshness", result.Name);
        Assert.Equal(0.0, result.Score);
        Assert.Equal("RED", result.Detail["zone"]);
        Assert.Equal("DB not found", result.Detail["detail"]);
        Assert.Contains("Copilot CLI", (string)result.Detail["hint"]!);
    }

    [Fact]
    public void Run_ReturnsFormattedAgeInDetail()
    {
        // Arrange
        var ageHours = 12.5;
        File.SetLastWriteTimeUtc(_tempDbPath, DateTime.UtcNow.AddHours(-ageHours));
        var dim = new DimFreshness();

        // Act
        var result = dim.RunInternal(_tempDbPath);

        // Assert
        var detail = (string)result.Detail["detail"]!;
        Assert.Contains("h old", detail);
        // Should be formatted as "X.Xh old" with 1 decimal place
        Assert.Matches(@"^\d+\.\d{1}h old$", detail);
    }
}
