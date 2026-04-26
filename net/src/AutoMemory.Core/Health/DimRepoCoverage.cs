using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using AutoMemory.Core.Util;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 6: Repo Coverage — sessions exist for the current repo.
/// </summary>
public sealed class DimRepoCoverage : IHealthDimension
{
    private const string Hint = "Run in a git repo or pass --repo all";

    public string Name => "Repo Coverage";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        var repo = DetectRepo.Detect();
        if (repo is null)
        {
            return new DimensionResult(
                Name,
                1.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "RED",
                    ["detail"] = "Cannot detect repo from cwd",
                    ["hint"] = Hint
                }
            );
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sessions WHERE repository = @repo";
            cmd.Parameters.AddWithValue("@repo", repo);
            var result = cmd.ExecuteScalar();
            var count = Convert.ToInt64(result);

            if (count >= 1)
            {
                return new DimensionResult(
                    Name,
                    10.0,
                    new Dictionary<string, object?>
                    {
                        ["zone"] = "GREEN",
                        ["detail"] = $"{count} sessions for {repo}",
                        ["hint"] = ""
                    }
                );
            }

            return new DimensionResult(
                Name,
                5.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "AMBER",
                    ["detail"] = $"0 sessions for {repo}",
                    ["hint"] = Hint
                }
            );
        }
        catch (Exception ex)
        {
            return new DimensionResult(
                Name,
                0.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "RED",
                    ["detail"] = ex.Message,
                    ["hint"] = Hint
                }
            );
        }
    }
}
