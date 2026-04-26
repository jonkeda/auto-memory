using System.Diagnostics;
using System.Reflection;
using System.Text;
using AutoMemory.Cli;
using AutoMemory.Cli.Commands;
using AutoMemory.Core;
using AutoMemory.Core.Db;
using AutoMemory.Core.Util;

// Configure console for UTF-8 output (emojis, unicode)
Console.OutputEncoding = Encoding.UTF8;

// Force Unix newlines for cross-platform parity
Console.Out.NewLine = "\n";
Console.Error.NewLine = "\n";

// Initialize telemetry
Telemetry.Init(Config.TelemetryPath);

var cliArgs = Environment.GetCommandLineArgs()[1..];

// Handle version flag
if (cliArgs.Length > 0 && cliArgs[0] is "-v" or "--version")
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "unknown";
    Console.WriteLine($"session-recall {version}");
    return 0;
}

// No arguments or help requested at top level
if (cliArgs.Length == 0 || cliArgs[0] is "-h" or "--help")
{
    Console.WriteLine("usage: auto-memory <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Query Copilot CLI session history");
    Console.WriteLine();
    Console.WriteLine("commands:");
    Console.WriteLine("  list           Recent sessions");
    Console.WriteLine("  files          Recently touched files");
    Console.WriteLine("  checkpoints    Recent checkpoints");
    Console.WriteLine("  show           Show session details");
    Console.WriteLine("  search         Full-text search");
    Console.WriteLine("  health         Health check");
    Console.WriteLine("  schema-check   Validate DB schema");
    Console.WriteLine();
    Console.WriteLine("Use 'auto-memory <command> --help' for more information about a command.");
    return 0;
}

var command = cliArgs[0];
var commandArgs = cliArgs[1..];

// Tier map for telemetry
var tierMap = new Dictionary<string, int>
{
    ["list"] = 1,
    ["files"] = 1,
    ["checkpoints"] = 1,
    ["search"] = 2,
    ["show"] = 3,
    ["health"] = 0,
    ["schema-check"] = 0,
};

var sw = Stopwatch.StartNew();
int exitCode = 1;

try
{
    exitCode = command switch
    {
        "list" => RunList(commandArgs),
        "files" => RunFiles(commandArgs),
        "checkpoints" => RunCheckpoints(commandArgs),
        "search" => RunSearch(commandArgs),
        "show" => RunShow(commandArgs),
        "health" => RunHealth(commandArgs),
        "schema-check" => RunSchemaCheck(commandArgs),
        _ => UnknownCommand(command)
    };
}
catch (HelpRequestedException ex)
{
    Console.WriteLine(ex.HelpText);
    exitCode = 0;
}
catch (UsageException ex)
{
    Console.Error.WriteLine($"usage: auto-memory {command}");
    Console.Error.WriteLine($"auto-memory {command}: error: {ex.Message}");
    exitCode = 2;
}
catch (DatabaseNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    exitCode = 4;
}
catch (DatabaseLockedException ex)
{
    Console.Error.WriteLine(ex.Message);
    exitCode = 3;
}
finally
{
    // Compute command-specific telemetry fields
    var tier = tierMap.TryGetValue(command, out var t) ? (int?)t : null;
    string? queryHash = null;
    string? sessionIdPrefix = null;

    if (command == "search" && commandArgs.Length > 0)
    {
        // Extract query from args (skip flags)
        var query = commandArgs.FirstOrDefault(arg => !arg.StartsWith('-')) ?? string.Empty;
        queryHash = Telemetry.QueryHash(query);
    }
    else if (command == "show" && commandArgs.Length > 0)
    {
        // Extract session_id from args (skip flags)
        var sessionId = commandArgs.FirstOrDefault(arg => !arg.StartsWith('-')) ?? string.Empty;
        sessionIdPrefix = sessionId.Length >= 8 ? sessionId[..8] : sessionId;
    }

    Telemetry.Record(
        cmd: command,
        durationMs: (int)sw.ElapsedMilliseconds,
        exitCode: exitCode,
        tier: tier,
        queryHash: queryHash,
        sessionIdPrefix: sessionIdPrefix);
}

return exitCode;

static int RunList(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory list", "List recent sessions for the current (or specified) repository")
        .AddOption("repo", defaultValue: null, help: "Repository path (default: auto-detect)")
        .AddOption("limit", defaultValue: null, help: "Maximum number of results (default: 10)")
        .AddOption("days", defaultValue: null, help: "Only include sessions from last N days (default: 30)")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return ListCommand.Run(parsedArgs);
}

static int RunFiles(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory files", "List recently touched files")
        .AddOption("repo", defaultValue: null, help: "Repository path (default: auto-detect)")
        .AddOption("limit", defaultValue: null, help: "Maximum number of results (default: 10)")
        .AddOption("days", defaultValue: null, help: "Only include files from last N days (default: no limit)")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return FilesCommand.Run(parsedArgs);
}

static int RunCheckpoints(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory checkpoints", "List recent checkpoints")
        .AddOption("repo", defaultValue: null, help: "Repository path (default: auto-detect)")
        .AddOption("limit", defaultValue: null, help: "Maximum number of results (default: 5)")
        .AddOption("days", defaultValue: null, help: "Only include checkpoints from last N days (default: no limit)")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return CheckpointsCommand.Run(parsedArgs);
}

static int RunSearch(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory search", "Full-text search across session turns and summaries")
        .AddPositional("query", required: true, help: "Search query")
        .AddOption("repo", defaultValue: null, help: "Repository path (default: auto-detect)")
        .AddOption("limit", defaultValue: null, help: "Maximum number of results (default: 5)")
        .AddOption("days", defaultValue: null, help: "Only include results from last N days (default: no limit)")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return SearchCommand.Run(parsedArgs);
}

static int RunShow(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory show", "Show detailed info for a single session")
        .AddPositional("session_id", required: true, help: "Session ID or prefix (4+ chars)")
        .AddOption("turns", defaultValue: null, help: "Limit to last N turns (default: all)")
        .AddOption("full", isFlag: true, help: "Show full turn content (default: truncate at 500 chars)")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return ShowCommand.Run(parsedArgs);
}

static int RunSchemaCheck(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory schema-check", "Validate DB schema")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return SchemaCheckCommand.Run(parsedArgs);
}

static int RunHealth(string[] commandArgs)
{
    var parser = new ArgParser("auto-memory health", "Run all health check dimensions")
        .AddOption("json", isFlag: true, help: "Output as JSON");

    var parsedArgs = parser.Parse(commandArgs);
    return HealthCommand.Run(parsedArgs);
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"auto-memory: error: unrecognized command '{command}'");
    Console.Error.WriteLine("Use 'auto-memory --help' for a list of available commands.");
    return 2;
}
