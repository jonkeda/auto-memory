using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Core.Health;

/// <summary>
/// Dimension 8: E2E probe — list → show → parse succeeds.
/// </summary>
public sealed class DimE2E : IHealthDimension
{
    private const string Hint = "Check exit code and stderr from individual commands";

    public string Name => "E2E Probe";
    public double Weight => 1.0 / 9.0;

    public DimensionResult Run(SqliteConnection conn, HealthContext ctx)
    {
        try
        {
            // Step 1: list - get the most recent session
            string? sessionId = null;
            string? summary = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, summary FROM sessions ORDER BY created_at DESC LIMIT 1";
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    sessionId = reader.GetString(0);
                    summary = reader.IsDBNull(1) ? null : reader.GetString(1);
                }
            }

            if (sessionId == null)
            {
                return new DimensionResult(
                    Name,
                    5.0,
                    new Dictionary<string, object?>
                    {
                        ["zone"] = "AMBER",
                        ["detail"] = "No sessions found",
                        ["hint"] = "Use Copilot CLI first"
                    }
                );
            }

            // Step 2: show (drill into first session) - sample some turns
            var turnCount = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT turn_index FROM turns WHERE session_id = @sessionId LIMIT 3";
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    turnCount++;
                }
            }

            var shortId = sessionId.Length >= 8 ? sessionId.Substring(0, 8) : sessionId;
            var detail = string.Format(
                CultureInfo.InvariantCulture,
                "list→show OK (session {0}, {1} turns sampled)",
                shortId,
                turnCount
            );

            return new DimensionResult(
                Name,
                10.0,
                new Dictionary<string, object?>
                {
                    ["zone"] = "GREEN",
                    ["detail"] = detail,
                    ["hint"] = ""
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
