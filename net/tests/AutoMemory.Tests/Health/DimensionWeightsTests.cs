using System;
using System.Linq;
using AutoMemory.Core.Health;
using Xunit;

namespace AutoMemory.Tests.Health;

/// <summary>
/// Tests for health dimension weights and interface contracts.
/// </summary>
public sealed class DimensionWeightsTests
{
    private static readonly IHealthDimension[] s_allDimensions = 
    [
        new DimFreshness(),
        new DimSchema(),
        new DimLatency(),
        new DimCorpus(),
        new DimSummaryCoverage(),
        new DimRepoCoverage(),
        new DimConcurrency(),
        new DimE2E(),
        new DimDisclosure()
    ];

    [Fact]
    public void AllNineDimensionsExist()
    {
        Assert.Equal(9, s_allDimensions.Length);
    }

    [Fact]
    public void WeightsSumToOne()
    {
        var sum = s_allDimensions.Sum(d => d.Weight);
        
        // Assert sum is approximately 1.0 with epsilon for floating point comparison
        const double epsilon = 1e-10;
        Assert.True(Math.Abs(sum - 1.0) < epsilon, 
            $"Expected weights to sum to 1.0, but got {sum:F15}");
    }

    [Fact]
    public void AllWeightsArePositive()
    {
        foreach (var dim in s_allDimensions)
        {
            Assert.True(dim.Weight > 0, $"{dim.Name} has non-positive weight: {dim.Weight}");
        }
    }

    [Fact]
    public void AllDimensionsHaveUniqueNames()
    {
        var names = s_allDimensions.Select(d => d.Name).ToList();
        var uniqueNames = names.Distinct().ToList();
        
        Assert.Equal(names.Count, uniqueNames.Count);
    }
}
