// Copyright (c) auto-memoryNet contributors. Licensed under MIT.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMemory.Mcp;

// MCP server for session-recall over stdio
// Implements JSON-RPC 2.0 protocol
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var server = new McpServer();

try
{
    using var stdin = Console.OpenStandardInput();
    using var stdout = Console.OpenStandardOutput();
    using var reader = new StreamReader(stdin);
    using var writer = new StreamWriter(stdout) { AutoFlush = true };

    await server.RunAsync(reader, writer);
}
catch (Exception ex)
{
    // Log to stderr since stdout is for JSON-RPC
    await Console.Error.WriteLineAsync($"MCP server error: {ex.Message}");
    return 1;
}

return 0;
