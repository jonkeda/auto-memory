using System;
using System.Collections.Generic;

namespace AutoMemory.Core.Health;

/// <summary>
/// Result of a health dimension check.
/// </summary>
/// <param name="Name">Name of the dimension.</param>
/// <param name="Score">Score from 0.0 (worst) to 10.0 (best), or null if not applicable.</param>
/// <param name="Detail">Additional detail fields (e.g., zone, detail message, hint).</param>
public sealed record DimensionResult(
    string Name,
    double? Score,
    IReadOnlyDictionary<string, object?> Detail
);
