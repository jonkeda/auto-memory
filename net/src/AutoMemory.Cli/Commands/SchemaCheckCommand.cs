using System;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Util;
using Microsoft.Data.Sqlite;

namespace AutoMemory.Cli.Commands;

/// <summary>
/// Schema-check command — validates session-store.db structure.
/// </summary>
public static class SchemaCheckCommand
{
    public static int Run(ParsedArgs args)
    {
        try
        {
            using var conn = Connect.ConnectReadOnly(Config.DbPath);
            return RunCore(args, conn);
        }
        catch (DatabaseNotFoundException)
        {
            throw; // Let Program.cs handle exit code 4
        }
        catch (DatabaseLockedException)
        {
            throw; // Let Program.cs handle exit code 3
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to check schema: {ex.Message}");
            return 1;
        }
    }
    
    /// <summary>
    /// Execute the schema-check subcommand with an existing connection (for testing).
    /// </summary>
    internal static int RunCore(ParsedArgs args, SqliteConnection conn)
    {
        var jsonMode = args.GetFlag("json");
        
        var result = SchemaCheck.CheckSchema(conn);
        
        if (!result.IsValid)
        {
            if (jsonMode)
            {
                var jsonResult = new SchemaCheckJsonResult
                {
                    Ok = false,
                    Problems = result.Problems.ToList()
                };
                Console.WriteLine(FormatOutput.FmtJson(jsonResult));
            }
            else
            {
                Console.Error.WriteLine("❌ Schema drift. Copilot CLI may have been upgraded.");
                foreach (var problem in result.Problems)
                {
                    Console.Error.WriteLine($"   - {problem}");
                }
            }
            return 2;
        }
        
        if (jsonMode)
        {
            var jsonResult = new SchemaCheckJsonResult
            {
                Ok = true,
                Problems = new List<string>()
            };
            Console.WriteLine(FormatOutput.FmtJson(jsonResult));
        }
        else
        {
            Console.WriteLine("✅ Schema OK");
        }
        return 0;
    }
}
