// Copyright (c) auto-memoryNet contributors. Licensed under MIT.
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AutoMemory.Mcp;

/// <summary>
/// Minimal MCP (Model Context Protocol) server implementation.
/// Handles JSON-RPC 2.0 over stdio.
/// </summary>
public sealed class McpServer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };


    private bool _initialized;

    public async Task RunAsync(StreamReader reader, StreamWriter writer)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var response = await HandleRequestAsync(line);
            if (response != null)
            {
                await writer.WriteLineAsync(response);
            }
        }
    }

    private async Task<string?> HandleRequestAsync(string requestJson)
    {
        try
        {
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestJson, s_jsonOptions);
            if (request?.Method == null)
            {
                return CreateErrorResponse(null, -32600, "Invalid Request");
            }

            var result = request.Method switch
            {
                "initialize" => HandleInitialize(request.Params),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCallAsync(request.Params),
                _ => CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
            };

            if (result != null && request.Id != null)
            {
                return CreateSuccessResponse(request.Id, result);
            }

            return null;
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(null, -32603, $"Internal error: {ex.Message}");
        }
    }

    private object HandleInitialize(JsonNode? @params)
    {
        _initialized = true;
        return new
        {
            protocolVersion = "2024-11-05",
            serverInfo = new
            {
                name = "session-recall-mcp",
                version = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.
                    InformationalVersion ?? "0.1.0"
            },
            capabilities = new
            {
                tools = new { }
            }
        };
    }

    private object HandleToolsList()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Server not initialized");
        }

        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "recall.list",
                    description = "List recent sessions with summary statistics",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            limit = new { type = "integer", description = "Max sessions to return", @default = 10 },
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        }
                    }
                },
                new
                {
                    name = "recall.files",
                    description = "List files referenced in recent sessions",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            limit = new { type = "integer", description = "Max files to return", @default = 20 },
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        }
                    }
                },
                new
                {
                    name = "recall.checkpoints",
                    description = "List checkpoint states from sessions",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            limit = new { type = "integer", description = "Max checkpoints to return", @default = 10 },
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        }
                    }
                },
                new
                {
                    name = "recall.search",
                    description = "Search session history by content or repo",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new { type = "string", description = "Search query" },
                            repo = new { type = "string", description = "Filter by repository" },
                            limit = new { type = "integer", description = "Max results to return", @default = 10 },
                            days = new { type = "integer", description = "Limit to recent days" },
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        },
                        required = new[] { "query" }
                    }
                },
                new
                {
                    name = "recall.show",
                    description = "Show detailed content for a specific session",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            sessionId = new { type = "string", description = "Session ID to show" },
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        },
                        required = new[] { "sessionId" }
                    }
                },
                new
                {
                    name = "recall.health",
                    description = "Check session database health",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        }
                    }
                },
                new
                {
                    name = "recall.schema_check",
                    description = "Verify session database schema",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            json = new { type = "boolean", description = "Output as JSON", @default = false }
                        }
                    }
                }
            }
        };
    }

    private async Task<object> HandleToolsCallAsync(JsonNode? @params)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Server not initialized");
        }

        var name = @params?["name"]?.GetValue<string>();
        var arguments = @params?["arguments"];

        if (name == null)
        {
            throw new ArgumentException("Tool name is required");
        }

        var result = await ToolRouter.ExecuteToolAsync(name, arguments);

        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private static string CreateSuccessResponse(object id, object result)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            result
        };
        return JsonSerializer.Serialize(response, s_jsonOptions);
    }

    private static string CreateErrorResponse(object? id, int code, string message)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            error = new
            {
                code,
                message
            }
        };
        return JsonSerializer.Serialize(response, s_jsonOptions);
    }
}

file sealed class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string? JsonRpc { get; set; }

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    public JsonNode? Params { get; set; }
}
