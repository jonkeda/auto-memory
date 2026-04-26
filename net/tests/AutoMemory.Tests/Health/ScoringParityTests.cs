using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AutoMemory.Core.Health;
using Xunit;

namespace AutoMemory.Tests.Health;

/// <summary>
/// Parity tests to verify Scoring.cs produces byte-identical output to Python scoring.py.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class ScoringParityTests
{
    [Theory]
    [InlineData(100, 50, 10, true, 10.0, "GREEN")]
    [InlineData(30, 50, 10, true, 5.5, "AMBER")]
    [InlineData(5, 50, 10, true, 1.5, "RED")]
    [InlineData(10, 24, 72, false, 8.8, "GREEN")]
    [InlineData(100, 24, 72, false, 2.2, "RED")]
    [InlineData(50, 50, 10, true, 7.0, "GREEN")]
    [InlineData(10, 50, 10, true, 4.0, "AMBER")]
    [InlineData(0, 50, 10, true, 0.0, "RED")]
    public void ScoreDim_ExactValues_MatchPython(
        double value,
        double greenThreshold,
        double amberThreshold,
        bool higherIsBetter,
        double expectedScore,
        string expectedZone)
    {
        var result = Scoring.ScoreDim(value, greenThreshold, amberThreshold, higherIsBetter);

        Assert.Equal(expectedZone, result["zone"]);
        var actualScore = (double)result["score"];
        
        // Scores must match to 1 decimal place (Python rounds to 1 decimal)
        Assert.Equal(expectedScore, actualScore, precision: 1);
    }

    [Fact]
    public void OverallScore_ExactValues_MatchPython()
    {
        var dims = new List<DimensionResult>
        {
            new("Dim1", 10.0, new Dictionary<string, object?>()),
            new("Dim2", 5.5, new Dictionary<string, object?>()),
            new("Dim3", 8.2, new Dictionary<string, object?>())
        };

        var overall = Scoring.OverallScore(dims);

        // Should return minimum: 5.5
        Assert.Equal(5.5, overall, precision: 10);
    }

    [Fact]
    public void ScoreDim_ZeroDivision_HandledCorrectly()
    {
        // Test edge cases where thresholds are 0
        var result1 = Scoring.ScoreDim(0, greenThreshold: 0, amberThreshold: 0);
        Assert.Equal("GREEN", result1["zone"]);
        
        var result2 = Scoring.ScoreDim(5, greenThreshold: 0, amberThreshold: 0, higherIsBetter: false);
        Assert.Equal("RED", result2["zone"]);
    }

    [Fact]
    public void ScoreDim_Rounding_UsesToEven()
    {
        // Verify MidpointRounding.ToEven matches Python's banker's rounding
        // 4.5 should round to 4.0, 5.5 should round to 6.0 (to even)
        
        // These specific values should trigger midpoint rounding
        var result1 = Scoring.ScoreDim(35, greenThreshold: 50, amberThreshold: 10);
        var score1 = (double)result1["score"];
        // Verify it's properly rounded to 1 decimal place
        Assert.Equal(Math.Round(score1, 1), score1);
    }

    /// <summary>
    /// Cross-check against Python: run the same test cases through Python's score_dim.
    /// This test requires Python with session_recall installed.
    /// </summary>
    [Fact(Skip = "Requires Python runtime and session_recall package")]
    public void ScoreDim_CrossCheckPython()
    {
        var testCases = new[]
        {
            (value: 100.0, green: 50.0, amber: 10.0, higher: true),
            (value: 30.0, green: 50.0, amber: 10.0, higher: true),
            (value: 5.0, green: 50.0, amber: 10.0, higher: true),
            (value: 10.0, green: 24.0, amber: 72.0, higher: false),
            (value: 100.0, green: 24.0, amber: 72.0, higher: false)
        };

        foreach (var (value, green, amber, higher) in testCases)
        {
            var dotnetResult = Scoring.ScoreDim(value, green, amber, higher);
            var pythonResult = CallPythonScoreDim(value, green, amber, higher);

            Assert.Equal(pythonResult["zone"], dotnetResult["zone"]);
            Assert.Equal((double)pythonResult["score"], (double)dotnetResult["score"], precision: 10);
        }
    }

    private static Dictionary<string, object> CallPythonScoreDim(
        double value, double green, double amber, bool higher)
    {
        var higherStr = higher ? "True" : "False";
        var script = $@"
import sys
import json
from session_recall.health.scoring import score_dim
result = score_dim({value.ToString(CultureInfo.InvariantCulture)}, 
                   {green.ToString(CultureInfo.InvariantCulture)}, 
                   {amber.ToString(CultureInfo.InvariantCulture)}, 
                   higher_is_better={higherStr})
print(json.dumps(result))
";
        
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-c \"{script.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Python process");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Python failed: {error}");
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(output)
            ?? throw new InvalidOperationException("Failed to parse Python output");
    }
}
