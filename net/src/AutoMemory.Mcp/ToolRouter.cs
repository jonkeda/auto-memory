// Copyright (c) auto-memoryNet contributors. Licensed under MIT.
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using AutoMemory.Core;
using AutoMemory.Core.Util;

namespace AutoMemory.Mcp;

/// <summary>
/// Routes MCP tool calls to session-recall commands.
/// </summary>
public sealed class ToolRouter
{
    static ToolRouter()
    {
        // Initialize telemetry
        Telemetry.Init(Config.TelemetryPath);
    }

    public static async Task<string> ExecuteToolAsync(string toolName, JsonNode? arguments)
    {
        // Parse tool name
        var parts = toolName.Split('.');
        if (parts.Length != 2 || parts[0] != "recall")
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        var command = parts[1];

        // Convert JSON arguments to ParsedArgs
        var args = ConvertToParsedArgs(arguments);

        // Capture console output
        var originalOut = Console.Out;
        var originalError = Console.Error;
        
        try
        {
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);

            // Set Unix newlines for cross-platform parity
            outWriter.NewLine = "\n";
            errorWriter.NewLine = "\n";

            int exitCode = command switch
            {
                "list" => ListCommand.Run(args),
                "files" => FilesCommand.Run(args),
                "checkpoints" => CheckpointsCommand.Run(args),
                "search" => SearchCommand.Run(args),
                "show" => ShowCommand.Run(args),
                "health" => HealthCommand.Run(args),
                "schema_check" => SchemaCheckCommand.Run(args),
                _ => throw new ArgumentException($"Unknown command: {command}")
            };

            // Combine stdout and stderr
            var output = outWriter.ToString();
            var errors = errorWriter.ToString();

            if (!string.IsNullOrEmpty(errors))
            {
                output = errors + output;
            }

            if (exitCode != 0)
            {
                output = $"[Exit code {exitCode}]\n{output}";
            }

            return output;
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static ParsedArgs ConvertToParsedArgs(JsonNode? arguments)
    {
        var positionals = new List<string>();
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (arguments is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                var key = prop.Key;
                var value = prop.Value;

                if (value == null)
                    continue;

                // Handle special positional arguments
                if (key.Equals("query", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("sessionId", StringComparison.OrdinalIgnoreCase))
                {
                    positionals.Add(value.GetValue<string>());
                    continue;
                }

                // Handle boolean flags
                if (value.GetValueKind() == System.Text.Json.JsonValueKind.True ||
                    value.GetValueKind() == System.Text.Json.JsonValueKind.False)
                {
                    values[key] = value.GetValue<bool>() ? "true" : null;
                    continue;
                }

                // Handle options
                if (value.GetValueKind() == System.Text.Json.JsonValueKind.Number)
                {
                    values[key] = value.GetValue<int>().ToString(CultureInfo.InvariantCulture);
                }
                else if (value.GetValueKind() == System.Text.Json.JsonValueKind.String)
                {
                    var strValue = value.GetValue<string>();
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        values[key] = strValue;
                    }
                }
            }
        }

        // Use reflection to call internal constructor
        var ctor = typeof(ParsedArgs).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(Dictionary<string, string?>), typeof(List<string>) },
            null);

        if (ctor == null)
        {
            throw new InvalidOperationException("Cannot find ParsedArgs internal constructor");
        }

        return (ParsedArgs)ctor.Invoke(new object[] { values, positionals });
    }

}
