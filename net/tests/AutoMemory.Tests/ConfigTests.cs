using System;
using System.IO;
using AutoMemory.Core;
using Xunit;

namespace AutoMemory.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public class ConfigTests
{
    [Fact]
    public void DbPath_UsesEnvironmentOverride_WhenSet()
    {
        // The static constructor has already run, but we can verify
        // that if SESSION_RECALL_DB is set, it would be used
        var envValue = Environment.GetEnvironmentVariable("SESSION_RECALL_DB");
        
        if (!string.IsNullOrEmpty(envValue))
        {
            // If env var is set, Config.DbPath should match it
            Assert.Equal(Path.GetFullPath(envValue), Config.DbPath);
        }
        else
        {
            // If env var is not set, should use default path
            Assert.Contains(".copilot", Config.DbPath);
            Assert.Contains("session-store.db", Config.DbPath);
        }
    }

    [Fact]
    public void TelemetryPath_UsesEnvironmentOverride_WhenSet()
    {
        var envValue = Environment.GetEnvironmentVariable("SESSION_RECALL_TELEMETRY");
        
        if (!string.IsNullOrEmpty(envValue))
        {
            // Normalize both paths to handle short vs long form (e.g., JONK43~1 vs jonk435491)
            var expectedPath = new FileInfo(Path.GetFullPath(envValue)).FullName;
            var actualPath = new FileInfo(Config.TelemetryPath).FullName;
            Assert.Equal(expectedPath, actualPath, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Contains(".copilot", Config.TelemetryPath);
            Assert.Contains(".session-recall-stats.json", Config.TelemetryPath);
        }
    }

    [Fact]
    public void DbPath_ResolvesToAbsolutePath()
    {
        Assert.True(Path.IsPathFullyQualified(Config.DbPath), 
            $"DbPath should be absolute but got: {Config.DbPath}");
    }

    [Fact]
    public void TelemetryPath_ResolvesToAbsolutePath()
    {
        Assert.True(Path.IsPathFullyQualified(Config.TelemetryPath),
            $"TelemetryPath should be absolute but got: {Config.TelemetryPath}");
    }

    [Fact]
    public void RetryDelaysMs_HasExpectedValues()
    {
        Assert.Equal([50, 150, 450], Config.RetryDelaysMs);
    }

    [Fact]
    public void MaxRetries_EqualsRetryDelaysLength()
    {
        Assert.Equal(Config.RetryDelaysMs.Length, Config.MaxRetries);
        Assert.Equal(3, Config.MaxRetries);
    }

    [Fact]
    public void ExpectedSchemaVersion_IsOne()
    {
        Assert.Equal(1, Config.ExpectedSchemaVersion);
    }

    [Fact]
    public void DefaultPaths_ContainExpectedSegments()
    {
        // When no env override is set, verify path structure
        var envDb = Environment.GetEnvironmentVariable("SESSION_RECALL_DB");
        var envTelem = Environment.GetEnvironmentVariable("SESSION_RECALL_TELEMETRY");

        if (string.IsNullOrEmpty(envDb))
        {
            Assert.EndsWith(Path.Combine(".copilot", "session-store.db"), Config.DbPath);
        }

        if (string.IsNullOrEmpty(envTelem))
        {
            Assert.EndsWith(Path.Combine(".copilot", "scripts", ".session-recall-stats.json"), 
                Config.TelemetryPath);
        }
    }
}
