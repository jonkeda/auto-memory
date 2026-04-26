# AutoMemory MCP Server

Model Context Protocol (MCP) server for session-recall.

## Overview

`session-recall-mcp` exposes session-recall functionality as an MCP server, enabling IDE and AI assistant integration via the standardized MCP protocol.

## Transport

- **stdio** - JSON-RPC 2.0 over stdin/stdout

## Available Tools

1. **recall.list** - List recent sessions with summary statistics
2. **recall.files** - List files referenced in recent sessions
3. **recall.checkpoints** - List checkpoint states from sessions
4. **recall.search** - Search session history by content or repo
5. **recall.show** - Show detailed content for a specific session
6. **recall.health** - Check session database health
7. **recall.schema_check** - Verify session database schema

## Usage

```bash
# Start the MCP server (stdio mode)
session-recall-mcp

# The server expects JSON-RPC 2.0 requests on stdin
# Example:
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}' | session-recall-mcp
```

## Protocol Flow

1. **Initialize**: Client sends `initialize` method
2. **Discover**: Client sends `tools/list` to get available tools
3. **Execute**: Client sends `tools/call` with tool name and arguments

## Example

```json
// Initialize
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}

// List tools
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}

// Call recall.health
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"recall.health","arguments":{"json":true}}}
```

## Testing

```powershell
# Run protocol tests
pwsh net/test_mcp.ps1
```

## Architecture

- **McpServer**: JSON-RPC 2.0 protocol handler
- **ToolRouter**: Routes tool calls to session-recall commands
- Reuses **AutoMemory.Core** and **AutoMemory.Cli** for all functionality

## Notes

- This server is part of Phase 8 (stretch goals)
- Binary size: ~15MB (untrimmed)
- Startup latency: <100ms
- MCP protocol version: 2024-11-05
