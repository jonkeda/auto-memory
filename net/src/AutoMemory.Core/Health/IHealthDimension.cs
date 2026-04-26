using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Interface for a health check dimension.
/// Each dimension evaluates one aspect of the system's health.
/// </summary>
public interface IHealthDimension
{
    /// <summary>
    /// Gets the name of this dimension (e.g., "Schema Integrity").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the weight of this dimension in the overall health score.
    /// All dimension weights must sum to 1.0.
    /// </summary>
    double Weight { get; }

    /// <summary>
    /// Executes the health check for this dimension.
    /// </summary>
    /// <param name="conn">Read-only database connection.</param>
    /// <param name="ctx">Shared health check context.</param>
    /// <returns>The result of the health check.</returns>
    DimensionResult Run(SqliteConnection conn, HealthContext ctx);
}
