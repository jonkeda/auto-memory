# Step 03 — MCP server in C#

## Goal
Reuse `AutoMemory.Core` to expose a Model Context Protocol server: `session-recall-mcp`.

## Tools exposed
- `recall.list`, `recall.files`, `recall.checkpoints`
- `recall.search`, `recall.show`
- `recall.health`, `recall.schema_check`

## Transport
- stdio MCP transport for IDE integration.

## Risks
- MCP spec churn.
- Binary size + startup latency for IDE plugin lifecycle.

## Done when
- [ ] `session-recall-mcp` starts and answers tool list.
- [ ] At least one MCP-aware client can call each tool.
