using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 2: Schema Integrity — expected tables and columns present.
/// </summary>
public sealed class DimSchema : IHealthDimension
{
    private const string Hint = "Run `session-recall schema-check` for details";

    public string Name => "Schema Integrity";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        var checkResult = Db.SchemaCheck.CheckSchema(conn);

        if (checkResult.IsValid)
        {
            return new DimensionResult(
                Name,
                10.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "GREEN",
                    ["detail"] = "All tables/columns OK",
                    ["hint"] = ""
                }
            );
        }

        var hasMissingTable = checkResult.Problems.Any(p => p.Contains("MISSING TABLE", StringComparison.Ordinal));
        var detail = string.Join("; ", checkResult.Problems);

        if (hasMissingTable)
        {
            return new DimensionResult(
                Name,
                1.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "RED",
                    ["detail"] = detail,
                    ["hint"] = Hint
                }
            );
        }

        return new DimensionResult(
            Name,
            5.0,
            new Dictionary<string, object?>
            {
                ["zone"] = "AMBER",
                ["detail"] = detail,
                ["hint"] = Hint
            }
        );
    }
}
