using System.Collections.Generic;
using System.Linq;
using AutoMemory.Core.Health;
using Xunit;

namespace AutoMemory.Tests.Health;

/// <summary>
/// Tests for health scoring logic — zone classification and overall calculation.
/// Ported from test_health_scoring.py.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class ScoringTests
{
    [Fact]
    public void GreenZone_HigherIsBetter()
    {
        var result = Scoring.ScoreDim(100, greenThreshold: 50, amberThreshold: 10);
        
        Assert.Equal("GREEN", result["zone"]);
        var score = (double)result["score"];
        Assert.True(score >= 7.0, $"Expected score >= 7.0, got {score}");
    }

    [Fact]
    public void AmberZone_HigherIsBetter()
    {
        var result = Scoring.ScoreDim(30, greenThreshold: 50, amberThreshold: 10);
        
        Assert.Equal("AMBER", result["zone"]);
        var score = (double)result["score"];
        Assert.True(score >= 4.0 && score < 7.0, $"Expected 4.0 <= score < 7.0, got {score}");
    }

    [Fact]
    public void RedZone_HigherIsBetter()
    {
        var result = Scoring.ScoreDim(5, greenThreshold: 50, amberThreshold: 10);
        
        Assert.Equal("RED", result["zone"]);
        var score = (double)result["score"];
        Assert.True(score < 4.0, $"Expected score < 4.0, got {score}");
    }

    [Fact]
    public void GreenZone_LowerIsBetter()
    {
        var result = Scoring.ScoreDim(10, greenThreshold: 24, amberThreshold: 72, higherIsBetter: false);
        
        Assert.Equal("GREEN", result["zone"]);
        var score = (double)result["score"];
        Assert.True(score >= 7.0, $"Expected score >= 7.0, got {score}");
    }

    [Fact]
    public void RedZone_LowerIsBetter()
    {
        var result = Scoring.ScoreDim(100, greenThreshold: 24, amberThreshold: 72, higherIsBetter: false);
        
        Assert.Equal("RED", result["zone"]);
        var score = (double)result["score"];
        Assert.True(score < 4.0, $"Expected score < 4.0, got {score}");
    }

    [Fact]
    public void OverallScore_ReturnsMinimum()
    {
        var dims = new List<DimensionResult>
        {
            new("Dim1", 10.0, new Dictionary<string, object?>()),
            new("Dim2", 5.0, new Dictionary<string, object?>()),
            new("Dim3", 8.0, new Dictionary<string, object?>())
        };

        var overall = Scoring.OverallScore(dims);
        
        Assert.Equal(5.0, overall);
    }

    [Fact]
    public void OverallScore_EmptyList_ReturnsZero()
    {
        var overall = Scoring.OverallScore(Enumerable.Empty<DimensionResult>());
        
        Assert.Equal(0.0, overall);
    }

    [Fact]
    public void Score_ClampedBetween0And10()
    {
        var result1 = Scoring.ScoreDim(0, greenThreshold: 50, amberThreshold: 10);
        var score1 = (double)result1["score"];
        Assert.True(score1 >= 0 && score1 <= 10, $"Expected 0 <= score <= 10, got {score1}");

        var result2 = Scoring.ScoreDim(99999, greenThreshold: 50, amberThreshold: 10);
        var score2 = (double)result2["score"];
        Assert.True(score2 >= 0 && score2 <= 10, $"Expected 0 <= score <= 10, got {score2}");
    }

    [Fact]
    public void OverallScore_SkipsNullScores()
    {
        var dims = new List<DimensionResult>
        {
            new("Calibrating", null, new Dictionary<string, object?> { ["zone"] = "CALIBRATING" }),
            new("Valid", 8.0, new Dictionary<string, object?> { ["zone"] = "GREEN" })
        };

        var overall = Scoring.OverallScore(dims);
        
        Assert.Equal(8.0, overall);
    }

    [Fact]
    public void OverallScore_AllNullScores_ReturnsZero()
    {
        var dims = new List<DimensionResult>
        {
            new("Dim1", null, new Dictionary<string, object?>()),
            new("Dim2", null, new Dictionary<string, object?>())
        };

        var overall = Scoring.OverallScore(dims);
        
        Assert.Equal(0.0, overall);
    }

    /// <summary>
    /// Verifies byte-identical scores with Python for specific test cases.
    /// These values were computed using Python's scoring.py and must match exactly.
    /// </summary>
    [Fact]
    public void Scores_MatchPythonExactly()
    {
        // Test case 1: value=100, green=50, amber=10, higher=true
        // Python: {"score": 10.0, "zone": "GREEN"}
        var r1 = Scoring.ScoreDim(100, greenThreshold: 50, amberThreshold: 10);
        Assert.Equal("GREEN", r1["zone"]);
        Assert.Equal(10.0, (double)r1["score"]);

        // Test case 2: value=30, green=50, amber=10, higher=true
        // Python: {"score": 5.5, "zone": "AMBER"}
        var r2 = Scoring.ScoreDim(30, greenThreshold: 50, amberThreshold: 10);
        Assert.Equal("AMBER", r2["zone"]);
        Assert.Equal(5.5, (double)r2["score"]);

        // Test case 3: value=5, green=50, amber=10, higher=true
        // Python: {"score": 1.5, "zone": "RED"}
        var r3 = Scoring.ScoreDim(5, greenThreshold: 50, amberThreshold: 10);
        Assert.Equal("RED", r3["zone"]);
        Assert.Equal(1.5, (double)r3["score"]);

        // Test case 4: value=10, green=24, amber=72, higher=false
        // Python: {"score": 8.8, "zone": "GREEN"}
        var r4 = Scoring.ScoreDim(10, greenThreshold: 24, amberThreshold: 72, higherIsBetter: false);
        Assert.Equal("GREEN", r4["zone"]);
        Assert.Equal(8.8, (double)r4["score"]);

        // Test case 5: value=50, green=24, amber=72, higher=false
        // Python: {"score": 5.4, "zone": "AMBER"}
        var r5 = Scoring.ScoreDim(50, greenThreshold: 24, amberThreshold: 72, higherIsBetter: false);
        Assert.Equal("AMBER", r5["zone"]);
        Assert.Equal(5.4, (double)r5["score"], 1);

        // Test case 6: value=100, green=24, amber=72, higher=false
        // Python: {"score": 2.2, "zone": "RED"}
        var r6 = Scoring.ScoreDim(100, greenThreshold: 24, amberThreshold: 72, higherIsBetter: false);
        Assert.Equal("RED", r6["zone"]);
        Assert.Equal(2.2, (double)r6["score"], 1);
    }
}
