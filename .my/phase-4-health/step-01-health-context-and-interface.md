# Step 01 — Health context + IHealthDimension

## Goal
Define the dimension interface and shared context object.

## Files
- `AutoMemory.Core/Health/IHealthDimension.cs`
- `AutoMemory.Core/Health/HealthContext.cs`
- `AutoMemory.Core/Health/DimensionResult.cs`

## API
```csharp
public interface IHealthDimension {
    string Name { get; }
    double Weight { get; }
    DimensionResult Run(SqliteConnection conn, HealthContext ctx);
}

public sealed record DimensionResult(string Name, double Score, IReadOnlyDictionary<string, object?> Detail);
public sealed record HealthContext(DateTimeOffset Now, string? Repo, /* …shared probes… */);
```

## Done when
- [ ] Interface + records compile.
- [ ] Sum of weights across all 9 dims tested ≈ 1.0 (±epsilon).
