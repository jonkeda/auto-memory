using System;

namespace AutoMemory.Core.Health;

/// <summary>
/// Shared context for health checks, passed to all dimensions.
/// </summary>
/// <param name="Now">Current timestamp for the health check run.</param>
/// <param name="Repo">Repository path, if applicable.</param>
public sealed record HealthContext(
    DateTimeOffset Now,
    string? Repo
);
